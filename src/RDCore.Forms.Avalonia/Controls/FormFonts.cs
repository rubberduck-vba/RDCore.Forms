// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// How a form's fonts are drawn.
/// </summary>
public static class FormFonts
{
    // The libre sans serif drawn where a form's family is not installed. VB's default, MS Sans Serif, is a proprietary bitmap font
    // that the renderer does not draw, so a form in it is drawn in this one.
    private const string Fallback = "Liberation Sans";

    private static readonly ConcurrentDictionary<string, bool> installed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The family a font is drawn in: the family it names where it is installed, or a sans serif.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <returns>The font family.</returns>
    /// <remarks>
    /// An installed family is named alone. Named first in a list of families, a symbol font such as Marlett is passed over for the
    /// platform's default face, though it resolves by itself.
    /// </remarks>
    public static FontFamily ToFontFamily(FormFont font) =>
        IsInstalled(font.Name) ? new FontFamily(font.Name) : new FontFamily($"{font.Name}, {Fallback}");

    /// <summary>
    /// The size a font is drawn at, in device-independent pixels.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <returns>The size: a font's size is in points, and a device-independent pixel is 1/96 of an inch, so 12 points are 16.</returns>
    public static double ToFontSize(FormFont font) => font.Size * 96.0 / 72.0;

    /// <summary>
    /// Sets a control's font family, size, weight and style to a form font's. The control's text inherits them.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="font">The font.</param>
    /// <remarks>
    /// A font's underline and strikethrough are not drawn.
    /// </remarks>
    public static void Apply(Control control, FormFont font)
    {
        control.SetValue(TextElement.FontFamilyProperty, ToFontFamily(font));
        control.SetValue(TextElement.FontSizeProperty, ToFontSize(font));
        control.SetValue(TextElement.FontWeightProperty, font.Bold ? FontWeight.Bold : FontWeight.Normal);
        control.SetValue(TextElement.FontStyleProperty, font.Italic ? FontStyle.Italic : FontStyle.Normal);
    }

    // The height of a line of a font's text, in whole pixels, as VB6 makes a combo's or a list's rows. MS Sans Serif's are VB6's,
    // measured: it is a bitmap font, drawn at the largest of its own sizes that is no larger than the size asked, or its least.
    // Another font's are RDForms' own: VB6 takes them from the font's hinting, so the line as drawn here, rounded up, stands in.
    internal static double LineHeight(FormFont font)
    {
        if (MSSansSerif(font) is { } metrics)
            return metrics.Line;
        var text = new TextBlock { Text = "Ag" };
        Apply(text, font);
        text.Measure(global::Avalonia.Size.Infinity);
        return Math.Ceiling(text.DesiredSize.Height);
    }

    // MS Sans Serif's line and Print column in whole pixels, as VB6 measures them at the size it is drawn at, or null for another
    // font. An italic face's column is RDForms' own: the regular one's.
    internal static (int Line, int Column)? MSSansSerif(FormFont font)
    {
        if (!string.Equals(font.Name, "MS Sans Serif", StringComparison.OrdinalIgnoreCase))
            return null;
        var pixels = Math.Round(ToFontSize(font), MidpointRounding.AwayFromZero);
        var drawn = MSSansSerifSizes[0];
        foreach (var size in MSSansSerifSizes)
            if (size.Size <= pixels)
                drawn = size;
        return (drawn.Line, font.Bold ? drawn.BoldColumn : drawn.Column);
    }

    // MS Sans Serif's sizes in pixels, and at each the height of a line and the width of a column, regular and bold, as VB6
    // measures them.
    private static readonly (double Size, int Line, int Column, int BoldColumn)[] MSSansSerifSizes =
        [(11, 13, 5, 6), (13, 16, 7, 8), (16, 20, 8, 9), (18, 24, 9, 10), (20, 25, 10, 11), (24, 29, 12, 13), (26, 32, 14, 16),
            (32, 37, 16, 17), (36, 48, 18, 20), (48, 58, 24, 26)];

    // Whether this machine has a font of exactly this family: a platform may answer a family it lacks with a substitute. The NULs
    // are removed because a symbol font's name is read a byte at a time, so Wingdings comes back as "\0W\0i\0n...".
    private static bool IsInstalled(string family) =>
        family.Length > 0 && installed.GetOrAdd(family, name =>
            FontManager.Current.TryGetGlyphTypeface(new Typeface(name), out var typeface)
            && string.Equals(typeface.FamilyName.Replace("\0", ""), name, StringComparison.OrdinalIgnoreCase));
}
