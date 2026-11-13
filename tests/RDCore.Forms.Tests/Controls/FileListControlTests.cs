using System.IO.Abstractions.TestingHelpers;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The folder of the VB6 measurements, its files and their attributes, on a file system of the test's own.
[TestClass]
public sealed class FileListControlTests
{
    private static readonly string Root = OperatingSystem.IsWindows() ? @"C:\tree" : "/tree";

    private static string In(params string[] parts) => Path.Combine([Root, .. parts]);

    private static MockFileSystem Tree() => new(new Dictionary<string, MockFileData>
    {
        [In("b.bas")] = new(""),
        [In("a.txt")] = new(""),
        [In("C.TXT")] = new(""),
        [In("Zeta.Txt")] = new(""),
        [In("noext")] = new(""),
        [In("hid.txt")] = new("") { Attributes = FileAttributes.Hidden },
        [In("sys.txt")] = new("") { Attributes = FileAttributes.System },
        [In("ro.txt")] = new("") { Attributes = FileAttributes.ReadOnly },
        [In("SubOne", "x.bas")] = new(""),
    }, Root);

    private static ComponentInstance File() => new(VBClasses.FileListBox, "File1");

    private static string[] Items(VBFileListBox list) => [.. Enumerable.Range(0, list.ListCount).Select(list.GetList)];

    [TestMethod]
    public void AFileList_ListsTheCurrentDirectorysFiles_SortedIgnoringCase_WithoutHiddenOrSystemFiles()
    {
        var (path, items, fileName, index) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            return (list.Path, Items(list), list.FileName, list.ListIndex);
        });

        Assert.AreEqual(Root, path);
        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "noext", "ro.txt", "Zeta.Txt" }, items);
        Assert.AreEqual("", fileName);
        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    [DataRow("*.txt", "a.txt|C.TXT|ro.txt|Zeta.Txt")]
    [DataRow("*.txt;*.bas", "a.txt|b.bas|C.TXT|ro.txt|Zeta.Txt")]
    [DataRow("*.*", "a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt")]
    [DataRow("*", "a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt")]
    [DataRow("?.txt", "a.txt|C.TXT")]
    [DataRow("NOEXT", "noext")]
    [DataRow("z*", "Zeta.Txt")]
    public void APattern_ListsTheFilesThatMatchAnyOfItsParts_WithoutRegardToCase(string pattern, string expected)
    {
        var items = UI.Run(() => Items(new VBFileListBox(File().Set(P.Pattern, pattern), Tree())));

        Assert.AreEqual(expected, string.Join("|", items));
    }

    [TestMethod]
    public void SettingThePattern_ListsItsFiles_AndRaisesPatternChange_OnlyWhereItChanges()
    {
        var (items, raised) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.Pattern = "*.bas";
            list.Pattern = "*.bas";
            window.Close();
            return (Items(list), sink.Raised);
        });

        Expect.Lines(new[] { "b.bas" }, items);
        Expect.Lines(new[] { "File1.PatternChange" }, raised);
    }

    [TestMethod]
    public void HiddenAndSystem_ListTheirFiles_WhileTrue()
    {
        var (shown, hidden) = UI.Run(() =>
        {
            var instance = File();
            var list = new VBFileListBox(instance, Tree());
            var (window, _) = ControlHost.Show(list);
            instance.Set(P.Hidden, true).Set(P.SystemFiles, true);
            var shown = string.Join("|", Items(list));
            instance.Set(P.Hidden, false).Set(P.SystemFiles, false);
            window.Close();
            return (shown, string.Join("|", Items(list)));
        });

        Assert.AreEqual("a.txt|b.bas|C.TXT|hid.txt|noext|ro.txt|sys.txt|Zeta.Txt", shown);
        Assert.AreEqual("a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt", hidden);
    }

    [TestMethod]
    public void ReadOnlyArchiveAndNormal_LeaveTheListAsItIs()
    {
        var items = UI.Run(() =>
        {
            var instance = File();
            var list = new VBFileListBox(instance, Tree());
            var (window, _) = ControlHost.Show(list);
            instance.Set(P.ReadOnly, false).Set(P.Archive, false).Set(P.Normal, false);
            window.Close();
            return Items(list);
        });

        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "noext", "ro.txt", "Zeta.Txt" }, items);
    }

    [TestMethod]
    public void TheListIndex_SelectsAFile_AndRaisesClick_AndOutsideTheFilesIs380()
    {
        var (fileName, raised, error) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 1;
            list.ListIndex = 1;
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = 6).Error.Number;
            window.Close();
            return (list.FileName, sink.Raised, error);
        });

        Assert.AreEqual("b.bas", fileName);
        Expect.Lines(new[] { "File1.Click" }, raised);
        Assert.AreEqual(380, error);
    }

    [TestMethod]
    public void AFileName_BecomesThePattern_AndSelectsTheFile_AsItsNameIsOnDisk()
    {
        var (pattern, fileName, index, items, raised) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.FileName = "c.txt";
            window.Close();
            return (list.Pattern, list.FileName, list.ListIndex, Items(list), sink.Raised);
        });

        Assert.AreEqual("c.txt", pattern);
        Assert.AreEqual("C.TXT", fileName);
        Assert.AreEqual(0, index);
        Expect.Lines(new[] { "C.TXT" }, items);
        Expect.Lines(new[] { "File1.PatternChange" }, raised);
    }

    [TestMethod]
    public void AFileNameOfNoFile_Is53_AndChangesNothing()
    {
        var (error, pattern, count, raised) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.FileName = "nothere.txt").Error.Number;
            window.Close();
            return (error, list.Pattern, list.ListCount, sink.Raised);
        });

        Assert.AreEqual(53, error);
        Assert.AreEqual("*.*", pattern);
        Assert.AreEqual(6, count);
        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void AFileNameThatIsAPattern_BecomesThePattern_AndSelectsNothing()
    {
        var (pattern, fileName, index, items) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            list.ListIndex = 0;
            list.FileName = "*.bas";
            return (list.Pattern, list.FileName, list.ListIndex, Items(list));
        });

        Assert.AreEqual("*.bas", pattern);
        Assert.AreEqual("", fileName);
        Assert.AreEqual(-1, index);
        Expect.Lines(new[] { "b.bas" }, items);
    }

    [TestMethod]
    public void AFileNameWithAFolder_SetsThePath_ThenThePattern()
    {
        var (path, fileName, index, raised) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.FileName = In("SubOne", "x.bas");
            window.Close();
            return (list.Path, list.FileName, list.ListIndex, sink.Raised);
        });

        Assert.AreEqual(In("SubOne"), path);
        Assert.AreEqual("x.bas", fileName);
        Assert.AreEqual(0, index);
        Expect.Lines(new[] { "File1.PathChange", "File1.PatternChange" }, raised);
    }

    [TestMethod]
    public void APath_IsReadWithoutItsFinalSeparator_AndRaisesPathChange_OnlyWhereItChanges()
    {
        var (path, fileName, raised) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 0;
            list.Path = In("SubOne") + Path.DirectorySeparatorChar;
            list.Path = In("SubOne");
            window.Close();
            return (list.Path, list.FileName, sink.Raised);
        });

        Assert.AreEqual(In("SubOne"), path);
        Assert.AreEqual("", fileName);
        Expect.Lines(new[] { "File1.Click", "File1.PathChange" }, raised);
    }

    [TestMethod]
    public void ARelativePath_IsTakenFromTheListsOwnPath()
    {
        var (down, up) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            list.Path = "SubOne";
            var down = list.Path;
            list.Path = "..";
            return (down, list.Path);
        });

        Assert.AreEqual(In("SubOne"), down);
        Assert.AreEqual(Root, up);
    }

    [TestMethod]
    public void APathThatDoesNotExist_Is76_AndChangesNothing()
    {
        var (error, fileNameError, path, count) = UI.Run(() =>
        {
            var list = new VBFileListBox(File(), Tree());
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.Path = In("nothere")).Error.Number;
            var fileNameError = Assert.ThrowsExactly<FormErrorException>(() => list.FileName = In("nothere", "a.txt")).Error.Number;
            return (error, fileNameError, list.Path, list.ListCount);
        });

        Assert.AreEqual(76, error);
        Assert.AreEqual(76, fileNameError);
        Assert.AreEqual(Root, path);
        Assert.AreEqual(6, count);
    }

    [TestMethod]
    public void AFileMadeOnDisk_IsListedOnlyAfterARefresh()
    {
        var (before, after) = UI.Run(() =>
        {
            var files = Tree();
            var list = new VBFileListBox(File(), files);
            files.AddFile(In("new.txt"), new MockFileData(""));
            var before = list.ListCount;
            list.Refresh();
            return (before, Items(list));
        });

        Assert.AreEqual(6, before);
        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "new.txt", "noext", "ro.txt", "Zeta.Txt" }, after);
    }

    [TestMethod]
    public void AFormsFileLists_ShowTheFileSystemItsViewIsGiven()
    {
        var path = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1");
            var file = new ComponentInstance(VBClasses.FileListBox, "File1");
            file.SetContainer(root);
            var view = new VBFormView(new FormDefinition(root), Tree());
            return ((VBFileListBox)view.ControlOf(file)!).Path;
        });

        Assert.AreEqual(Root, path);
    }
}
