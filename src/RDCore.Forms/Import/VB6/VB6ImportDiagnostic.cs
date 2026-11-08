using System.Globalization;
using System.Resources;

namespace RDCore.Forms.Import.VB6;

/// <summary>
/// What a VB6 import reports about a line of a designer file.
/// </summary>
public enum VB6ImportDiagnosticKind
{
    /// <summary>
    /// The file has no Begin block, so it holds no form.
    /// </summary>
    NoDesignerBlock,

    /// <summary>
    /// The file's root block is not a Form, a UserControl or a PropertyPage.
    /// </summary>
    NotAFormRoot,

    /// <summary>
    /// A line of the designer half is neither a block's Begin or End, nor a property.
    /// </summary>
    MalformedLine,

    /// <summary>
    /// A Begin or BeginProperty has no matching End or EndProperty, or the reverse.
    /// </summary>
    UnbalancedBlock,

    /// <summary>
    /// A header line names a control library by something other than its type library's id, such as a project of the same group.
    /// </summary>
    UnreadLibraryReference,

    /// <summary>
    /// A component's class is not one the model has, so the component is kept as text.
    /// </summary>
    UnmodelledClass,

    /// <summary>
    /// A component is inside a component that cannot hold it, such as a control inside a ListBox, so it is placed in the nearest
    /// enclosing component that can, as VB6 places it, with its position as written.
    /// </summary>
    MisplacedComponent,

    /// <summary>
    /// A property's value is not one the property can hold, so the property is kept as text.
    /// </summary>
    InvalidValue,

    /// <summary>
    /// A property cites the companion binary file, and there is none.
    /// </summary>
    MissingCompanion,

    /// <summary>
    /// A property cites a record the companion binary file does not hold, or holds in a shape the property cannot read.
    /// </summary>
    MissingRecord,
}

/// <summary>
/// How much a <see cref="VB6ImportDiagnostic"/> costs the import.
/// </summary>
public enum VB6ImportSeverity
{
    /// <summary>
    /// The import kept something it does not model as text.
    /// </summary>
    Information,

    /// <summary>
    /// The import left out or kept as text something the file meant to set.
    /// </summary>
    Warning,

    /// <summary>
    /// The file holds no form the import can read.
    /// </summary>
    Error,
}

/// <summary>
/// One thing a VB6 import reports about the file it read.
/// </summary>
/// <param name="Kind">What is reported.</param>
/// <param name="Line">The 1-based line of the designer file it is about.</param>
/// <param name="Message">The report, in the current UI culture.</param>
public sealed record VB6ImportDiagnostic(VB6ImportDiagnosticKind Kind, int Line, string Message)
{
    /// <summary>
    /// How much the reported thing costs the import.
    /// </summary>
    public VB6ImportSeverity Severity => Kind switch
    {
        VB6ImportDiagnosticKind.NoDesignerBlock or VB6ImportDiagnosticKind.NotAFormRoot => VB6ImportSeverity.Error,
        VB6ImportDiagnosticKind.UnmodelledClass or VB6ImportDiagnosticKind.UnreadLibraryReference => VB6ImportSeverity.Information,
        _ => VB6ImportSeverity.Warning,
    };

    /// <summary>
    /// The line, the severity and the message.
    /// </summary>
    /// <returns>The diagnostic as text.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Line}: {Severity}: {Message}");
}

// The diagnostics of one import, each message taken from the resources in the current UI culture.
internal sealed class DiagnosticBag
{
    private static readonly ResourceManager Messages =
        new("RDCore.Forms.Import.VB6.VB6ImportMessages", typeof(DiagnosticBag).Assembly);

    private readonly List<VB6ImportDiagnostic> diagnostics = [];

    public IReadOnlyList<VB6ImportDiagnostic> Items => diagnostics;

    public bool HasErrors => diagnostics.Any(d => d.Severity == VB6ImportSeverity.Error);

    public void Add(VB6ImportDiagnosticKind kind, int line, params object?[] arguments)
    {
        var format = Messages.GetString(kind.ToString(), CultureInfo.CurrentUICulture) ?? kind.ToString();
        diagnostics.Add(new VB6ImportDiagnostic(kind, line, string.Format(CultureInfo.CurrentCulture, format, arguments)));
    }
}
