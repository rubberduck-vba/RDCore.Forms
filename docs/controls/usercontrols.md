# UserControls

A UserControl's designer file (`.ctl`) reads as a [`FormDefinition`](../api/RDCore.Forms.Model.FormDefinition.yml) whose root is a `VB.UserControl`. A form places the UserControl as a control of the class [`UserControlClasses.Of`](../api/RDCore.Forms.Model.VB.UserControlClasses.yml) makes from that definition.

> [!NOTE]
> **Not implemented.** RDForms shows no UserControl: [`VBControls.Create`](../api/RDCore.Forms.Avalonia.Controls.VBControls.yml) returns `null` for a placed one, and neither the root nor a placed control raises an event.

## The root

The root has the properties VB6's Properties window lists for a UserControl, with VB6's defaults. The model holds them and acts on none of them.

|Property|Default|
|---|---|
|`BackStyle`|1, Opaque.|
|`MaskColor`|The button face's system colour, `&H8000000F&`.|
|`PaletteMode`|3, Container.|
|`CanGetFocus`, `ClipControls`, `FontTransparent`, `HasDC`, `Enabled`|True.|
|`ClipBehavior`, `HitBehavior`|1, Use Region.|
|`Alignable`, `ControlContainer`, `DefaultCancel`, `EditAtDesignTime`, `ForwardFocus`, `InvisibleAtRuntime`, `Public`, `Windowless`|False.|
|`DataBindingBehavior`, `DataSourceBehavior`, `OLEDropMode`|0, None.|
|`AccessKeys`|"".|
|`MaskPicture`, `Palette`, `ToolboxBitmap`|No picture.|

Its drawing properties (`AutoRedraw`, `DrawMode`, `DrawStyle`, `DrawWidth`, `FillColor`, `FillStyle`), `Appearance`, `BorderStyle`, `Picture`, `MouseIcon`, `MousePointer`, `RightToLeft`, `KeyPreview`, `Font`, `BackColor` and `ForeColor` have a form's defaults.

> [!NOTE]
> **Not implemented.** `PropertyPages` is kept as text.

## Placed UserControls

[`UserControlClasses.Of`](../api/RDCore.Forms.Model.VB.UserControlClasses.yml) makes the class from the UserControl's definition, the project's name and the events the UserControl's code declares. Its type name is the project's name and the UserControl's, such as `Project1.Gauge`, and [`VB6ImportOptions.Classes`](../api/RDCore.Forms.Import.VB6.VB6ImportOptions.yml) gives it to the reader of the forms that place it:

```csharp
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model.VB;

var gauge = UserControlClasses.Of(VB6FormReader.ReadFile("Gauge.ctl").Form!, "Project1");
var form = VB6FormReader.ReadFile("Form1.frm", new VB6ImportOptions { Classes = [gauge] }).Form!;
```

A placed control's members are those of the extender VB6 wraps it in. The UserControl's settings decide which it has, as measured with VB6:

|Setting|Members of a placed control|
|---|---|
|Any|`Left` and `Top`.|
|`InvisibleAtRuntime` False|`Width`, `Height`, `Visible`, `DragMode`, `ToolTipText`, `WhatsThisHelpID` and `Tag`.|
|`InvisibleAtRuntime` True|Nothing else. The control has no size, and its `Left` and `Top` are not available at run time (393).|
|`CanGetFocus` or `ForwardFocus` True, and visible|`TabIndex`.|
|`CanGetFocus` True, `ForwardFocus` False, and visible|`TabStop`, `CausesValidation`, `HelpContextID`, `SetFocus`, and the GotFocus and LostFocus events.|
|`DefaultCancel` True|`Default` and `Cancel`.|
|`Alignable` True|`Align`, and `Negotiate`: False, and not available at run time (393).|

A placed control has no `Enabled`, `hWnd` or data-binding member (438 in VB6). It is a container when the UserControl's `ControlContainer` is True, and windowless when its `Windowless` is. The properties a form's file sets on it that are the UserControl's own, such as its `_ExtentX` and `_ExtentY`, are kept as text.

> [!NOTE]
> **Not implemented.** A placed control has no `Move`, `ZOrder`, `Parent`, `Container` or `Object`. An `Alignable` one's `Align` does not dock it to an edge of its form.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)
