using System.Runtime.CompilerServices;

namespace RDCore.Forms.Model;

/// <summary>
/// A placed UserControl's site: the placed control and its own copy of its class's UserControl, as the UserControl's code sees them.
/// </summary>
/// <remarks>
/// <para>
/// Every placed UserControl has one, an InvisibleAtRuntime one too, made the first time <see cref="Of"/> is asked for it and kept for
/// as long as the placed control lives. A placed control's view shows its site's <see cref="UserControl"/>.
/// </para>
/// <para>
/// No event is raised through the site: a placed UserControl raises none of its own events (Initialize, Resize, Show, Paint, Terminate)
/// and none of its focus events. The site has no <c>Ambient</c> and no <c>PropertyBag</c>.
/// </para>
/// </remarks>
public sealed class UserControlSite
{
    // Each placed control's site, for as long as the placed control lives.
    private static readonly ConditionalWeakTable<ComponentInstance, UserControlSite> sites = [];

    private UserControlSite(ComponentInstance placed, FormDefinition definition)
    {
        Extender = placed;
        UserControl = definition.Copy();
    }

    /// <summary>
    /// The site of a placed UserControl, made the first time it is asked for.
    /// </summary>
    /// <param name="placed">The placed UserControl: a component of a class <see cref="UserControlClasses.Of"/> made.</param>
    /// <returns>The same site for the same placed control, each time.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="placed"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="placed"/>'s class was not made by <see cref="UserControlClasses.Of"/>.</exception>
    public static UserControlSite Of(ComponentInstance placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var definition = UserControlClasses.DefinitionOf(placed.Class)
                         ?? throw new ArgumentException($"{placed.Class.TypeName} is not a placed UserControl's class.", nameof(placed));
        return sites.GetValue(placed, p => new UserControlSite(p, definition));
    }

    /// <summary>
    /// The control the form places: what VB calls the UserControl's <c>Extender</c>, with the members its settings give it.
    /// </summary>
    public ComponentInstance Extender { get; }

    /// <summary>
    /// The placed control's own copy of its class's UserControl (<see cref="FormDefinition.Copy"/>): a change to it leaves the class's
    /// UserControl and the other placed controls as they are.
    /// </summary>
    public FormDefinition UserControl { get; }
}
