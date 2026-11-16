namespace RDCore.Forms.Porting;

/// <summary>
/// The unit a legacy library's code uses for the places and sizes of controls and for mouse coordinates.
/// </summary>
/// <remarks>
/// RDForms uses the device-independent pixel (DIP, 1/96 inch) for both. One DIP is 15 twips, and 0.75 point.
/// </remarks>
public enum LegacyUnit
{
    /// <summary>
    /// Twips (1/1440 inch), VB6's unit. A VB6 control's place is in its container's <c>ScaleMode</c>, twips by default: see
    /// <see cref="PortUnit.ContainerLength"/>.
    /// </summary>
    Twips,

    /// <summary>
    /// Points (1/72 inch), MSForms' unit.
    /// </summary>
    Points,
}
