using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;
using RDCore.SDK.Runtime.Libraries;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Libraries;

/// <summary>
/// The libraries RDForms describes: <c>VB</c>, the library of the VB6 forms and controls the RDForms model implements,
/// <c>MSComctlLib</c>, the library of the Windows Common Controls 6.0 (MSCOMCTL.OCX), <c>MSForms</c>, the Microsoft Forms 2.0 Object
/// Library (FM20.DLL), and <c>RDForms</c>, the library of RDForms' own forms and controls.
/// </summary>
/// <remarks>
/// <para>
/// <c>VB</c> is what <c>rdc export-library</c> reads from VB6.OLB, with each property, method and event of a modelled class that VB6.OLB
/// does not declare: Opacity, a Label's Picture, a control's ContextMenu, and the others <see cref="Additions"/> lists, and App's
/// PlatformVersion and ComponentVersion, which read the platform's versions. Each of these is
/// marked <see cref="MemberDescription.IsAddition"/>. The description keeps VB6.OLB's GUID and version, so a project's reference to VB6.OLB
/// finds it. Every member VB6.OLB declares stays in it, whether the model holds the member or not. A setting of VB6's designer that
/// VB6.OLB declares no member for, such as a UserControl's CanGetFocus, is not in it.
/// </para>
/// <para>
/// A member an addition shares its name with in VB6.OLB, such as a PictureBox's Picture for a Label's, gives the addition its type and its
/// accessors. Another addition takes its type from the model.
/// </para>
/// <para>
/// <c>MSComctlLib</c> is what <c>rdc export-library</c> reads from MSCOMCTL.OCX, as exported: it keeps the OCX's GUID and version, so
/// a project's reference to MSCOMCTL.OCX finds it, and it has no additions.
/// </para>
/// <para>
/// <c>MSForms</c> is what <c>rdc export-library</c> reads from FM20.DLL, as exported: it keeps its GUID and version, so a project's
/// reference to FM20.DLL finds it, and it has no additions.
/// </para>
/// <para>
/// <c>RDForms</c> has a class for each of the model's forms and controls, named as the model names it (<c>RDForms.Form</c>,
/// <c>RDForms.TextBox</c>), and one for each other class a port map ports a legacy class to, such as <c>RDForms.Control</c> and
/// <c>RDForms.DataObject</c>. A class has, in this order and each only where a member of its name is not there already:
/// </para>
/// <list type="bullet">
/// <item>the members and events of the class of <c>VB</c> that VB6's port map ports to it, as <c>VB</c> describes them, its additions
/// included;</item>
/// <item>those of each class of <c>MSForms</c> that MSForms' port map ports to it, under the names that map gives them, and, for a control
/// a form's <c>Controls.Add</c> creates, those of MSForms' <c>Control</c>, where a control's extender members are;</item>
/// <item>the model's methods, the properties a program reaches and the model's events, typed as <c>VB</c> types its additions.</item>
/// </list>
/// <para>
/// A class that no legacy class ports to takes the model's default member as its own. <c>RDForms</c> declares MSForms' enumerations by
/// their names and values (<c>fmScrollAction</c>, <c>fmScrollActionNoChange</c>), so their members are constants a program names as it names
/// MSForms'. A type of MSForms takes the type that MSForms' port map ports it to: a class of RDForms, or the type of a wrapper's value, as
/// <c>ReturnBoolean</c>'s <c>Boolean</c>. A type of <c>VB</c> or of <c>MSForms</c> whose name <c>RDForms</c> does not declare is qualified with
/// its library (<c>MSForms.IFont</c>), and <c>RDForms</c> depends on <c>VB</c>, on <c>MSForms</c>, and on what they depend on. No type library
/// describes <c>RDForms</c>, so it has no GUID, and nothing in it is marked an addition.
/// </para>
/// </remarks>
public sealed class RDFormsLibraries : ILibrarySource
{
    /// <summary>
    /// The name of the library of VB6's forms and controls.
    /// </summary>
    public const string VB = "VB";

    /// <summary>
    /// The name of the library of the Windows Common Controls 6.0 (MSCOMCTL.OCX).
    /// </summary>
    public const string MSComctlLib = "MSComctlLib";

    /// <summary>
    /// The name of the Microsoft Forms 2.0 Object Library (FM20.DLL).
    /// </summary>
    public const string MSForms = "MSForms";

    /// <summary>
    /// The name of the library of RDForms' own forms and controls.
    /// </summary>
    public const string RDForms = "RDForms";

    /// <summary>
    /// The libraries RDForms describes. A description is built once, the first time it is asked for.
    /// </summary>
    public static RDFormsLibraries Instance { get; } = new();

    private readonly Lazy<LibraryDescription> exportedVB = new(() => Exported(VB));

    private readonly Lazy<LibraryDescription> vb;

    private readonly Lazy<LibraryDescription> commonControls = new(() => Exported(MSComctlLib));

    private readonly Lazy<LibraryDescription> msforms = new(() => Exported(MSForms));

    private readonly Lazy<LibraryDescription> rdforms;

    private RDFormsLibraries()
    {
        vb = new(() => DescribeVB(exportedVB.Value));
        rdforms = new(() => DescribeRDForms(vb.Value, exportedVB.Value, msforms.Value));
    }

    /// <inheritdoc/>
    public IEnumerable<string> Names => [VB, MSComctlLib, MSForms, RDForms];

    /// <inheritdoc/>
    public bool TryGet(string name, [NotNullWhen(true)] out LibraryDescription? description)
    {
        description = string.Equals(name, VB, StringComparison.OrdinalIgnoreCase) ? vb.Value
            : string.Equals(name, MSComctlLib, StringComparison.OrdinalIgnoreCase) ? commonControls.Value
            : string.Equals(name, MSForms, StringComparison.OrdinalIgnoreCase) ? msforms.Value
            : string.Equals(name, RDForms, StringComparison.OrdinalIgnoreCase) ? rdforms.Value
            : null;
        return description is not null;
    }

    /// <summary>
    /// What RDForms adds to VB6.OLB's <c>VB</c>: each member and event of a modelled class that VB6.OLB does not declare, as
    /// <c>Class.Member</c>, in the order of VB6.OLB's classes and then of the model's members.
    /// </summary>
    public IEnumerable<string> Additions =>
        vb.Value.Classes.SelectMany(declared => declared.Members.Where(member => member.IsAddition).Select(member => member.Name)
            .Concat(declared.Events.Where(raised => raised.IsAddition).Select(raised => raised.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => $"{declared.Name}.{name}"));

    // A library's description as rdc export-library wrote it, from the embedded <name>.json.
    private static LibraryDescription Exported(string name)
    {
        using var stream = typeof(RDFormsLibraries).Assembly.GetManifestResourceStream($"{typeof(RDFormsLibraries).Namespace}.{name}.json")
            ?? throw new InvalidOperationException($"RDCore.Forms holds no {name}.json.");
        using var reader = new StreamReader(stream);
        return LibraryJson.Read(reader.ReadToEnd());
    }

    private static LibraryDescription DescribeVB(LibraryDescription exported) => exported with
    {
        FormatVersion = LibraryJson.CurrentFormatVersion,
        Classes = [.. exported.Classes.Select(declared => WithAdditions(declared, exported))],
    };

    private static LibraryDescription DescribeRDForms(LibraryDescription vb, LibraryDescription exported, LibraryDescription msforms)
    {
        var classNames = ComponentClasses.All.Select(modelled => modelled.ClassName)
            .Concat(new[] { FormsPortMap.VB6, FormsPortMap.MSForms }.SelectMany(map => map.Types).SelectMany(TargetsOf))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var own = classNames.Concat(msforms.Enums.Select(declared => declared.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // A legacy type RDForms declares none of the name of, by its library: VB's where both have one.
        var legacyTypes = vb.Classes.Select(declared => (declared.Name, Library: VB)).Concat(vb.Enums.Select(declared => (declared.Name, Library: VB)))
            .Concat(msforms.Classes.Select(declared => (declared.Name, Library: MSForms)))
            .Where(type => !own.Contains(type.Name))
            .DistinctBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(type => type.Name, type => type.Library, StringComparer.OrdinalIgnoreCase);
        string Qualified(string type) => !type.Contains('.') && legacyTypes.TryGetValue(type, out var library) ? $"{library}.{type}" : type;
        ParameterDescription QualifiedParameter(ParameterDescription parameter) => parameter with { Type = Qualified(parameter.Type) };
        ClassDescription Owned(ClassDescription declared) => declared with
        {
            IsAddition = false,
            Members = [.. declared.Members.Select(member => member with
            {
                IsAddition = false,
                Type = member.Type is { } type ? Qualified(type) : null,
                Parameters = [.. member.Parameters.Select(QualifiedParameter)],
            })],
            Events = [.. declared.Events.Select(raised => raised with { IsAddition = false, Parameters = [.. raised.Parameters.Select(QualifiedParameter)] })],
        };

        return new LibraryDescription
        {
            Name = RDForms,
            Description = "RDForms forms and controls",
            DependsOn = [.. new[] { LibraryIdentity.Of(vb) }.Concat(vb.DependsOn).Append(LibraryIdentity.Of(msforms)).Concat(msforms.DependsOn)
                .DistinctBy(dependency => dependency.Name, StringComparer.OrdinalIgnoreCase)],
            Enums = [.. msforms.Enums.Select(declared => declared with { IsAddition = false })],
            Classes = [.. classNames.Select(name => Owned(OwnClass(name, vb, exported, msforms)))],
        };
    }

    // The RDForms classes a port map ports a legacy class to, by their names in RDForms: both scroll bars, for MSForms' ScrollBar.
    private static IEnumerable<string> TargetsOf(TypePort port) => port.Kind != TypePortKind.Class ? []
        : port.ByOrientation is { } byOrientation ? [Unqualified(byOrientation.Horizontal), Unqualified(byOrientation.Vertical)]
        : [Unqualified(port.RDFormsName)];

    private static string Unqualified(string rdformsName) =>
        rdformsName.StartsWith(RDForms + ".", StringComparison.OrdinalIgnoreCase) ? rdformsName[(RDForms.Length + 1)..] : rdformsName;

    private static ClassDescription? ClassOf(LibraryDescription library, string name) =>
        library.Classes.FirstOrDefault(declared => string.Equals(declared.Name, name, StringComparison.OrdinalIgnoreCase));

    // A class of RDForms: what VB's class has, then what MSForms' classes have under the names the port gives them, then what the model's class
    // has; neither legacy library's default interface, which RDForms does not declare.
    private static ClassDescription OwnClass(string name, LibraryDescription vb, LibraryDescription exported, LibraryDescription msforms)
    {
        bool PortsTo(TypePort port) => TargetsOf(port).Contains(name, StringComparer.OrdinalIgnoreCase);
        var fromVB = FormsPortMap.VB6.Types.Where(PortsTo).Select(port => ClassOf(vb, port.LegacyName)).OfType<ClassDescription>().FirstOrDefault();
        var fromMSForms = FormsPortMap.MSForms.Types.Where(PortsTo).Select(port => ClassOf(msforms, port.LegacyName)).OfType<ClassDescription>().ToList();
        var extender = ClassOf(msforms, FormsPortMap.ExtenderClass);
        var modelled = ComponentClasses.All.FirstOrDefault(candidate => string.Equals(candidate.ClassName, name, StringComparison.OrdinalIgnoreCase));

        var head = fromVB ?? fromMSForms.FirstOrDefault();
        var members = fromVB?.Members.ToList() ?? [];
        var events = fromVB?.Events.ToList() ?? [];
        // A control Controls.Add creates has its extender's members, which MSForms declares on Control.
        var legacy = fromMSForms.SelectMany(declared => extender is not null && IsCreated(declared.Name) ? new[] { declared, extender } : [declared]).Distinct();
        foreach (var declared in legacy)
            AddFromMSForms(declared, keepsDispIds: ReferenceEquals(declared, head), members, events);

        if (modelled is not null)
            AddFromModel(modelled, members, events, exported, defaultMember: head is null);

        var described = head ?? new ClassDescription();
        return described with { Name = name, DefaultInterface = null, IsDefaultInterfaceHidden = false, Members = [.. members], Events = [.. events] };
    }

    private static bool IsCreated(string msformsClass) => FormsPortMap.MSForms.ProgIdOf($"Forms.{msformsClass}.1") is not null;

    // A class of MSForms' members and events, under the names MSForms' port map gives them, each where its class has none of the name; a member's
    // dispatch identifier where the class takes its default member from this one.
    private static void AddFromMSForms(ClassDescription declared, bool keepsDispIds, List<MemberDescription> members, List<EventDescription> events)
    {
        var known = members.Select(member => member.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var accessors in declared.Members.GroupBy(member => member.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = FormsPortMap.MSForms.MemberOf(declared.Name, accessors.Key)?.RDFormsName ?? accessors.Key;
            if (!known.Add(name))
                continue;

            members.AddRange(accessors.Select(member => member with
            {
                Name = name,
                DispId = keepsDispIds ? member.DispId : null,
                Type = member.Type is { } type ? MSFormsType(type) : null,
                Parameters = [.. member.Parameters.Select(parameter => parameter with { Type = MSFormsType(parameter.Type) })],
            }));
        }

        var raised = events.Select(known => known.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var legacyEvent in declared.Events)
        {
            var ported = FormsPortMap.MSForms.EventOf(declared.Name, legacyEvent.Name);
            var portedEvent = ported is null
                ? legacyEvent with { Parameters = [.. legacyEvent.Parameters.Select(parameter => parameter with { Type = MSFormsType(parameter.Type) })] }
                : legacyEvent with
                {
                    Name = ported.RDFormsName,
                    Parameters = [.. ported.Parameters.Select(parameter => Parameter(parameter.Name, Unqualified(parameter.RDFormsType), byVal: !parameter.ByRef))],
                };
            if (raised.Add(portedEvent.Name))
                events.Add(portedEvent);
        }
    }

    // A type of MSForms, as RDForms has it: the type MSForms' port map ports it to; an enumeration, RDForms' of its name; another, as MSForms names it.
    private static string MSFormsType(string type)
    {
        var name = type.StartsWith(MSForms + ".", StringComparison.OrdinalIgnoreCase) ? type[(MSForms.Length + 1)..] : type;
        if (name.Contains('.'))
            return type;

        return FormsPortMap.MSForms.TypeOf(name) switch
        {
            { Kind: TypePortKind.Class, ByOrientation: not null } => FormsPortMap.ExtenderClass,
            { } port => Unqualified(port.RDFormsName),
            null => name,
        };
    }

    // What the model's class has that the class has none of the name of: its methods first, as a running program reaches one where a property
    // has its name, then the properties a program reaches, then its events. A class that no legacy class ports to takes the model's default
    // member as its own, as a type library marks it.
    private static void AddFromModel(ComponentClass modelled, List<MemberDescription> members, List<EventDescription> events, LibraryDescription exported,
        bool defaultMember)
    {
        var known = members.Select(member => member.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var settings = DesignerSettings.GetValueOrDefault(modelled.ClassName) ?? [];
        var added = new List<MemberDescription>();
        foreach (var method in modelled.Methods.Where(method => known.Add(method.Name)))
            added.AddRange(MethodMembers(method, exported));
        foreach (var property in modelled.Properties.Where(property => !modelled.IsNotAvailableAtRunTime(property)
            && !settings.Contains(property.Name, StringComparer.OrdinalIgnoreCase) && known.Add(property.Name)))
            added.AddRange(PropertyAccessors(property, modelled.IsReadOnlyAtRunTime(property), exported));
        members.AddRange(defaultMember
            ? added.Select(member => string.Equals(member.Name, modelled.DefaultMember, StringComparison.OrdinalIgnoreCase) ? member with { DispId = 0 } : member)
            : added);

        var raised = events.Select(known => known.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        events.AddRange(modelled.Events.Where(modelledEvent => raised.Add(modelledEvent.Name)).Select(modelledEvent => new EventDescription
        {
            Name = modelledEvent.Name,
            Parameters = [.. modelledEvent.Parameters.Select(parameter => Parameter(parameter.Name, parameter.TypeName))],
        }));
    }

    // A class with what its modelled classes have and VB6.OLB does not declare, then what RD-VBA adds to it; any other class is as exported.
    private static ClassDescription WithAdditions(ClassDescription declared, LibraryDescription exported)
    {
        var modelled = ModelledAs(declared.Name);
        var platform = PlatformAdditions.GetValueOrDefault(declared.Name) ?? [];
        if (modelled.Count == 0 && platform.Length == 0)
            return declared;

        var members = declared.Members.ToList();
        var events = declared.Events.ToList();
        bool Declared(string name) => members.Any(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));
        // A method the window answers for comes first: a running program reaches it where a model property has its name.
        foreach (var modelledClass in modelled)
        {
            foreach (var method in modelledClass.Methods.Where(method => !Declared(method.Name)))
                members.AddRange(Added(MethodMembers(method, exported)));
            var settings = DesignerSettings.GetValueOrDefault(declared.Name) ?? [];
            foreach (var property in modelledClass.Properties.Where(property => !modelledClass.IsNotAvailableAtRunTime(property) && !Declared(property.Name)
                && !settings.Contains(property.Name, StringComparer.OrdinalIgnoreCase)))
                members.AddRange(Added(PropertyAccessors(property, modelledClass.IsReadOnlyAtRunTime(property), exported)));
            foreach (var raised in modelledClass.Events.Where(raised => !events.Any(known => string.Equals(known.Name, raised.Name, StringComparison.OrdinalIgnoreCase))))
                events.Add(new EventDescription { Name = raised.Name, IsAddition = true, Parameters = [.. raised.Parameters.Select(parameter => Parameter(parameter.Name, parameter.TypeName))] });
        }
        // Asked of the members before any is added, so that a property's Let follows its Get.
        members.AddRange(Added(platform.Where(member => !Declared(member.Name)).ToList()));
        return declared with { Members = [.. members], Events = [.. events] };
    }

    // What RD-VBA adds to a class of VB6.OLB that no model class implements: App's PlatformVersion and ComponentVersion(Name), read-only, which give the
    // platform's version and that of one of its components ("RDCore", "RDCore.SDK", "RDCore.Forms"), and its KeepSystemTheme, read and written, whether
    // the program's forms take the system's theme variant (True) or stay light (False).
    private static readonly Dictionary<string, MemberDescription[]> PlatformAdditions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["App"] =
        [
            new MemberDescription { Name = "PlatformVersion", Kind = MemberKind.PropertyGet, Type = "String" },
            new MemberDescription { Name = "ComponentVersion", Kind = MemberKind.PropertyGet, Type = "String", Parameters = [Parameter("Name", "String", byVal: true)] },
            new MemberDescription { Name = "KeepSystemTheme", Kind = MemberKind.PropertyGet, Type = "Boolean" },
            new MemberDescription { Name = "KeepSystemTheme", Kind = MemberKind.PropertyLet, Parameters = [Parameter("Value", "Boolean", byVal: true)] },
        ],
    };

    // What VB6's designer sets on a root that VB6.OLB declares no member for, and that RD-VBA adds none for: VB6's documentation has each of a
    // UserControl's "not available at run time", and a UserControl's or a PropertyPage's place is its extender's Left and Top.
    private static readonly Dictionary<string, string[]> DesignerSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UserControl"] = ["Alignable", "CanGetFocus", "ControlContainer", "DataBindingBehavior", "DataSourceBehavior", "DefaultCancel", "EditAtDesignTime",
            "ForwardFocus", "InvisibleAtRuntime", "Public", "ToolboxBitmap", "Windowless", "Left", "Top"],
        ["PropertyPage"] = ["Left", "Top"],
    };

    // The mark, in the one place every added member takes it.
    private static IEnumerable<MemberDescription> Added(IEnumerable<MemberDescription> members) => members.Select(member => member with { IsAddition = true });

    // The model's classes a class of VB6.OLB describes, as VB6's port map names them: VBControlExtender is the extender of every placed UserControl.
    private static IReadOnlyList<ComponentClass> ModelledAs(string name) =>
        string.Equals(name, "VBControlExtender", StringComparison.OrdinalIgnoreCase)
            ? [FullestExtender.Value]
            : FormsPortMap.VB6.TypeOf(name) is { Kind: TypePortKind.Class } port && ComponentClasses.ByTypeName(port.RDFormsName) is { } modelled ? [modelled] : [];

    // The extender of a UserControl whose settings give a placed control every extender member there is.
    private static readonly Lazy<ComponentClass> FullestExtender = new(() => UserControlClasses.Of(
        new FormDefinition(new ComponentInstance(ComponentClasses.UserControl, "UserControl1")
            .Set(P.InvisibleAtRuntime, false).Set(P.CanGetFocus, true).Set(P.ForwardFocus, false).Set(P.DefaultCancel, true).Set(P.Alignable, true)),
        "Project1"));

    // A property's accessors: those of a member of its name on another of VB6.OLB's classes, or a Property Get with a Property Let (or Set) of the
    // model's type.
    private static IEnumerable<MemberDescription> PropertyAccessors(PropertyDefinition property, bool readOnly, LibraryDescription exported)
    {
        if (SharedAccessors(property.Name, exported) is { } shared)
            return shared.Where(accessor => !readOnly || accessor.Kind == MemberKind.PropertyGet).Select(accessor => accessor with { Name = property.Name, DispId = null });

        var type = TypeOf(property.ValueType);
        var get = new MemberDescription { Name = property.Name, Kind = MemberKind.PropertyGet, Type = type };
        return readOnly ? [get] : [get, new MemberDescription { Name = property.Name, Kind = MemberKind.PropertyLet, Parameters = [Parameter("Value", type, byVal: true)] }];
    }

    // The accessors of a property of the name on the first of VB6.OLB's classes that declares one, or null where none does.
    private static List<MemberDescription>? SharedAccessors(string name, LibraryDescription exported) =>
        exported.Classes.Select(declared => declared.Members.Where(member => member.Kind != MemberKind.Method
                && string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)).ToList())
            .FirstOrDefault(accessors => accessors.Count > 0);

    // A method, or a property the window answers for, as code calls it. A property whose value the window types as a property of VB6.OLB
    // (ItemIcon's Picture) has that property's accessors and type, after its own parameters: VB6's Picture is a stdole.StdPicture that is
    // read, let and set.
    private static IEnumerable<MemberDescription> MethodMembers(MethodDefinition method, LibraryDescription exported)
    {
        var parameters = method.Parameters.Select(parameter => Parameter(parameter.Name, parameter.TypeName, optional: parameter.IsOptional)).ToImmutableArray();
        if (method.Value is not { } value)
            return [new MemberDescription { Name = method.Name, Kind = MemberKind.Method, Parameters = parameters }];

        if (SharedAccessors(value.TypeName, exported) is { } shared)
            return shared.Where(accessor => !method.IsReadOnly || accessor.Kind == MemberKind.PropertyGet).Select(accessor => accessor with
            {
                Name = method.Name,
                DispId = null,
                Description = null,
                Parameters = [.. parameters, .. accessor.Parameters],
            });

        var get = new MemberDescription { Name = method.Name, Kind = MemberKind.PropertyGet, Type = value.TypeName, Parameters = parameters };
        return method.IsReadOnly
            ? [get]
            : [get, new MemberDescription { Name = method.Name, Kind = MemberKind.PropertyLet, Parameters = [.. parameters, Parameter(value.Name, value.TypeName, byVal: true)] }];
    }

    private static ParameterDescription Parameter(string name, string type, bool byVal = false, bool optional = false) =>
        new() { Name = name, Type = type, IsByVal = byVal, IsOptional = optional };

    // The VB type of a model property's values, for an addition VB6.OLB has no member of the name of.
    private static string TypeOf(Type valueType) => (Nullable.GetUnderlyingType(valueType) ?? valueType) switch
    {
        var type when type == typeof(bool) => "Boolean",
        var type when type == typeof(byte) => "Byte",
        var type when type == typeof(short) => "Integer",
        var type when type == typeof(int) => "Long",
        var type when type == typeof(float) => "Single",
        var type when type == typeof(double) => "Double",
        var type when type == typeof(string) => "String",
        var type when type == typeof(FormColor) => "OLE_COLOR",
        var type when type.IsEnum => "Long",
        _ => "Variant",
    };
}
