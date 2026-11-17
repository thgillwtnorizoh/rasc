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
    private double _lastOffsetDip;

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
            Canvas.SetLeft(numeric, 84);
            Canvas.SetTop(numeric, 31);
            _surface.Children.Add(numeric);
            // The original progress screenshot shows a small medal/rosette
            // immediately before the numeric "(current/max)" label.
            var medal = CreateProgressMedal();
            Canvas.SetLeft(medal, 69);
            Canvas.SetTop(medal, 33);
            _surface.Children.Add(medal);

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
        // Steam-like mechanical wheel/rod/hub drawn from simple geometry.
        // This supersedes the C02 bare-circles placeholder without embedding
        // copyrighted Valve artwork. Positioning is deliberately unchanged:
        // one continuous faint watermark clipped across 1/2/3 popup slots.
        // Still not source-equivalent without the packaged CSS/svg asset.
        var canvas = new Canvas
        {
            Width = 170, Height = ModernWatermarkLayout.LogoHeight,
            Opacity = 0.09,
            IsHitTestVisible = false
        };
        var ink = new SolidColorBrush(Color.FromRgb(118, 134, 154));
        ink.Freeze();

        // Main diagonal connecting rod must sit underneath the wheel disks.
        var rod = new Line
        {
            X1 = 30, Y1 = 139, X2 = 122, Y2 = 62,
            Stroke = ink, StrokeThickness = 18,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        canvas.Children.Add(rod);

        // Steam-logo-like upper-right wheel: a circular body with an open
        // inner channel. Difference geometry, not layered dark paint, means
        // original radial background remains visible through the opening.
        var disc = new Path
        {
            Fill = ink,
            Data = new CombinedGeometry(GeometryCombineMode.Exclude,
                new EllipseGeometry(new Rect(78, 0, 118, 118)),
                new EllipseGeometry(new Rect(96, 18, 82, 82)))
        };
        canvas.Children.Add(disc);

        var axle = new Ellipse
        {
            Width = 43, Height = 43, Fill = ink
        };
        Canvas.SetLeft(axle, 115);
        Canvas.SetTop(axle, 38);
        canvas.Children.Add(axle);

        var smallHub = new Path
        {
            Fill = ink,
            Data = new CombinedGeometry(GeometryCombineMode.Exclude,
                new EllipseGeometry(new Rect(2, 109, 57, 57)),
                new EllipseGeometry(new Rect(17, 124, 27, 27)))
        };
        canvas.Children.Add(smallHub);
        return canvas;
    }

    private static FrameworkElement CreateProgressMedal()
    {
        // Tiny monochrome award marker from owner screenshot, modeled with
        // original vector primitives rather than emoji or bundled client art.
        var medal = new Canvas { Width = 12, Height = 12, IsHitTestVisible = false };
        var silver = new SolidColorBrush(Color.FromRgb(152, 167, 181));
        silver.Freeze();
        var ribbon = new Polygon
        {
            Points = new PointCollection
            {
                new(3.1, 1), new(5, 4), new(7.2, 1),
                new(9.6, 1), new(7.7, 6), new(4.2, 6), new(1, 1)
            },
            Fill = silver,
            Opacity = 0.65
        };
        medal.Children.Add(ribbon);
        var rosette = new Ellipse
        {
            Width = 7, Height = 7, Stroke = silver,
            StrokeThickness = 1.4, Fill = Brushes.Transparent
        };
        Canvas.SetLeft(rosette, 2.3);
        Canvas.SetTop(rosette, 4.5);
        medal.Children.Add(rosette);
        var dot = new Ellipse { Width = 2, Height = 2, Fill = silver };
        Canvas.SetLeft(dot, 4.9);
        Canvas.SetTop(dot, 7);
        medal.Children.Add(dot);
        return medal;
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

        // There is NO ordinary blue icon frame, and NO solid gold rarity
        // frame. The actual award bitmap will be supplied by a future catalog.
        var image = new Border
        {
            Width = 48, Height = 48,
            BorderThickness = new Thickness(0),
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
        Canvas.SetLeft(image, 3);
        Canvas.SetTop(image, 3);
        if (rare)
        {
            // Community-style rotating, softly diffused gold highlight.
            // Not claimed byte-for-byte equivalent to Steam's conic-gradient
            // or rare-achievement mask CSS until native toast code is traced.
            image.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(255, 183, 32),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.85
            };
            var glints = new Canvas
            {
                Width = 54, Height = 54, IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Opacity = 0.65
            };
            for (var i = 0; i < 5; i++)
            {
                var angle = i * Math.PI * 2 / 5;
                var glint = new Ellipse
                {
                    Width = 9, Height = 9,
                    Fill = new RadialGradientBrush(
                        Color.FromArgb(210, 255, 221, 90),
                        Color.FromArgb(0, 255, 184, 78))
                };
                Canvas.SetLeft(glint, 27 + Math.Cos(angle) * 24 - 4.5);
                Canvas.SetTop(glint, 27 + Math.Sin(angle) * 24 - 4.5);
                glints.Children.Add(glint);
            }
            glints.RenderTransform = new RotateTransform();
            glints.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(6)))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });
            iconCanvas.Children.Add(glints);
        }
        iconCanvas.Children.Add(image);
        return iconCanvas;
    }

    internal void Apply(ModernToastFrame frame, double scale,
        ForegroundWindowPlacement.Target target)
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

        // Native physical-pixel anchoring follows the foreground application's
        // monitor. In windowed mode clip at visible window/work area; in
        // fullscreen (including borderless) the monitor edge is used.
        _lastOffsetDip = frame.BottomRelativeY;
        target.Place(this, ModernToastScheduler.CardWidth,
            ModernToastScheduler.CardHeight, scale, _lastOffsetDip);

        // Use unscaled logical design units for clipping. All cards in a wave
        // start at the same invisible y=0 below the app and enter above it.
        var visible = Math.Clamp(-frame.BottomRelativeY,
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
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            // WPF creates the HWND before the first device-pixel positioning
            // pass; the host continues updating the physical anchor.
            if (!IsLoaded) return;
            var t = ForegroundWindowPlacement.Capture();
            t.Place(this, ModernToastScheduler.CardWidth,
                ModernToastScheduler.CardHeight, _scale, _lastOffsetDip);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
