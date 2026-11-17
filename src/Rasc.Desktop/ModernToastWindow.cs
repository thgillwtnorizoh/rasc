using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// A separate non-activating desktop window for each modern toast, matching
/// the supplied JS's per-window MoveTo architecture at a structural level.
/// Visual content is an unlicensed-safe WPF approximation, not a captured
/// stock Steam achievement card. This is NOT an in-game overlay.
/// </summary>
internal sealed class ModernToastWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExToolWindow = 0x80;
    private const long WsExNoActivate = 0x08000000;
    private readonly Border _panel;
    private double _scale = 1;

    internal ModernToastWindow(string title, string description)
    {
        Width = ModernToastScheduler.CardWidth;
        Height = ModernToastScheduler.CardHeight;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Focusable = false;
        IsHitTestVisible = false;

        var background = new RadialGradientBrush
        {
            Center = new Point(0.4, 0.2),
            GradientOrigin = new Point(0.4, 0.2),
            RadiusX = 0.9,
            RadiusY = 1.1,
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#23262E"), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#0E141B"), 1)
            }
        };
        background.Freeze();
        var grid = new Grid { Width = ModernToastScheduler.CardWidth,
            Height = ModernToastScheduler.CardHeight };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new Border
        {
            Width = 48, Height = 48, Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(49, 57, 70)),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(76, 85, 99)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "★", FontSize = 22,
                Foreground = new SolidColorBrush(Color.FromRgb(188, 202, 224)),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new StackPanel
        {
            Margin = new Thickness(4, 0, 7, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        });
        text.Children.Add(new TextBlock
        {
            Text = description,
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(177, 195, 214)),
            FontSize = 11.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        _panel = new Border
        {
            Width = ModernToastScheduler.CardWidth,
            Height = ModernToastScheduler.CardHeight,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(48, 58, 72)),
            Background = background,
            Child = grid,
            SnapsToDevicePixels = true
        };
        Content = _panel;
    }

    internal void Apply(ModernToastFrame frame, double scale, Rect workArea)
    {
        if (!double.IsFinite(scale) || scale <= 0) scale = 1;
        if (_scale != scale)
        {
            _scale = scale;
            _panel.LayoutTransform = new ScaleTransform(scale, scale);
            Width = ModernToastScheduler.CardWidth * scale;
            Height = ModernToastScheduler.CardHeight * scale;
        }
        Left = workArea.Right - Width;
        Top = workArea.Bottom + frame.BottomRelativeY * scale;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        var flags = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        _ = SetWindowLongPtr(hwnd, GwlExStyle,
            new IntPtr(flags | WsExTransparent | WsExToolWindow | WsExNoActivate));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
