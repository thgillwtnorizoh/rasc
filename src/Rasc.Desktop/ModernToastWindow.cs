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
        // The C03 hand-drawn rings were not even recognizably Steam. Use
        // the actual geometric Steam mark, not an improvised mechanism.
        // Vector path: simple-icons/simple-icons, icons/steam.svg (CC0);
        // the Steam mark remains Valve's trademark. This is a faint,
        // noninteractive historical UI reference, not Steam affiliation.
        // Wave-specific logo Y offsets/opacity and 170-unit footprint are
        // UNCHANGED from the user-approved C02 positioning.
        var canvas = new Canvas
        {
            Width = 170,
            Height = ModernWatermarkLayout.LogoHeight,
            Opacity = 0.09,
            IsHitTestVisible = false
        };
        var logo = new Path
        {
            Width = 170,
            Height = 170,
            Stretch = Stretch.Fill,
            Fill = new SolidColorBrush(Color.FromRgb(118, 134, 154)),
            Data = Geometry.Parse("M11.979 0C5.678 0 .511 4.86.022 11.037l6.432 2.658c.545-.371 1.203-.59 1.912-.59.063 0 .125.004.188.006l2.861-4.142V8.91c0-2.495 2.028-4.524 4.524-4.524 2.494 0 4.524 2.031 4.524 4.527s-2.03 4.525-4.524 4.525h-.105l-4.076 2.911c0 .052.004.105.004.159 0 1.875-1.515 3.396-3.39 3.396-1.635 0-3.016-1.173-3.331-2.727L.436 15.27C1.862 20.307 6.486 24 11.979 24c6.627 0 11.999-5.373 11.999-12S18.605 0 11.979 0zM7.54 18.21l-1.473-.61c.262.543.714.999 1.314 1.25 1.297.539 2.793-.076 3.332-1.375.263-.63.264-1.319.005-1.949s-.75-1.121-1.377-1.383c-.624-.26-1.29-.249-1.878-.03l1.523.63c.956.4 1.409 1.5 1.009 2.455-.397.957-1.497 1.41-2.454 1.012H7.54zm11.415-9.303c0-1.662-1.353-3.015-3.015-3.015-1.665 0-3.015 1.353-3.015 3.015 0 1.665 1.35 3.015 3.015 3.015 1.663 0 3.015-1.35 3.015-3.015zm-5.273-.005c0-1.252 1.013-2.266 2.265-2.266 1.249 0 2.266 1.014 2.266 2.266 0 1.251-1.017 2.265-2.266 2.265-1.253 0-2.265-1.014-2.265-2.265z")
        };
        canvas.Children.Add(logo);
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

    private static readonly ImageSource RareConicSource = CreateRareConicSource();

    /// <summary>
    /// WPF reconstruction of the Steam UI's rare glow: a rotating *conic
    /// gradient* (6s foreground, 18s reverse blur), masked to a STATIONARY
    /// square halo around the achievement bitmap. Unlike the C03 five-dot
    /// effect, only light moves: the achievement frame itself never rotates.
    /// Gradient stops/speeds trace the Steam UI CSS (P003-C03-C01 research);
    /// blend mode and exact rare-toasts asset remain unverified.
    /// </summary>
    private static ImageSource CreateRareConicSource()
    {
        const int steps = 96;
        const double size = 96;
        var center = new Point(size / 2, size / 2);
        var drawing = new DrawingGroup();
        for (var i = 0; i < steps; i++)
        {
            var a = i * Math.PI * 2 / steps - Math.PI / 2;
            var b = (i + 1) * Math.PI * 2 / steps - Math.PI / 2;
            var radius = size * 0.75;
            var p1 = new Point(center.X + radius * Math.Cos(a),
                center.Y + radius * Math.Sin(a));
            var p2 = new Point(center.X + radius * Math.Cos(b),
                center.Y + radius * Math.Sin(b));
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(center, true, true);
                ctx.LineTo(p1, true, false);
                ctx.ArcTo(p2, new Size(radius, radius), 0, false,
                    SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();
            var value = ConicGold((i + 0.5) / steps);
            var brush = new SolidColorBrush(Color.FromArgb(
                (byte)Math.Round(value * 255), 255, 184, 78));
            brush.Freeze();
            drawing.Children.Add(new GeometryDrawing(brush, null, geometry));
        }
        drawing.Freeze();
        var img = new DrawingImage(drawing);
        img.Freeze();
        return img;
    }

    private static double ConicGold(double fraction)
    {
        // Source-inspired repeating-conic gradient:
        // transparent at 6%; translucent 0/10/35/60/100%;
        // bright bands near 26/40/82% (Steam app details rare glow).
        double[] positions = [0, .06, .10, .26, .35, .40, .60, .82, 1];
        double[] values = [.178, 0, .178, 1, .178, 1, .178, 1, .178];
        for (var i = 1; i < positions.Length; i++)
        {
            if (fraction > positions[i]) continue;
            var t = (fraction - positions[i - 1]) /
                (positions[i] - positions[i - 1]);
            return values[i - 1] + (values[i] - values[i - 1]) * t;
        }
        return values[^1];
    }

    private static Geometry CreateHaloMask()
    {
        var geometry = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, 66, 66)),
            new RectangleGeometry(new Rect(9, 9, 48, 48)));
        geometry.Freeze();
        return geometry;
    }

    private static Image MakeRotatingGlow(double seconds, bool reverse,
        double opacity, double blurRadius)
    {
        var image = new Image
        {
            Source = RareConicSource,
            Width = 96,
            Height = 96,
            Opacity = opacity,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(.5, .5),
            Effect = new BlurEffect { Radius = blurRadius }
        };
        var rotation = new RotateTransform();
        image.RenderTransform = rotation;
        rotation.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(0, reverse ? -360 : 360,
                new Duration(TimeSpan.FromSeconds(seconds)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            });
        return image;
    }

    private static FrameworkElement CreateIcon(bool rare)
    {
        var iconCanvas = new Canvas
        {
            Width = 54,
            Height = 54,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(iconCanvas, 6);
        Canvas.SetTop(iconCanvas, 8);

        // NO blue ordinary frame, NO solid yellow rare frame.
        if (rare)
        {
            // Full 66x66 stationary mask, inset around the 48x48 icon.
            // Rotating 96x96 conic layers are larger than the mask to cover
            // its four corners at every angle; no rotating square artifacts.
            var halo = new Canvas
            {
                Width = 66,
                Height = 66,
                ClipToBounds = true,
                IsHitTestVisible = false,
                OpacityMask = new DrawingBrush(new GeometryDrawing(
                    Brushes.White, null, CreateHaloMask()))
                {
                    Stretch = Stretch.Fill
                }
            };
            Canvas.SetLeft(halo, -6);
            Canvas.SetTop(halo, -6);

            var slowHalo = MakeRotatingGlow(18, true, .48, 5);
            Canvas.SetLeft(slowHalo, -15);
            Canvas.SetTop(slowHalo, -15);
            halo.Children.Add(slowHalo);

            var sharpGlow = MakeRotatingGlow(6, false, .83, 1.2);
            Canvas.SetLeft(sharpGlow, -15);
            Canvas.SetTop(sharpGlow, -15);
            halo.Children.Add(sharpGlow);
            iconCanvas.Children.Add(halo);
        }

        var image = new Border
        {
            Width = 48,
            Height = 48,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Color.FromRgb(13, 16, 22)),
            Child = new TextBlock
            {
                Text = "★", // Explicit placeholder until CS:GO icon catalog.
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
        iconCanvas.Children.Add(image);
        return iconCanvas;
    }

    internal void Apply(ModernToastFrame frame, double scale,
        Rect desktopWorkArea, ForegroundWindowPlacement.Target? fullscreen)
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

        // Only focused fullscreen/borderless windows override the original
        // C02 bottom-right desktop/taskbar position. Normal apps NEVER
        // control popup location or scale.
        _lastOffsetDip = frame.BottomRelativeY;
        if (fullscreen is { } target)
            target.Place(this, ModernToastScheduler.CardWidth,
                ModernToastScheduler.CardHeight, scale, _lastOffsetDip);
        else
        {
            Left = desktopWorkArea.Right - Width;
            Top = desktopWorkArea.Bottom + _lastOffsetDip * scale;
        }

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
            var t = ForegroundWindowPlacement.CaptureFullscreen();
            if (t is { } fullscreen)
                fullscreen.Place(this, ModernToastScheduler.CardWidth,
                    ModernToastScheduler.CardHeight, _scale, _lastOffsetDip);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
