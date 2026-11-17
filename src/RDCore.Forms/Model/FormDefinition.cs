// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Globalization;
using RDCore.Forms.Model;

namespace RDCore.Forms.Model;

/// <summary>
/// A form's designer: its root component, with every control and menu item placed in it, and what the form declares about itself.
/// </summary>
/// <remarks>
/// The form's code is not part of its definition. A control's position and size are in pixels (device-independent pixels, 1/96
/// inch), whatever its container's <c>ScaleMode</c>; a program's code reads them in its own unit (see <see cref="CodeUnits"/>). A VB
/// Line's ends are in its container's scale: twips, unless its container (a form, a UserControl, a PropertyPage or a PictureBox) has a
/// <c>ScaleMode</c> of its own. The root's own position and size are its client area's, in pixels.
/// </remarks>
public sealed class FormDefinition
{
    // The controls added at run time and not removed, in the order they were added.
    private readonly List<ComponentInstance> added = [];

    /// <summary>
    /// Creates the definition of a form whose root is <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The form's root component: a form, a UserControl or a PropertyPage.</param>
    /// <exception cref="ArgumentException"><paramref name="root"/> is not of a root class, or is placed in a container.</exception>
    public FormDefinition(ComponentInstance root)
    {
        if (root.Class.Kind != ComponentKind.Root)
            throw new ArgumentException($"{root.Class.TypeName} is not the class of a form's root.", nameof(root));
        if (root.Container is not null)
            throw new ArgumentException($"{root.Name} is placed in {root.Container.Name}.", nameof(root));
        Root = root;
    }

    /// <summary>
    /// The form's root component.
    /// </summary>
    public ComponentInstance Root { get; }

    /// <summary>
    /// The form's name: its root's.
    /// </summary>
    public string Name => Root.Name;

    /// <summary>
    /// The control libraries the form's file declares, in its order: the ActiveX controls its components may be instances of.
    /// </summary>
    public List<ControlLibraryReference> ControlLibraries { get; } = [];

    /// <summary>
    /// The form's root, then every component placed in it, depth first in the order the form's file holds them.
    /// </summary>
    public IEnumerable<ComponentInstance> Components => Root.Descendants().Prepend(Root);

    /// <summary>
    /// The form's component of a given name and control-array index, the name compared without regard to case.
    /// </summary>
    /// <param name="name">The component's name.</param>
    /// <param name="index">The component's index in its control array, or <see langword="null"/> for a component not in one.</param>
    /// <returns>The component, or <see langword="null"/> when the form has none of that name and index.</returns>
    public ComponentInstance? Find(string name, int? index = null) =>
        Components.FirstOrDefault(c => c.Index == index && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Raised after <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/> places a control in the form.
    /// </summary>
    public event EventHandler<FormControlEventArgs>? ControlAdded;

    /// <summary>
    /// Raised after <see cref="RemoveControl(ComponentInstance)"/> takes a control, and what it holds, out of the form.
    /// </summary>
    public event EventHandler<FormControlEventArgs>? ControlRemoved;

    /// <summary>
    /// The form's controls and menu items as a program's <c>Controls</c> collection holds them: those of the form's file, depth first
    /// in its order, then those added at run time, in the order they were added.
    /// </summary>
    public IReadOnlyList<ComponentInstance> Controls =>
        [.. Root.Descendants().Where(c => !added.Contains(c)), .. added];

    /// <summary>
    /// Whether a component was added by <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/> and not removed since.
    /// </summary>
    /// <param name="component">The component.</param>
    /// <returns><see langword="true"/> for a control added at run time.</returns>
    public bool IsAddedAtRunTime(ComponentInstance component) => added.Contains(component);

    /// <summary>
    /// Makes a copy of the form, which has components of its own: each one a copy of one of this form's, with its class, name, index,
    /// the values set on it and what it keeps as text, in the same container at the same place. A copy of a control added at run
    /// time is one the copy added at run time. The copy declares the same control libraries.
    /// </summary>
    /// <returns>The copy. A change to it, or to one of its components, leaves this form as it is, and the other way round.</returns>
    /// <remarks>
    /// Each UserControl a form places has a copy of its UserControl's definition, so that its controls are its own.
    /// </remarks>
    public FormDefinition Copy()
    {
        var copies = new Dictionary<ComponentInstance, ComponentInstance>(ReferenceEqualityComparer.Instance);
        foreach (var component in Components)
        {
            var copy = new ComponentInstance(component.Class, component.Name, component.Index);
            foreach (var (property, value) in component.SetValues)
                copy.TryRead(property, value);
            copy.UnmodelledProperties.AddRange(component.UnmodelledProperties);
            copy.UnmodelledContents.AddRange(component.UnmodelledContents);
            if (component.Container is { } container)
                copy.SetContainer(copies[container]);
            copies.Add(component, copy);
        }
        var form = new FormDefinition(copies[Root]);
        form.ControlLibraries.AddRange(ControlLibraries);
        form.added.AddRange(added.Select(control => copies[control]));
        return form;
    }

    /// <summary>
    /// Adds a control of an RDForms class to the form, as a program's <c>Controls.Add</c> does: the class named by its ProgID, such
    /// as <c>RDForms.CommandButton</c>, without regard to case.
    /// </summary>
    /// <param name="progId">The class's ProgID.</param>
    /// <param name="name">The control's name.</param>
    /// <param name="container">The component to place it in: the root, or a container control of the form. <see langword="null"/>
    /// places it in the root.</param>
    /// <returns>The control added.</returns>
    /// <exception cref="FormErrorException">
    /// No RDForms class has that ProgID: 711, Invalid class string. Otherwise as
    /// <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="container"/> is not a component of this form.</exception>
    public ComponentInstance AddControl(string progId, string name, ComponentInstance? container = null)
    {
        ArgumentNullException.ThrowIfNull(progId);
        var type = ComponentClasses.All.FirstOrDefault(c => string.Equals(c.TypeName, progId, StringComparison.OrdinalIgnoreCase))
                   ?? throw new FormErrorException(FormError.InvalidClassString(progId));
        return AddControl(type, name, container);
    }

    /// <summary>
    /// Adds a control to the form, as a program's <c>Controls.Add</c> does, and raises <see cref="ControlAdded"/>.
    /// </summary>
    /// <param name="type">The control's class.</param>
    /// <param name="name">The control's name.</param>
    /// <param name="container">The component to place it in: the root, or a container control of the form. <see langword="null"/>
    /// places it in the root.</param>
    /// <returns>The control added.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="type"/> is a menu's or a root's class: 720, not a valid control type. <paramref name="name"/> is empty or
    /// white space: 728, not a legal control name. A component of the form other than its root has the name, compared without regard
    /// to case: 727. <paramref name="container"/> holds no controls, or none of the class: 721, not a valid control container. A page goes in
    /// a MultiPage only, and a MultiPage holds pages only (<see cref="ComponentClasses.CanHold"/>).
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="container"/> is not a component of this form.</exception>
    /// <remarks>
    /// <para>
    /// The control is placed last in its container, so that it is under the controls already there. It is hidden, at 0, 0, and 80
    /// by 32 pixels (1200 by 480 twips) where its class has an extent. Its <c>TabIndex</c>, where its class has one, is the next
    /// after the form's others. Its other properties have their class's defaults, so that a caption or a text is "".
    /// </para>
    /// <para>
    /// A page is shown, as a MultiPage shows its selected page. A MultiPage starts with two pages, captioned as they are named:
    /// <c>Page1</c> and <c>Page2</c>, or the next names no component of the form has.
    /// </para>
    /// <para>
    /// Any other name is accepted, even one that starts with a digit, holds a space, is a keyword, or is the form's own name.
    /// </para>
    /// </remarks>
    public ComponentInstance AddControl(ComponentClass type, string name, ComponentInstance? container = null) =>
        AddControl(type, name, container, -1);

    // AddControl, at index among the container's contents (last where it is negative). A page is shown, and a MultiPage starts with two
    // of them, Page1 and Page2 or the next names the form has free, as MSForms gives one; they are among the controls added.
    internal ComponentInstance AddControl(ComponentClass type, string name, ComponentInstance? container, int index)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);
        container ??= Root;
        if (!ReferenceEquals(container, Root) && !Root.Descendants().Contains(container))
            throw new ArgumentException($"{container.Name} is not a component of {Name}.", nameof(container));
        if (type.Kind != ComponentKind.Control)
            throw new FormErrorException(FormError.NotAValidControlType(type.ClassName));
        if (string.IsNullOrWhiteSpace(name))
            throw new FormErrorException(FormError.NotALegalControlName(name));
        if (IsNameTaken(name))
            throw new FormErrorException(FormError.ControlNameTaken(name));
        if (!ComponentClasses.CanHold(container.Class, type))
            throw new FormErrorException(FormError.NotAValidControlContainer(container.Class.ClassName));
        var control = New(type, name);
        control.SetContainer(container, index);
        added.Add(control);
        if (ReferenceEquals(type, ComponentClasses.MultiPage))
        {
            foreach (var _ in Enumerable.Range(0, 2))
            {
                var page = New(ComponentClasses.Page, FreeName(ComponentClasses.Page.DefaultName));
                page.Set(Properties.Caption, page.Name).SetContainer(control);
                added.Add(page);
            }
        }

        ControlAdded?.Invoke(this, new FormControlEventArgs(control, container));
        return control;
    }

    // Whether a component of the form other than its root has the name, compared without regard to case.
    internal bool IsNameTaken(string name) => Root.Descendants().Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    // The first of stem1, stem2, ... that no component of the form has.
    internal string FreeName(string stem)
    {
        var number = 1;
        while (IsNameTaken(stem + number.ToString(CultureInfo.InvariantCulture)))
            number++;
        return stem + number.ToString(CultureInfo.InvariantCulture);
    }

    private ComponentInstance New(ComponentClass type, string name)
    {
        var control = new ComponentInstance(type, name);
        if (type.Has(Properties.Visible) && !ReferenceEquals(type, ComponentClasses.Page))
            control.Set(Properties.Visible, false);
        if (type.HasExtent)
            control.Set(Properties.Width, 80d).Set(Properties.Height, 32d);
        if (type.Has(Properties.TabIndex))
            control.Set(Properties.TabIndex, (short)Root.Descendants().Count(c => c.Class.Has(Properties.TabIndex)));
        return control;
    }

    /// <summary>
    /// Removes the control that has a name, as a program's <c>Controls.Remove</c> does with a name.
    /// </summary>
    /// <param name="name">The control's name, compared without regard to case.</param>
    /// <exception cref="FormErrorException">
    /// The form has no control of that name: 730, Control not found. Otherwise as <see cref="RemoveControl(ComponentInstance)"/>.
    /// </exception>
    public void RemoveControl(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var control = Root.Descendants().FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                      ?? throw new FormErrorException(FormError.ControlNotFound(name));
        RemoveControl(control);
    }

    /// <summary>
    /// Takes a control added at run time out of the form, with the controls it holds, as a program's <c>Controls.Remove</c> does,
    /// and raises <see cref="ControlRemoved"/>.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <exception cref="FormErrorException">
    /// The control is not one of the form's: 730, Control not found. It was not added at run time: 729. It holds a control that was
    /// not added at run time: 740, and nothing is removed.
    /// </exception>
    /// <remarks>
    /// The form's other <c>TabIndex</c> values close up over those taken out, so that they run on from 0 with no gap.
    /// </remarks>
    public void RemoveControl(ComponentInstance control)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (!Root.Descendants().Contains(control))
            throw new FormErrorException(FormError.ControlNotFound(control.Name));
        if (!added.Contains(control))
            throw new FormErrorException(FormError.CanOnlyRemoveAddedControls);
        var removed = control.Descendants().Prepend(control).ToList();
        if (removed.FirstOrDefault(c => !added.Contains(c)) is { } placed)
            throw new FormErrorException(FormError.ContainerHoldsADesignTimeControl(placed.Name));
        var container = control.Container!;
        control.SetContainer(null);
        foreach (var component in removed)
            added.Remove(component);
        var freed = removed.Where(c => c.Class.Has(Properties.TabIndex)).Select(c => c.Get(Properties.TabIndex)).ToList();
        foreach (var other in Root.Descendants().Where(c => c.Class.Has(Properties.TabIndex)))
        {
            var tabIndex = other.Get(Properties.TabIndex);
            var below = freed.Count(f => f < tabIndex);
            if (below > 0)
                other.Set(Properties.TabIndex, (short)(tabIndex - below));
        }
        ControlRemoved?.Invoke(this, new FormControlEventArgs(control, container));
    }

    // Takes a page out of its MultiPage, with the controls on it, as Pages.Remove does: unlike Controls.Remove, a page placed at design
    // time goes too, and the controls on it with it.
    internal void RemovePage(ComponentInstance page)
    {
        if (!ReferenceEquals(page.Class, ComponentClasses.Page) || !Root.Descendants().Contains(page))
            throw new FormErrorException(FormError.ControlNotFound(page.Name));
        foreach (var component in page.Descendants().Prepend(page).Where(c => !added.Contains(c)))
            added.Add(component);
        RemoveControl(page);
    }
}

/// <summary>
/// The data of <see cref="FormDefinition.ControlAdded"/> and <see cref="FormDefinition.ControlRemoved"/>.
/// </summary>
/// <param name="control">The control added or removed.</param>
/// <param name="container">The component it was placed in, or taken out of.</param>
public sealed class FormControlEventArgs(ComponentInstance control, ComponentInstance container) : EventArgs
{
    /// <summary>
    /// The control added or removed. A control removed keeps the controls it held.
    /// </summary>
    public ComponentInstance Control { get; } = control;

    /// <summary>
    /// The component the control was placed in, or taken out of.
    /// </summary>
    public ComponentInstance Container { get; } = container;
}

/// <summary>
/// A control library a form declares, such as <c>MSCOMCTL.OCX</c>.
/// </summary>
/// <param name="Id">The GUID of the library's type library.</param>
/// <param name="Version">The library's version, such as <c>2.0</c>.</param>
/// <param name="FileName">The library's file name, as the form's file names it.</param>
public sealed record ControlLibraryReference(Guid Id, string Version, string FileName);
