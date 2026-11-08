namespace RDCore.Forms.Model;

/// <summary>
/// A run-time error a form or a control answers with: VB's error number, and its standard description.
/// </summary>
/// <remarks>
/// A control refuses a value, a member or a call with the error VB raises for it, such as 380 for a value outside a property's range.
/// The code that runs the form raises it in the program, where <c>Err.Number</c> reads <see cref="Number"/>.
/// </remarks>
/// <param name="Number">The error's number, as <c>Err.Number</c> reads it.</param>
/// <param name="Description">The error's description, as <c>Err.Description</c> reads it.</param>
public readonly record struct FormError(int Number, string Description)
{
    /// <summary>
    /// Error 5, "Invalid procedure call or argument".
    /// </summary>
    public static FormError InvalidProcedureCall => new(5, "Invalid procedure call or argument");

    /// <summary>
    /// Error 6, "Overflow".
    /// </summary>
    public static FormError Overflow => new(6, "Overflow");

    /// <summary>
    /// Error 13, "Type mismatch".
    /// </summary>
    public static FormError TypeMismatch => new(13, "Type mismatch");

    /// <summary>
    /// Error 53, "File not found".
    /// </summary>
    public static FormError FileNotFound => new(53, "File not found");

    /// <summary>
    /// Error 94, "Invalid use of Null".
    /// </summary>
    public static FormError InvalidUseOfNull => new(94, "Invalid use of Null");

    /// <summary>
    /// Error 343, "Object not an array".
    /// </summary>
    public static FormError ObjectNotAnArray => new(343, "Object not an array");

    /// <summary>
    /// Error 360, "Object already loaded".
    /// </summary>
    public static FormError ObjectAlreadyLoaded => new(360, "Object already loaded");

    /// <summary>
    /// Error 361, "Can't load or unload this object".
    /// </summary>
    public static FormError CantLoadOrUnloadThisObject => new(361, "Can't load or unload this object");

    /// <summary>
    /// Error 362, "Can't unload controls created at design time".
    /// </summary>
    public static FormError CantUnloadControlsCreatedAtDesignTime => new(362, "Can't unload controls created at design time");

    /// <summary>
    /// Error 380, "Invalid property value".
    /// </summary>
    public static FormError InvalidPropertyValue => new(380, "Invalid property value");

    /// <summary>
    /// Error 381, "Invalid property array index".
    /// </summary>
    public static FormError InvalidPropertyArrayIndex => new(381, "Invalid property array index");

    /// <summary>
    /// Error 382, "Set not supported at runtime": a property a program cannot assign while it runs.
    /// </summary>
    public static FormError SetNotSupportedAtRuntime => new(382, "Set not supported at runtime");

    /// <summary>
    /// Error 383, "Set not supported (read-only property)".
    /// </summary>
    public static FormError SetNotSupportedReadOnly => new(383, "Set not supported (read-only property)");

    /// <summary>
    /// Error 393, "Get not supported at runtime": a property a program cannot read while it runs.
    /// </summary>
    public static FormError GetNotSupportedAtRuntime => new(393, "Get not supported at runtime");

    /// <summary>
    /// Error 425, "Invalid object use".
    /// </summary>
    public static FormError InvalidObjectUse => new(425, "Invalid object use");

    /// <summary>
    /// Error 438, "Object doesn't support this property or method".
    /// </summary>
    public static FormError ObjectDoesntSupportPropertyOrMethod => new(438, "Object doesn't support this property or method");

    /// <summary>
    /// Error 448, "Named argument not found".
    /// </summary>
    public static FormError NamedArgumentNotFound => new(448, "Named argument not found");

    /// <summary>
    /// Error 449, "Argument not optional".
    /// </summary>
    public static FormError ArgumentNotOptional => new(449, "Argument not optional");

    /// <summary>
    /// Error 450, "Wrong number of arguments or invalid property assignment".
    /// </summary>
    public static FormError WrongNumberOfArguments => new(450, "Wrong number of arguments or invalid property assignment");

    /// <summary>
    /// Error 461, "Method or data member not found".
    /// </summary>
    public static FormError MethodOrDataMemberNotFound => new(461, "Method or data member not found");

    /// <summary>
    /// Error 481, "Invalid picture".
    /// </summary>
    public static FormError InvalidPicture => new(481, "Invalid picture");

    /// <summary>
    /// The error's number and description, such as <c>380: Invalid property value</c>.
    /// </summary>
    /// <returns>The error's number and description.</returns>
    public override string ToString() => $"{Number}: {Description}";
}

/// <summary>
/// The exception a form or a control throws to answer with a <see cref="FormError"/>.
/// </summary>
/// <param name="error">The error.</param>
public sealed class FormErrorException(FormError error) : Exception(error.Description)
{
    /// <summary>
    /// The error the form or the control answers with.
    /// </summary>
    public FormError Error { get; } = error;
}
