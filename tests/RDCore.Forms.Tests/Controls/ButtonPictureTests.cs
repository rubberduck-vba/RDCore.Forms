using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A button's Picture at its PicturePosition, as MSForms' CommandButton and ToggleButton lay it out (measured in Excel): the Label's
// layout inside the button 4 pixels from its edge, the caption's box 4 pixels wider and taller than its text, the text at its left and
// 2 pixels from its top, and both one pixel right and down while the button is down.
[TestClass]
public sealed class ButtonPictureTests
{
    private const double Inset = 4, Wider = 4, Taller = 4, Above = 2;
    private static readonly Color Red = Color.FromRgb(255, 0, 0), Green = Color.FromRgb(0, 255, 0);

    // A 24-bit bitmap of one colour, its lower-left pixel of another.
    private static byte[] Bitmap(int width, int height)
    {
        var stride = (width * 3 + 3) / 4 * 4;
        var bytes = new byte[54 + stride * height];
        using var writer = new BinaryWriter(new MemoryStream(bytes));
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(bytes.Length);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(stride * height);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0L);
        for (var y = 0; y < height; y++)
        {
            writer.Seek(54 + y * stride, SeekOrigin.Begin);
            for (var x = 0; x < width; x++)
            {
                var colour = x == 0 && y == 0 ? Green : Red;
                writer.Write(colour.B);
                writer.Write(colour.G);
                writer.Write(colour.R);
            }
        }
        return bytes;
    }

    private static ComponentInstance Toggle(double width, double height, byte[]? picture, string caption, PicturePosition? position = null)
    {
        var button = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1").Set(P.Width, width).Set(P.Height, height)
            .Set(P.Caption, caption);
        if (picture is not null)
            button.Set(P.Picture, picture);
        return position is { } at ? button.Set(P.PicturePosition, at) : button;
    }

    // Where the button draws its picture and its caption's text, from its top left corner, shown at its size, after an action on it.
    private static (Rect? Picture, Rect? Text) Laid(ComponentInstance button, Action<ComponentInstance>? then = null)
    {
        var control = new RDFormsToggleButton(button);
        var (window, _) = ControlHost.Show((control, new Rect(0, 0, button.Get(P.Width), button.Get(P.Height))));
        Dispatcher.UIThread.RunJobs();
        if (then is not null)
        {
            then(button);
            Dispatcher.UIThread.RunJobs();
        }
        var result = (control.PictureBounds, control.CaptionBounds);
        window.Close();
        return result;
    }

    [TestMethod]
    public void APictureBesideTheCaption_IsOnePixelFromItsBox_TheBlockCentredInside_AndTheSecondWordAlignsThem()
    {
        var laid = UI.Run(() => new[] { PicturePosition.LeftTop, PicturePosition.LeftCenter, PicturePosition.LeftBottom, PicturePosition.RightTop }
            .Select(position => Laid(Toggle(200, 93, Bitmap(16, 40), "Ab", position))).Select(l => (l.Picture!.Value, l.Text!.Value)).ToArray());

        var (picture, text) = laid[0];
        var box = new Size(text.Width + Wider, text.Height + Taller);
        Assert.AreEqual(new Size(16, 40), picture.Size);
        Assert.AreEqual(picture.Right + 1, text.X);
        Assert.AreEqual(Inset + Math.Floor((200 - 2 * Inset - (16 + 1 + box.Width)) / 2), picture.X);
        Assert.AreEqual(Inset + Math.Floor((93 - 2 * Inset - 40) / 2), picture.Y);
        CollectionAssert.AreEqual(new[] { Above, Math.Floor((40 - box.Height) / 2) + Above, 40 - box.Height + Above },
            laid.Take(3).Select(l => l.Item2.Y - l.Item1.Y).ToArray());
        var (right, rightText) = laid[3];
        Assert.AreEqual(rightText.X + box.Width + 1, right.X);
        Assert.AreEqual(Inset + Math.Floor((200 - 2 * Inset - (box.Width + 1 + 16)) / 2), rightText.X);
    }

    [TestMethod]
    public void APictureAboveOrBelowTheCaption_IsOnePixelFromItsBox_AndTheSecondWordAlignsTheirEdges()
    {
        var laid = UI.Run(() => new[]
            {
                PicturePosition.AboveLeft, PicturePosition.AboveCenter, PicturePosition.AboveRight, PicturePosition.BelowLeft, PicturePosition.Center,
            }
            .Select(position => Laid(Toggle(200, 93, Bitmap(40, 12), "Ab", position))).Select(l => (l.Picture!.Value, l.Text!.Value)).ToArray());

        var (above, text) = laid[0];
        var box = new Size(text.Width + Wider, text.Height + Taller);
        Assert.AreEqual(Inset + Math.Floor((200 - 2 * Inset - 40) / 2), above.X);
        Assert.AreEqual(Inset + Math.Floor((93 - 2 * Inset - (12 + 1 + box.Height)) / 2), above.Y);
        Assert.AreEqual(above.Bottom + 1 + Above, text.Y);
        CollectionAssert.AreEqual(new[] { above.X, above.X + Math.Floor((40 - box.Width) / 2), above.Right - box.Width },
            laid.Take(3).Select(l => l.Item2.X).ToArray());
        Assert.AreEqual(laid[3].Item2.Y - Above + box.Height + 1, laid[3].Item1.Y);
        var (centre, centreText) = laid[4];
        Assert.AreEqual((Inset + Math.Floor((200 - 2 * Inset - 40) / 2), Inset + Math.Floor((93 - 2 * Inset - 12) / 2)), (centre.X, centre.Y));
        Assert.AreEqual(Inset + Math.Floor((200 - 2 * Inset - box.Width) / 2), centreText.X);
    }

    // As measured: MSForms' default position is AboveCenter, and an empty caption still takes its box, so a picture beside it is off
    // the centre by half the box and the gap.
    [TestMethod]
    public void TheDefaultPosition_IsAboveCenter_AndAnEmptyCaption_StillTakesItsBox()
    {
        var (byDefault, set) = UI.Run(() =>
            (Laid(Toggle(200, 93, Bitmap(16, 12), "Ab")), Laid(Toggle(200, 93, Bitmap(16, 12), "Ab", PicturePosition.AboveCenter))));
        var (beside, _) = UI.Run(() => Laid(Toggle(200, 93, Bitmap(16, 12), "", PicturePosition.LeftCenter)));

        Assert.AreEqual(set, byDefault);
        Assert.IsTrue(beside!.Value.X < Math.Floor((200 - 16) / 2d) - 1, beside.Value.ToString());
    }

    [TestMethod]
    public void ADownButton_DrawsItsPictureAndCaptionOnePixelRightAndDown()
    {
        var (up, down) = UI.Run(() => (Laid(Toggle(200, 93, Bitmap(16, 12), "Ab", PicturePosition.LeftCenter)),
            Laid(Toggle(200, 93, Bitmap(16, 12), "Ab", PicturePosition.LeftCenter), b => b.Set(P.ToggleButtonValue, true))));

        Assert.AreEqual(up.Picture!.Value.Translate(new Vector(1, 1)), down.Picture);
        Assert.AreEqual(up.Text!.Value.Translate(new Vector(1, 1)), down.Text);
    }

    // As in MSForms, the colour of a bitmap's lower-left pixel shows the button's face: the green pixel is drawn as the face beside the
    // picture is, and the red ones are drawn.
    [TestMethod]
    public void ABitmapsLowerLeftColour_ShowsTheFace()
    {
        var (inside, lowerLeft, beside) = UI.Run(() =>
        {
            var control = new RDFormsToggleButton(Toggle(60, 40, Bitmap(16, 12), "", PicturePosition.Center));
            var (window, _) = ControlHost.Show((control, new Rect(0, 0, 60, 40)));
            Dispatcher.UIThread.RunJobs();
            var at = control.PictureBounds!.Value;
            var (x, y) = ((int)at.X, (int)at.Bottom - 1);
            var pixels = PictureControlTests.PixelsAt(window, new PixelPoint(x + 8, y - 5), new PixelPoint(x, y), new PixelPoint(x - 1, y));
            window.Close();
            return (pixels[0], pixels[1], pixels[2]);
        });

        Assert.AreEqual(Red, inside);
        Assert.AreEqual(beside, lowerLeft);
        Assert.AreNotEqual(Green, lowerLeft);
    }

    // A picture larger than the inside of the button is shrunk to it, each way alone, as the Label's is.
    [TestMethod]
    public void APictureLargerThanTheInside_IsShrunkToIt()
    {
        var (picture, _) = UI.Run(() => Laid(Toggle(53, 34, Bitmap(64, 48), "", PicturePosition.Center)));

        Assert.AreEqual(new Rect(Inset, Inset, 53 - 2 * Inset, 34 - 2 * Inset), picture);
    }

    // With no picture the button shows its caption alone, as it did; a picture set later is laid out with it, and taking it away
    // shows the caption alone again.
    [TestMethod]
    public void APicture_SetAndTakenAway_IsLaidOutWithTheCaption_ThenLeavesItAlone()
    {
        var (none, set, taken) = UI.Run(() => (Laid(Toggle(200, 93, null, "Ab")),
            Laid(Toggle(200, 93, null, "Ab"), b => b.Set(P.Picture, Bitmap(16, 12))),
            Laid(Toggle(200, 93, Bitmap(16, 12), "Ab"), b => b.Set(P.Picture, null))));

        Assert.AreEqual((null, null), (none.Picture, none.Text));
        Assert.AreEqual(new Size(16, 12), set.Picture!.Value.Size);
        Assert.IsNotNull(set.Text);
        Assert.AreEqual((null, null), (taken.Picture, taken.Text));
    }
}
