using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

// A placed UserControl's site: its copy, its Ambient and its PropertyBag, as VB6 gives them to the UserControl's code (measured with
// VB6, native and p-code agreeing).
[TestClass]
public sealed class UserControlSiteTests
{
    private static string Ctl(params string[] settings) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.UserControl Gauge ",
            "   ClientHeight    =   500",
            "   ClientWidth     =   1500",
            .. settings.Select(s => "   " + s),
            "End",
            "Attribute VB_Name = \"Gauge\"",
            ""]);

    // Gauge1 as the VB6 grid places it (2000 by 600 twips), with the values its ReadProperties read; Gauge2 plain.
    private const string PlacingForm = """
        VERSION 5.00
        Begin VB.Form Form1
           ClientHeight    =   4500
           ClientWidth     =   6000
           Begin Project1.Gauge Gauge1
              Height          =   600
              Left            =   100
              TabIndex        =   0
              Top             =   600
              Width           =   2000
              _ExtentX        =   3528
              _ExtentY        =   1058
              Caption         =   "hello"
              Num             =   42
              Big             =   100000
              Neg             =   -5
              Hex2            =   &H8000000F&
              Flag            =   -1  'True
              Dbl             =   1.5
              Hex             =   -2147483633
              Str             =   "007"
              Pic             =   "Form1.frx":0000
              BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851}
                 Name            =   "Arial"
                 Size            =   12
                 Charset         =   0
                 Weight          =   700
                 Underline       =   0   'False
                 Italic          =   0   'False
                 Strikethrough   =   0   'False
              EndProperty
           End
           Begin Project1.Gauge Gauge2
              Height          =   500
              Left            =   100
              TabIndex        =   1
              Top             =   1300
              Width           =   1500
              _ExtentX        =   2646
              _ExtentY        =   882
           End
        End
        Attribute VB_Name = "Form1"

        """;

    private static (FormDefinition Gauge, FormDefinition Form) Read(params string[] settings)
    {
        var gauge = VB6FormReader.Read(Ctl(settings)).Form!;
        var type = UserControlClasses.Of(gauge, "Project1");
        return (gauge, VB6FormReader.Read(PlacingForm, options: new VB6ImportOptions { Classes = [type] }).Form!);
    }

    [TestMethod]
    public void APlacedControl_HasOneSite_WithItsOwnCopyOfItsUserControl()
    {
        var (gauge, form) = Read();
        var gauge1 = form.Find("Gauge1")!;
        var gauge2 = form.Find("Gauge2")!;

        var site = UserControlSite.Of(gauge1);

        Assert.AreSame(site, UserControlSite.Of(gauge1));
        Assert.AreSame(gauge1, site.Extender);
        Assert.AreNotSame(gauge, site.UserControl);
        Assert.AreNotSame(site.UserControl, UserControlSite.Of(gauge2).UserControl);
        Assert.AreEqual("Gauge", site.UserControl.Root.Name);
    }

    [TestMethod]
    public void AControlInvisibleAtRunTime_HasASiteToo()
    {
        var (_, form) = Read("InvisibleAtRuntime=   -1  'True");

        var site = UserControlSite.Of(form.Find("Gauge2")!);

        Assert.AreEqual("Gauge", site.UserControl.Root.Name);
    }

    [TestMethod]
    public void AComponentOfAnotherClass_HasNoSite()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");

        Assert.ThrowsExactly<ArgumentException>(() => UserControlSite.Of(form));
    }

    // The PropertyBag a UserControl's ReadProperties reads, as VB6 types the form file's text: the host's values wait for the SDK's VBRUN
    // PropertyBag object, which holds them.
    [TestMethod]
    [Ignore("needs the SDK Vbrun objects")]
    [DataRow("Caption", "String", "hello", DisplayName = "\"hello\": String")]
    [DataRow("caption", "String", "hello", DisplayName = "a name in another case")]
    [DataRow("Str", "String", "007", DisplayName = "\"007\": String")]
    [DataRow("Num", "Integer", "42", DisplayName = "42: Integer")]
    [DataRow("Neg", "Integer", "-5", DisplayName = "-5: Integer")]
    [DataRow("Flag", "Integer", "-1", DisplayName = "-1 'True: Integer")]
    [DataRow("Big", "Long", "100000", DisplayName = "100000: Long")]
    [DataRow("Hex", "Long", "-2147483633", DisplayName = "-2147483633: Long")]
    [DataRow("Hex2", "Long", "-2147483633", DisplayName = "&H8000000F&: Long")]
    [DataRow("Dbl", "Currency", "1.5", DisplayName = "1.5: Currency")]
    [DataRow("Font", "Font", "Arial 12 bold", DisplayName = "a Font bag: its Font")]
    [DataRow("_ExtentX", "Long", "3519", DisplayName = "_ExtentX from 133 pixels, not the file's 3528")]
    [DataRow("_ExtentY", "Long", "1058", DisplayName = "_ExtentY from 40 pixels")]
    [DataRow("Left", "327", "", DisplayName = "an extender property: not the UserControl's (327)")]
    [DataRow("Missing", "327", "", DisplayName = "a name the file lacks, with no default: 327")]
    [DataRow("", "5", "", DisplayName = "an empty name: 5")]
    public void ReadProperty_TypesTheFilesText_AsVB6Does(string name, string type, string value) =>
        Assert.Fail($"{name}: {type} {value}");

    // Ambient on a form, as VB6 reads it: the host's values wait for the SDK's VBRUN AmbientProperties object.
    [TestMethod]
    [Ignore("needs the SDK Vbrun objects")]
    [DataRow("BackColor", "the form's, unsigned (2147483663 for &H8000000F)")]
    [DataRow("ForeColor", "the form's, unsigned")]
    [DataRow("Font", "a copy of the form's Font, never the same object")]
    [DataRow("DisplayName", "Gauge1")]
    [DataRow("ScaleUnits", "Twip, Pixel after ScaleMode 3")]
    [DataRow("DisplayAsDefault", "False")]
    public void Ambient_ReadsTheForm(string member, string value) =>
        Assert.Fail($"{member}: {value}");
}
