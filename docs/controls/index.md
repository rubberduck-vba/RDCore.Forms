# Controls

`RDCore.Forms.Avalonia` shows a form's components with Avalonia controls. Each control shows one [`ComponentInstance`](../api/RDCore.Forms.Model.ComponentInstance.yml): the component holds the values, and the control follows them. A change from code, made on the component, shows in the control; a change the user makes, such as typing in a TextBox, is written to the component.

## Catalogue

[`RDFormsControls.Create`](../api/RDCore.Forms.Avalonia.Controls.RDFormsControls.yml) makes the control for a component of an RDForms class: the classes Visual Basic 6.0's intrinsic controls port to.

|Class|Control|Events raised|Page|
|---|---|---|---|
|`RDForms.CommandButton`|[`RDFormsCommandButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsCommandButton.yml)|Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`RDForms.CheckBox`|[`RDFormsCheckBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsCheckBox.yml)|Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`RDForms.OptionButton`|[`RDFormsOptionButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsOptionButton.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`RDForms.Label`|[`RDFormsLabel`](../api/RDCore.Forms.Avalonia.Controls.RDFormsLabel.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`RDForms.TextBox`|[`RDFormsTextBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsTextBox.yml)|Change, Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`RDForms.ListBox`|[`RDFormsListBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsListBox.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Lists](lists.md)|
|`RDForms.ComboBox`|[`RDFormsComboBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsComboBox.yml)|Change, Click, DblClick, DropDown, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus|[Lists](lists.md)|
|`RDForms.DriveListBox`|[`RDFormsDriveListBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsDriveListBox.yml)|Change, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus|[File system lists](file-lists.md)|
|`RDForms.DirListBox`|[`RDFormsDirListBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsDirListBox.yml)|Change, Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[File system lists](file-lists.md)|
|`RDForms.FileListBox`|[`RDFormsFileListBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsFileListBox.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp, PathChange, PatternChange|[File system lists](file-lists.md)|
|`RDForms.Frame`|[`RDFormsFrame`](../api/RDCore.Forms.Avalonia.Controls.RDFormsFrame.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`RDForms.PictureBox`|[`RDFormsPictureBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsPictureBox.yml)|Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Paint, Resize|[Containers and pictures](containers.md)|
|`RDForms.Image`|[`RDFormsImage`](../api/RDCore.Forms.Avalonia.Controls.RDFormsImage.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`RDForms.Timer`|[`RDFormsTimer`](../api/RDCore.Forms.Avalonia.Controls.RDFormsTimer.yml)|Timer|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`RDForms.HScrollBar`, `RDForms.VScrollBar`|[`RDFormsScrollBar`](../api/RDCore.Forms.Avalonia.Controls.RDFormsScrollBar.yml)|Change, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, Scroll|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`RDForms.Shape`|[`RDFormsShape`](../api/RDCore.Forms.Avalonia.Controls.RDFormsShape.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`RDForms.Line`|[`RDFormsLine`](../api/RDCore.Forms.Avalonia.Controls.RDFormsLine.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`RDForms.Data`|[`RDFormsData`](../api/RDCore.Forms.Avalonia.Controls.RDFormsData.yml)|None|[Data control](data.md)|
|`RDForms.Menu`|The window's [`RDFormsMenuBar`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsMenuBar.yml)|Click|[Menus](menus.md)|
|A placed UserControl, such as `Project1.Gauge`|[`RDFormsUserControl`](../api/RDCore.Forms.Avalonia.Controls.RDFormsUserControl.yml)|None of its own; its UserControl's root and controls raise theirs|[UserControls](usercontrols.md)|

A `RDForms.Menu` has no control of its own, and `Create` returns `null` for it: a form's window shows its menus on its [menu bar](menus.md). It returns `null` for a UserControl that is invisible at run time too.

## Showing a form

A [`RDFormsFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsFormWindow.yml) shows a form whose root is a `RDForms.Form`. Its client area is a [`RDFormsFormView`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsFormView.yml), which holds the form's controls. The controls raise their events to the [`IComponentEventSink`](../api/RDCore.Forms.Model.IComponentEventSink.yml) set on the window; a window with no sink raises none.

```csharp
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

// In a running Avalonia application.
var form = VB6FormReader.ReadFile("Form1.frm").Form!;
var window = new RDFormsFormWindow(form);
RDFormsControl.SetEventSink(window, new PrintingSink());
window.Show();

sealed class PrintingSink : IComponentEventSink
{
    public void Raise(ComponentInstance source, EventDefinition definition, FormValue[] arguments) =>
        Console.WriteLine($"{source.Name}_{definition.Name}");
}
```

The sink is called with the component, the event, and its arguments in the order of the event's parameters. A handler's assignment to an argument, such as a `KeyAscii` set to 0, is written back into the array, and the control reads it there.

The window and the controls need an Avalonia application with a theme. A host that has none uses [`RDFormsApplication`](../api/RDCore.Forms.Avalonia.RDFormsApplication.yml): Avalonia's Simple theme, drawn with Skia, with Inter as its default font. Its `Configure` method returns the application's builder, and the host adds the platform:

```csharp
using Avalonia;
using RDCore.Forms.Avalonia;

// Avalonia's desktop platform, from the Avalonia.Desktop package.
RDFormsApplication.Configure().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
```

A host that has an Avalonia application of its own keeps it.

The window follows the root's `Caption`, `Icon`, `Width`, `Height`, `MaxButton`, `MinButton` and `ShowInTaskbar`, and a user who resizes it sets the root's `Width` and `Height`. An `Icon` is an icon or a cursor; setting it to any other picture is 380. A window with no `Icon` shows the window system's own. The client area shows the root's `Picture` at its size from its top left corner, under the drawing and the controls (see [Pictures](containers.md#pictures)). A `StartUpPosition` of CenterOwner or CenterScreen centres it; Manual and Windows Default leave its place to the window system. The root raises its own events, Resize, Activate, QueryUnload, Unload and its mouse and key events among them: see [Form events](form-events.md).

## Placement

|Rule|Behaviour|
|---|---|
|Units|The model holds a control's `Left`, `Top`, `Width` and `Height` in pixels (1/96 inch, 15 twips), whatever its container's `ScaleMode`. A program's code reads them in its own unit: VB6 code in the container's scale (twips in a Frame, and for a form's own), VBA code in points, and code made for RDForms in pixels ([`CodeUnits`](../api/RDCore.Forms.Model.CodeUnits.yml)). A Line's ends are in its container's scale.|
|Containers|A control placed in a Frame or a PictureBox is shown in it. A control placed in any other component is not shown.|
|Stacking|In each container, the first control the form holds is on top until [`ZOrder`](methods.md#zorder) moves one. A windowless control (a Label, an Image, a Shape or a Line) is under every windowed one.|
|Tab order|One order across the whole form, by `TabIndex`, the form's order deciding a tie. A `TabIndex` past the last place takes the first free place, in the form's order.|
|Focus|Tab skips a control that takes no focus (a Label, a Frame, an Image, a Timer, a Shape, a Line or a Data control), one whose `TabStop` is False, and one that is hidden or disabled or in a hidden or disabled container.|

## Common properties

|Property|Behaviour|
|---|---|
|`Visible`, `Enabled`|Hide or disable the control, and every control it holds. A disabled form takes no input in any of its controls.|
|`BackColor`, `ForeColor`|A system colour is drawn in Windows 11's default palette ([`FormPalette`](../api/RDCore.Forms.Avalonia.Controls.FormPalette.yml)), on every platform.|
|`Font`|The control's font, held at the size VB6 draws it at: see [Fonts](fonts.md).|
|`ToolTipText`|Shown as the control's tooltip; an empty text shows none.|
|`MousePointer`|The pointer over the control. Custom, drawn from `MouseIcon` in VB6, shows the arrow.|

## Run-time controls

[`FormDefinition.AddControl`](../api/RDCore.Forms.Model.FormDefinition.yml) and `RemoveControl` add and remove controls as a program's `Controls.Add` and `Controls.Remove` do. A shown form's view places and removes the controls at once.

|Rule|Behaviour|
|---|---|
|Class|Named by its ProgID, such as `RDForms.CommandButton`, without regard to case. A menu's or a form's class is refused.|
|Name|Any name that no other control has, compared without regard to case, even one that starts with a digit, holds a space, is a keyword, or is the form's own name. An empty name is refused, and so is a name of white space only (RDForms' own).|
|Container|The form, or a Frame or a PictureBox of the form.|
|Defaults|Hidden, at 0, 0, and 80 by 32 pixels (1200 by 480 twips) where the class has an extent. The next `TabIndex` after the form's others. A caption or a text is "".|
|Stacking|Under the controls already in its container, windowed or windowless.|
|Order|`FormDefinition.Controls` holds the controls of the form's file depth first, then those added, in the order added, whatever their container.|
|Events|An added control raises its events to the window's sink as the form's other controls do. Adding one raises no event of the form.|
|Removal|Only a control added at run time, with the controls it holds. The other `TabIndex` values close up. A control that had the focus gives it to the first control of the tab order that takes it (RDForms' own).|

|Error|When|
|---|---|
|711|No VB class has the ProgID.|
|720|The class is a menu's or a form's.|
|721|The container holds no controls, such as a CommandButton.|
|727|The name is taken.|
|728|The name is empty, or white space only.|
|729|The control to remove was not added at run time.|
|730|No control has the name to remove, or it was removed already.|
|740|The control to remove holds a control that was not added at run time. Nothing is removed.|

> [!NOTE]
> **Not implemented.** A ListBox added at run time to a form that is not shown keeps a height of 32 pixels until the form is shown, where its height is snapped to whole rows (450 twips, 30 pixels); VB6 snaps it as it is added. The `Controls` collection itself, error 735 for a control used after its removal, and the controls of a form's file coming back after Unload and Load belong to the runtime that runs the program.

## Properties set only at design time

Some properties are set only by a designer and a form's file. A running VB6 program can read them and cannot set them: its assignment is error 382. A few it can neither read (error 393) nor set (382). These are the ones RDForms models, as measured in VB6:

|Access from code|Properties|
|---|---|
|Read only|Form and MDIForm: `StartUpPosition`. Form: `ShowInTaskbar`, `MaxButton`, `MinButton`. CommandButton, CheckBox, OptionButton and ComboBox: `Style`. TextBox: `MultiLine`, `ScrollBars`. ListBox and FileListBox: `MultiSelect`. ListBox and ComboBox: `IntegralHeight`. Menu: `WindowList`.|
|Neither read nor set|Menu: `Shortcut`. Timer: `Left`, `Top`. Data: `Negotiate`.|

The model answers for each class with [`ComponentClass.IsReadOnlyAtRunTime`](../api/RDCore.Forms.Model.ComponentClass.yml) and `IsNotAvailableAtRunTime`. A Timer's `Left` and `Top` are the same definitions as other controls', and only the Timer makes them unavailable.

> [!NOTE]
> **Not implemented.** The runtime does not refuse a program's read or assignment of these properties with 393 or 382.

## Errors

A member that refuses a value throws a [`FormErrorException`](../api/RDCore.Forms.Model.FormErrorException.yml) that holds VB's error number and description, such as 380 for a `ListIndex` outside the items.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | CONTROLS | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
