using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Owner-target modern NORMAL desktop achievement card. Nonactivating, input
/// transparent, and no outer border. Graphics are independently drawn WPF
/// shapes: not redistributed Steam logos/icons or a claim of original CSS parity.
/// Only the portion above the desktop work-area bottom is painted: the common
/// entering origin remains invisible even if our window is above the taskbar.
/// </summary>
internal sealed class ModernToastWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExToolWindow = 0x80;
    private const long WsExNoActivate = 0x08000000;
    private readonly Canvas _surface;
    private readonly Canvas _watermark;
    private double _scale = 1;

    internal ModernToastWindow(ModernToastContent content)
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

        var gradient = new RadialGradientBrush
        {
            Center = new Point(0.48, 0.26),
            GradientOrigin = new Point(0.48, 0.26),
            RadiusX = 0.85,
            RadiusY = 1.1,
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#23262E"), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#0E141B"), 1)
            }
        };
        gradient.Freeze();

        _surface = new Canvas
        {
            Width = ModernToastScheduler.CardWidth,
            Height = ModernToastScheduler.CardHeight,
            Background = gradient,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        // The decoration is ONE long geometric watermark shared across a
        // wave; each 70px window clips only its own slot. It is deliberately
        // approximate until the original hashed CSS can be re-examined.
        _watermark = CreateWatermark();
        Canvas.SetLeft(_watermark, 143);
        _surface.Children.Add(_watermark);

        _surface.Children.Add(CreateIcon(content.IsRare));

        var headline = new TextBlock
        {
            Text = content.Name,
            Width = 204,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(211, 216, 223)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(headline, 69);
        Canvas.SetTop(headline, content.Kind == ModernToastKind.Progress ? 13 : 18);
        _surface.Children.Add(headline);

        if (content.Kind == ModernToastKind.Progress)
        {
            var numeric = new TextBlock
            {
                Text = content.NumericProgress,
                Foreground = new SolidColorBrush(Color.FromRgb(157, 168, 182)),
                FontSize = 11,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(numeric, 69);
            Canvas.SetTop(numeric, 31);
            _surface.Children.Add(numeric);

            var track = new Border
            {
                Width = 202, Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromRgb(54, 65, 78)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(track, 69);
            Canvas.SetTop(track, 49);
            _surface.Children.Add(track);

            if (content.ProgressFraction > 0)
            {
                var fill = new Border
                {
                    Width = 202 * content.ProgressFraction,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromRgb(24, 148, 245)),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(fill, 69);
                Canvas.SetTop(fill, 49);
                _surface.Children.Add(fill);
            }
        }
        else
        {
            var description = new TextBlock
            {
                Text = content.Description,
                Width = 204,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 164)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(description, 69);
            Canvas.SetTop(description, 36);
            _surface.Children.Add(description);
        }

        Content = _surface;
    }

    private static Canvas CreateWatermark()
    {
        // Independently modeled Steam-like geometry, not extracted artwork.
        // Low alpha is intentional; original asset/positioning is unverified.
        var canvas = new Canvas
        {
            Width = 170, Height = ModernWatermarkLayout.LogoHeight,
            Opacity = 0.09,
            IsHitTestVisible = false
        };
        var ink = new SolidColorBrush(Color.FromRgb(118, 134, 154));
        ink.Freeze();
        var ring = new Ellipse
        {
            Width = 137, Height = 137, Stroke = ink, StrokeThickness = 19,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(ring, 52);
        Canvas.SetTop(ring, 0);
        canvas.Children.Add(ring);
        var arm = new Line
        {
            X1 = 23, Y1 = 138, X2 = 98, Y2 = 68,
            Stroke = ink, StrokeThickness = 19,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(arm);
        var hub = new Ellipse
        {
            Width = 54, Height = 54, Stroke = ink, StrokeThickness = 9,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(hub, 3);
        Canvas.SetTop(hub, 112);
        canvas.Children.Add(hub);
        var axle = new Ellipse
        {
            Width = 31, Height = 31, Stroke = ink, StrokeThickness = 6,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(axle, 94);
        Canvas.SetTop(axle, 53);
        canvas.Children.Add(axle);
        return canvas;
    }

    private static FrameworkElement CreateIcon(bool rare)
    {
        var iconCanvas = new Canvas
        {
            Width = 54, Height = 54,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(iconCanvas, 6);
        Canvas.SetTop(iconCanvas, 8);

        var borderColor = rare
            ? new SolidColorBrush(Color.FromRgb(247, 192, 41))
            : new SolidColorBrush(Color.FromRgb(76, 85, 99));
        var frame = new Border
        {
            Width = 48, Height = 48,
            BorderThickness = new Thickness(rare ? 2 : 1),
            BorderBrush = borderColor,
            Background = new SolidColorBrush(Color.FromRgb(13, 16, 22)),
            Child = new TextBlock
            {
                Text = "★",
                FontSize = 23,
                Foreground = rare
                    ? new SolidColorBrush(Color.FromRgb(255, 216, 70))
                    : new SolidColorBrush(Color.FromRgb(179, 196, 222)),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Canvas.SetLeft(frame, 3);
        Canvas.SetTop(frame, 3);
        if (rare)
        {
            frame.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(255, 174, 22),
                BlurRadius = 13,
                ShadowDepth = 0,
                Opacity = 0.85
            };
            frame.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0.65,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(1100),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            });
        }
        iconCanvas.Children.Add(frame);
        return iconCanvas;
    }

    internal void Apply(ModernToastFrame frame, double scale, Rect workArea)
    {
        if (!double.IsFinite(scale) || scale <= 0) scale = 1;
        if (_scale != scale)
        {
            _scale = scale;
            _surface.LayoutTransform = new ScaleTransform(scale, scale);
            Width = ModernToastScheduler.CardWidth * scale;
            Height = ModernToastScheduler.CardHeight * scale;
        }

        Canvas.SetTop(_watermark,
            ModernWatermarkLayout.TopInsideCard(frame.WaveSize, frame.Slot));

        Left = workArea.Right - Width;
        Top = workArea.Bottom + frame.BottomRelativeY * scale;

        // The taskbar must cover all pixels beneath WorkArea.Bottom, even if
        // the separate WPF topmost HWND would otherwise paint over it.
        var visible = Math.Clamp((workArea.Bottom - Top) / scale,
            0.0, ModernToastScheduler.CardHeight);
        _surface.Clip = new RectangleGeometry(new Rect(
            0, 0, ModernToastScheduler.CardWidth, visible));
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
