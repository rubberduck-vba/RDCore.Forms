using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A list whose MultiSelect is Simple or Extended, pressed and keyed as in the VB6 measurements. Each step logs the Click and DblClick it
// raised, the items selected and ListIndex. VB6's Ctrl and Shift presses cannot be measured by sending messages, so those steps follow
// the ListBox's own rules.
[TestClass]
public sealed class MultiSelectListTests
{
    // A list of five items, and what a program reads of it.
    private sealed record Multi(ListBox Control, Func<int> ListIndex, Func<int, bool> Selected);

    private static Multi Make(string type, ListBoxMultiSelect mode)
    {
        switch (type)
        {
            case "ListBox":
                var list = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List1")
                    .Set(P.List, ["a", "b", "c", "d", "e"]).Set(P.MultiSelect, mode));
                return new(list, () => list.ListIndex, list.GetSelected);
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static Pointer Mouse() => new(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);

    // A left press and release on an item, a double click's second one where clickCount is 2.
    private static void Press(Window window, ListBox list, int index, KeyModifiers modifiers = KeyModifiers.None, int clickCount = 1)
    {
        var item = list.ContainerFromIndex(index)!;
        var pointer = Mouse();
        var point = item.TranslatePoint(new Point(4, 4), window)!.Value;
        item.RaiseEvent(new PointerPressedEventArgs(item, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), modifiers, clickCount));
        item.RaiseEvent(new PointerReleasedEventArgs(item, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();
    }

    // A key pressed, the character it types, and the key released.
    private static void Stroke(Window window, Key key, PhysicalKey physical, string? text)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, text);
        Dispatcher.UIThread.RunJobs();
        if (text is not null)
            window.KeyTextInput(text);
        window.KeyRelease(key, RawInputModifiers.None, physical, text);
        Dispatcher.UIThread.RunJobs();
    }

    // Runs the steps on a new list and logs each as "step: events [selected] ListIndex".
    private static List<string> Steps(string type, ListBoxMultiSelect mode, params (string Name, Action<Window, ListBox> Act)[] steps) =>
        UI.Run(() =>
        {
            var list = Make(type, mode);
            var (window, sink) = ControlHost.Show((list.Control, new Rect(10, 5, 160, 140)));
            var log = new List<string>();
            foreach (var (name, act) in steps)
            {
                sink.Raised.Clear();
                act(window, list.Control);
                var events = sink.Raised.Where(e => e.EndsWith(".Click", StringComparison.Ordinal) || e.EndsWith(".DblClick", StringComparison.Ordinal))
                    .Select(e => e[(e.IndexOf('.') + 1)..]);
                var selected = Enumerable.Range(0, list.Control.ItemCount).Where(list.Selected);
                log.Add($"{name}: {string.Join(" ", events)} [{string.Join(",", selected)}] {list.ListIndex()}");
            }
            window.Close();
            return log;
        });

    [TestMethod]
    [DataRow("ListBox")]
    public void ASimpleListsPress_TogglesTheItem_WhateverTheModifiers_AndRaisesClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Simple,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("Ctrl+press 1", (w, l) => Press(w, l, 1, KeyModifiers.Control)),
            ("Shift+press 4", (w, l) => Press(w, l, 4, KeyModifiers.Shift)));

        Expect.Lines(["press 1: Click [1] 1", "press 3: Click [1,3] 3", "press 3: Click [1] 3", "Ctrl+press 1: Click [] 1",
            "Shift+press 4: Click [4] 4"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    public void AnExtendedListsPress_SelectsTheItemAlone_TogglesItWithCtrl_AndSelectsFromTheAnchorWithShift(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Extended,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("Ctrl+press 1", (w, l) => Press(w, l, 1, KeyModifiers.Control)),
            ("Shift+press 4", (w, l) => Press(w, l, 4, KeyModifiers.Shift)),
            ("press 2", (w, l) => Press(w, l, 2)));

        Expect.Lines(["press 1: Click [1] 1", "press 3: Click [3] 3", "Ctrl+press 1: Click [1,3] 1", "Shift+press 4: Click [1,2,3,4] 4",
            "press 2: Click [2] 2"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    public void ASimpleListsDown_MovesOnlyTheFocus_AndSpaceTogglesTheFocusedItem_EachWithClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Simple,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("Down", (w, _) => Stroke(w, Key.Down, PhysicalKey.ArrowDown, null)),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Down: Click [1] 2", "Space: Click [1,2] 2", "Space: Click [1] 2"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    public void AnExtendedListsDown_SelectsTheNextItemAlone_AndSpaceChangesNothing_EachWithClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Extended,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("Down", (w, _) => Stroke(w, Key.Down, PhysicalKey.ArrowDown, null)),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Down: Click [2] 2", "Space: Click [2] 2"], log);
    }

    [TestMethod]
    [DataRow("ListBox", ListBoxMultiSelect.Simple)]
    [DataRow("ListBox", ListBoxMultiSelect.Extended)]
    public void ADoubleClick_RaisesClickThenDblClick_AndTogglesTheItemOnce(string type, ListBoxMultiSelect mode)
    {
        var log = Steps(type, mode,
            ("double click 0", (w, l) =>
            {
                Press(w, l, 0);
                Press(w, l, 0, clickCount: 2);
            }));

        Expect.Lines(["double click 0: Click DblClick [0] 0"], log);
    }
}
