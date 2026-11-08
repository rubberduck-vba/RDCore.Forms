// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Text;
using System.Text.RegularExpressions;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Import.VB6;

/// <summary>
/// Reads a VB6 designer file (<c>.frm</c>, <c>.ctl</c> or <c>.pag</c>) and its companion binary file into a <see cref="FormDefinition"/>.
/// </summary>
/// <remarks>
/// <para>
/// The designer half, from <c>VERSION</c> to the root block's <c>End</c>, becomes the form; the rest of the file is returned as
/// <see cref="VB6FormImport.ModuleText"/>. Property and class names are read without regard to case. A component's
/// <c>Index</c> becomes its <see cref="ComponentInstance.Index"/>, and a root's <c>Client*</c> rectangle its position and size.
/// Positions and sizes are read as written: in twips for a root, and in the container's scale for a control.
/// </para>
/// <para>
/// Nothing is dropped. A property the model does not have, or whose value it cannot hold, is kept as an
/// <see cref="UnmodelledProperty"/>; a component of a class the model does not have, such as an ActiveX control, is kept as an
/// <see cref="UnmodelledComponent"/>. Each of these, and each line that cannot be read, is reported in
/// <see cref="VB6FormImport.Diagnostics"/>.
/// </para>
/// <para>
/// The import reads a file as VB6 loads it. A control on a form that writes no <c>Font</c> takes a copy of the form's font. A
/// component placed inside one that cannot hold it, such as a control inside a ListBox, is placed in the nearest enclosing
/// component that can. A companion record is read in the shape its property's type has: a string cited as <c>$"Form1.frx":0000</c>
/// has a 4-byte length, and one cited as <c>"Form1.frx":0000</c> a 1-byte length and ends at its first NUL; a picture is the
/// signature <c>lt\0\0</c> and the image file's length before its bytes; a List is a 2-byte count, then, for a list that is not
/// empty, a 2-byte word, then each item's 2-byte length and bytes; an ItemData is a List of the decimal text of each Long.
/// </para>
/// </remarks>
public static partial class VB6FormReader
{
    /// <summary>
    /// Reads a designer file's text and its companion binary file.
    /// </summary>
    /// <param name="designerFile">The designer file's text.</param>
    /// <param name="companion">
    /// The companion binary file's bytes, or <see langword="null"/> when there is none. A property citing it is then kept as text.
    /// </param>
    /// <param name="options">How strings in the companion are decoded; <see langword="null"/> for <see cref="VB6ImportOptions.Default"/>.</param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    public static VB6FormImport Read(string designerFile, byte[]? companion = null, VB6ImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(designerFile);
        options ??= VB6ImportOptions.Default;

        var diagnostics = new DiagnosticBag();
        var file = DesignerFile.Parse(designerFile, diagnostics);
        var form = file.Root is null
            ? null
            : new Importer(diagnostics, companion is null ? null : new Companion(companion, options.Encoding)).Import(file, file.Root);
        return new VB6FormImport(form, file.ModuleText, file.ModuleLine, diagnostics.Items);
    }

    /// <summary>
    /// Reads a designer file and the companion binary file beside it: <c>.frx</c> for a <c>.frm</c>, <c>.ctx</c> for a <c>.ctl</c>,
    /// and <c>.pgx</c> for a <c>.pag</c>.
    /// </summary>
    /// <param name="path">The designer file's path.</param>
    /// <param name="options">
    /// How the designer file and the strings in its companion are decoded; <see langword="null"/> for <see cref="VB6ImportOptions.Default"/>.
    /// </param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="IOException">The designer file cannot be read.</exception>
    public static VB6FormImport ReadFile(string path, VB6ImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= VB6ImportOptions.Default;

        var text = options.Encoding.GetString(File.ReadAllBytes(path));
        var companionPath = Path.ChangeExtension(path, Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".ctl" => ".ctx",
            ".pag" => ".pgx",
            _ => ".frx",
        });
        return Read(text, File.Exists(companionPath) ? File.ReadAllBytes(companionPath) : null, options);
    }

    [GeneratedRegex("""^"\{(?<id>[0-9A-Fa-f-]{36})\}#(?<version>[^#"]*)#[^"]*"\s*;\s*"(?<file>[^"]*)"$""")]
    private static partial Regex LibraryReference();

    private sealed class Importer(DiagnosticBag diagnostics, Companion? companion)
    {
        private static readonly Dictionary<string, PropertyDefinition> ClientRectangle = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ClientLeft"] = VBProperties.Left,
            ["ClientTop"] = VBProperties.Top,
            ["ClientWidth"] = VBProperties.Width,
            ["ClientHeight"] = VBProperties.Height,
        };

        public FormDefinition? Import(DesignerFile file, DesignerBlock root)
        {
            var rootClass = VBClasses.ByTypeName(root.TypeName);
            if (rootClass is not { Kind: ComponentKind.Root })
            {
                diagnostics.Add(VB6ImportDiagnosticKind.NotAFormRoot, root.Line, root.TypeName);
                return null;
            }

            var rootInstance = new ComponentInstance(rootClass, root.Name, ReadIndex(root));
            foreach (var property in root.Properties)
            {
                if (ClientRectangle.TryGetValue(property.Name, out var axis))
                {
                    SetFromText(rootInstance, axis, property, root.Name);
                }
                else if (rootClass.Property(property.Name) is { } definition
                    && definition != VBProperties.Left && definition != VBProperties.Top
                    && definition != VBProperties.Width && definition != VBProperties.Height)
                {
                    SetFromText(rootInstance, definition, property, root.Name);
                }
                else if (!property.Name.Equals("Index", StringComparison.OrdinalIgnoreCase))
                {
                    // The window's outer rectangle among them: the model holds the client rectangle.
                    rootInstance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                }
            }

            ReadBags(rootInstance, root);
            var form = new FormDefinition(rootInstance);
            ReadLibraries(file, form);

            var ambientFont = rootClass == VBClasses.Form && rootInstance.IsSet(VBProperties.Font) ? rootInstance.Get(VBProperties.Font) : (FormFont?)null;
            ReadContents(rootInstance, root, ambientFont);
            return form;
        }

        private void ReadContents(ComponentInstance container, DesignerBlock block, FormFont? ambientFont)
        {
            foreach (var child in block.Children)
            {
                var componentClass = VBClasses.ByTypeName(child.TypeName);
                if (componentClass is null or { Kind: ComponentKind.Root })
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnmodelledClass, child.Line, child.TypeName, child.Name);
                    KeepAsText(container, child);
                    continue;
                }

                var instance = new ComponentInstance(componentClass, child.Name, ReadIndex(child));
                Place(instance, container, child.Line);

                foreach (var property in child.Properties)
                {
                    if (componentClass.Property(property.Name) is { } definition)
                    {
                        SetFromText(instance, definition, property, instance.ToString());
                    }
                    else if (!property.Name.Equals("Index", StringComparison.OrdinalIgnoreCase))
                    {
                        instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                    }
                }

                ReadBags(instance, child);
                if (ambientFont is { } font && componentClass.Has(VBProperties.Font) && !instance.IsSet(VBProperties.Font))
                {
                    instance.Set(VBProperties.Font, font);
                }

                ReadContents(instance, child, ambientFont);
            }
        }

        // In the component the file places it in, or else in the nearest enclosing component that can hold it; the root holds any
        // control and any menu.
        private void Place(ComponentInstance instance, ComponentInstance writtenIn, int line)
        {
            for (ComponentInstance? container = writtenIn; container is not null; container = container.Container)
            {
                if (CanHold(container, instance.Class))
                {
                    if (container != writtenIn)
                    {
                        diagnostics.Add(VB6ImportDiagnosticKind.MisplacedComponent, line, writtenIn.ToString(), instance.ToString(), container.ToString());
                    }

                    instance.SetContainer(container);
                    return;
                }
            }
        }

        private static bool CanHold(ComponentInstance container, ComponentClass content) => content.Kind switch
        {
            ComponentKind.Menu => container.Class.Kind is ComponentKind.Root or ComponentKind.Menu,
            _ => container.Class.IsContainer,
        };

        private static void KeepAsText(ComponentInstance container, DesignerBlock block) =>
            container.UnmodelledContents.Add(new UnmodelledComponent(block.TypeName, block.Name, container.Contents.Count, block.Text ?? ""));

        private int? ReadIndex(DesignerBlock block)
        {
            var index = block.Properties.LastOrDefault(p => p.Name.Equals("Index", StringComparison.OrdinalIgnoreCase));
            if (index is null)
            {
                return null;
            }

            if (ValueLiteral.Parse(index.Value) is ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= short.MaxValue } number)
            {
                return (int)number.Value;
            }

            diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, index.Line, index.Name, block.Name);
            return null;
        }

        private void ReadLibraries(DesignerFile file, FormDefinition form)
        {
            foreach (var line in file.Header)
            {
                if (!line.Name.Equals("Object", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.MalformedLine, line.Line, $"{line.Name} = {line.Value}");
                    continue;
                }

                var match = LibraryReference().Match(line.Value);
                if (match.Success && Guid.TryParse(match.Groups["id"].Value, out var id))
                {
                    form.ControlLibraries.Add(new ControlLibraryReference(id, match.Groups["version"].Value, match.Groups["file"].Value));
                }
                else
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnreadLibraryReference, line.Line, line.Value);
                }
            }
        }

        private void ReadBags(ComponentInstance instance, DesignerBlock block)
        {
            foreach (var bag in block.Bags)
            {
                if (bag.Name.Equals("Font", StringComparison.OrdinalIgnoreCase) && instance.Class.Has(VBProperties.Font) && ReadFont(bag) is { } font)
                {
                    instance.Set(VBProperties.Font, font);
                    continue;
                }

                if (bag.Name.Equals("Font", StringComparison.OrdinalIgnoreCase) && instance.Class.Has(VBProperties.Font))
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, bag.Line, bag.Name, instance.ToString());
                }

                instance.UnmodelledProperties.Add(new UnmodelledProperty(bag.Name, bag.Text));
            }
        }

        // A Font bag's fields, in any case and any order; a field the bag leaves out keeps the default font's.
        private static FormFont? ReadFont(DesignerBag bag)
        {
            var font = FormFont.Default;
            foreach (var field in bag.Properties)
            {
                var value = ValueLiteral.Parse(field.Value);
                switch (field.Name.ToLowerInvariant(), value)
                {
                    case ("name", ValueLiteral.Text text):
                        font = font with { Name = text.Value };
                        break;
                    case ("size", ValueLiteral.Number { Value: > 0 } size):
                        font = font with { Size = size.Value };
                        break;
                    case ("charset", ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= 255 } charset):
                        font = font with { Charset = (int)charset.Value };
                        break;
                    case ("weight", ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= 1000 } weight):
                        font = font with { Weight = (int)weight.Value };
                        break;
                    case ("underline", ValueLiteral.Number underline):
                        font = font with { Underline = underline.Value != 0 };
                        break;
                    case ("italic", ValueLiteral.Number italic):
                        font = font with { Italic = italic.Value != 0 };
                        break;
                    case ("strikethrough", ValueLiteral.Number strikethrough):
                        font = font with { Strikethrough = strikethrough.Value != 0 };
                        break;
                    default:
                        return null;
                }
            }

            return font;
        }

        private void SetFromText(ComponentInstance instance, PropertyDefinition definition, DesignerProperty property, string component)
        {
            var literal = ValueLiteral.Parse(property.Value);
            if (literal is ValueLiteral.Citation citation)
            {
                if (companion is null)
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.MissingCompanion, property.Line, property.Name, component, citation.FileName);
                    instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                    return;
                }

                if (!TryReadRecord(definition, citation, out var record))
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.MissingRecord, property.Line, property.Name, component,
                        citation.Offset.ToString("X4", System.Globalization.CultureInfo.InvariantCulture), citation.FileName);
                    instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                    return;
                }

                instance.TrySet(definition, record);
                return;
            }

            if (TryConvert(definition.ValueType, literal, out var value) && instance.TrySet(definition, value))
            {
                return;
            }

            diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, property.Line, property.Name, component);
            instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
        }

        private bool TryReadRecord(PropertyDefinition definition, ValueLiteral.Citation citation, out object? record)
        {
            record = null;
            var type = definition.ValueType;
            if (type == typeof(string))
            {
                var read = citation.IsDollar
                    ? companion!.TryReadLongString(citation.Offset, out var text)
                    : companion!.TryReadShortString(citation.Offset, out text);
                record = text;
                return read;
            }

            if (citation.IsDollar)
            {
                return false;
            }

            if (type == typeof(byte[]))
            {
                var read = companion!.TryReadPicture(citation.Offset, out var image);
                record = image;
                return read;
            }

            if (type == typeof(IReadOnlyList<string>))
            {
                var read = companion!.TryReadList(citation.Offset, out var items);
                record = items;
                return read;
            }

            if (type == typeof(IReadOnlyList<int>))
            {
                var read = companion!.TryReadItemData(citation.Offset, out var values);
                record = values;
                return read;
            }

            return false;
        }

        private static bool TryConvert(Type type, ValueLiteral literal, out object? value)
        {
            value = null;
            switch (literal)
            {
                case ValueLiteral.Text text when type == typeof(string):
                    value = text.Value;
                    return true;

                case ValueLiteral.Number number when type == typeof(double):
                    value = number.Value;
                    return true;

                case ValueLiteral.Number { IsIntegral: true } number when type == typeof(int)
                    && number.Value is >= int.MinValue and <= int.MaxValue:
                    value = (int)number.Value;
                    return true;

                case ValueLiteral.Number number when type == typeof(bool):
                    value = number.Value != 0;
                    return true;

                case ValueLiteral.Number { IsIntegral: true } number when type.IsEnum
                    && number.Value is >= int.MinValue and <= int.MaxValue:
                    var member = Enum.ToObject(type, (int)number.Value);
                    value = member;
                    return Enum.IsDefined(type, member);

                case ValueLiteral.Number { IsIntegral: true } number when type == typeof(FormColor)
                    && number.Value is >= int.MinValue and <= int.MaxValue
                    && FormColor.TryFromOle((int)number.Value, out var color):
                    value = color;
                    return true;

                default:
                    return false;
            }
        }
    }
}

/// <summary>
/// How a <see cref="VB6FormReader"/> decodes the text it reads.
/// </summary>
public sealed class VB6ImportOptions
{
    /// <summary>
    /// The options a read takes when it is given none: the Windows-1252 code page, which VB6 writes on Western European systems.
    /// </summary>
    public static VB6ImportOptions Default { get; } = new();

    /// <summary>
    /// The code page a designer file read by <see cref="VB6FormReader.ReadFile"/> and the strings of a companion file are decoded
    /// with: the ANSI code page of the system that wrote them.
    /// </summary>
    public Encoding Encoding { get; init; } = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;
}

/// <summary>
/// What a <see cref="VB6FormReader"/> read from a designer file.
/// </summary>
public sealed class VB6FormImport
{
    internal VB6FormImport(FormDefinition? form, string moduleText, int moduleLine, IReadOnlyList<VB6ImportDiagnostic> diagnostics)
    {
        Form = form;
        ModuleText = moduleText;
        ModuleLine = moduleLine;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The form, or <see langword="null"/> when the file holds none; <see cref="Diagnostics"/> then says why.
    /// </summary>
    public FormDefinition? Form { get; }

    /// <summary>
    /// The part of the file after the designer half: its <c>Attribute</c> lines and its code, with the file's own line endings.
    /// </summary>
    public string ModuleText { get; }

    /// <summary>
    /// The 1-based line of the file that <see cref="ModuleText"/> starts on.
    /// </summary>
    public int ModuleLine { get; }

    /// <summary>
    /// What the import reports, in the order of the lines it is about.
    /// </summary>
    public IReadOnlyList<VB6ImportDiagnostic> Diagnostics { get; }
}
