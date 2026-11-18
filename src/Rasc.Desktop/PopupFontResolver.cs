using System.Windows;
using System.Windows.Media;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Chosen family is applied only to text. Backgrounds, glow, shadow, medal,
/// icons, frame geometry and accepted color palette remain immutable.
/// Missing Motiva Sans or Helvetica must NOT silently imply font fidelity.
/// </summary>
internal static class PopupFontResolver
{
    internal static FontFamily Resolve(PopupFontChoice choice, bool classic)
    {
        return choice switch
        {
            PopupFontChoice.Original => classic
                ? new FontFamily("Helvetica, Arial") : SystemFonts.MessageFontFamily,
            PopupFontChoice.MotivaSans => new FontFamily("Motiva Sans, Arial"),
            PopupFontChoice.Arial => new FontFamily("Arial"),
            PopupFontChoice.Helvetica => new FontFamily("Helvetica, Arial"),
            PopupFontChoice.SansSerif => SystemFonts.MessageFontFamily,
            _ => classic ? new FontFamily("Helvetica, Arial") : SystemFonts.MessageFontFamily
        };
    }

    internal static bool IsInstalled(PopupFontChoice choice)
    {
        var name = choice switch
        {
            PopupFontChoice.MotivaSans => "Motiva Sans",
            PopupFontChoice.Arial => "Arial",
            PopupFontChoice.Helvetica => "Helvetica",
            _ => null
        };
        if (name is null) return true;
        return Fonts.SystemFontFamilies.Any(f =>
            string.Equals(f.Source, name, StringComparison.OrdinalIgnoreCase));
    }

    internal static string UserLabel(PopupFontChoice choice) => choice switch
    {
        PopupFontChoice.Original => "Original RASC profile font",
        PopupFontChoice.MotivaSans => "Motiva Sans",
        PopupFontChoice.Arial => "Arial",
        PopupFontChoice.Helvetica => "Helvetica",
        PopupFontChoice.SansSerif => "sans-serif (Windows UI default)",
        _ => "Original"
    };
}
