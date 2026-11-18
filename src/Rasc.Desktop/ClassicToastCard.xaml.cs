using System.Windows.Controls;

namespace Rasc.Desktop;

public partial class ClassicToastCard : UserControl
{
    public ClassicToastCard(string title, string description)
    {
        InitializeComponent();
        Headline.Text = Sanitize(title);
        Description.Text = Sanitize(description);
    }

    internal void ApplyFontChoice(Rasc.Core.PopupFontChoice choice)
    {
        var family = PopupFontResolver.Resolve(choice, classic: true);
        Headline.FontFamily = family;
        Description.FontFamily = family;
    }

    private static string Sanitize(string? text)
    {
        var safe = (text ?? string.Empty).Replace('\r', ' ').Replace('\t', ' ');
        return safe.Length > 200 ? safe[..200] : safe;
    }
}
