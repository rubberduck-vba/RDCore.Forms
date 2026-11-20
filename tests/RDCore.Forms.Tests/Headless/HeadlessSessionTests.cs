using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Simple;
using RDCore.Forms.Avalonia;

namespace RDCore.Forms.Tests.Headless;

[TestClass]
public sealed class HeadlessSessionTests
{
    [TestMethod]
    public void TheSession_RunsTheShippedApplication_WhoseThemeGivesTheControlsTheirTemplates()
    {
        var (application, themed, templated) = Headless.Run(() =>
        {
            var box = new TextBox();
            var window = new Window { Content = box };
            window.Show();
            var result = (Application.Current is RDFormsApplication, Application.Current!.Styles.OfType<SimpleTheme>().Any(), box.Template is not null);
            window.Close();
            return result;
        });

        Assert.IsTrue(application);
        Assert.IsTrue(themed);
        Assert.IsTrue(templated);
    }

    // the name the platform shows where it names the application, as macOS names its application menu: the session's application was built with no name.
    [TestMethod]
    public void TheSessionsApplication_IsNamedRDForms_ItsHostNamingItNone()
        => Assert.AreEqual(RDFormsApplication.DefaultName, Headless.Run(() => Application.Current!.Name));

    [TestMethod]
    [DataRow("Ducky", "Ducky", DisplayName = "the name given")]
    [DataRow(null, "RDForms", DisplayName = "no name")]
    [DataRow("", "RDForms", DisplayName = "an empty name")]
    [DataRow("  ", "RDForms", DisplayName = "white space")]
    public void AnApplication_IsNamedAsItsHostNamesIt_OrRDForms(string? name, string expected)
        => Assert.AreEqual(expected, Headless.Run(() => RDFormsApplication.Create<RDFormsApplication>(name).Name));

    [TestMethod]
    [DataRow("Ducky", "Ducky", DisplayName = "the name given")]
    [DataRow(null, "Named in its constructor", DisplayName = "no name")]
    [DataRow("  ", "Named in its constructor", DisplayName = "white space")]
    public void ADerivedApplication_NamedInItsConstructor_KeepsItsName_ItsHostNamingItNone(string? name, string expected)
        => Assert.AreEqual(expected, Headless.Run(() => RDFormsApplication.Create<SelfNamedApplication>(name).Name));

    private sealed class SelfNamedApplication : RDFormsApplication
    {
        public SelfNamedApplication() => Name = "Named in its constructor";
    }

    [TestMethod]
    public void AWindowShown_InTheHeadlessSession_TakesTypedText()
    {
        var text = Headless.Run(() =>
        {
            var box = new TextBox();
            var window = new Window { Content = box };
            window.Show();
            box.Focus();

            window.KeyTextInput("abc");
            window.Close();
            return box.Text;
        });

        Assert.AreEqual("abc", text);
    }
}
