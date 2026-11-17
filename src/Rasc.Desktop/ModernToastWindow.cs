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
    private bool _closed;

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
        // Confirmed in Steam UI snapshot (3c658f496c3bc713...):
        // notifications 11374.js renders l.Achievement immediately before
        // (current/max); 35488.js defines the 36x36 inline SVG. Preserve its
        // silhouette and original viewBox padding at this 12-DIP UI size.
        // The geometry is a source-informed compatibility rendering, not a
        // bundled Steam client image/font or claimed redistribution license.
        const string badge =
            "M30 30.05 H26 L24 34.05 L20.11 27.57 L22.9 24.87 L26.9 24.81 Z " +
            "M13.1 24.87 L9.1 24.81 L6 30.05 H10 L12 34.05 L15.89 27.57 Z " +
            "M18 2.05 L21.25 5.2 L25.78 5.27 L25.85 9.8 L29 13.05 " +
            "L25.85 16.3 L25.78 20.83 L21.25 20.9 L18 24.05 " +
            "L14.75 20.9 L10.22 20.83 L10.15 16.3 L7 13.05 " +
            "L10.15 9.8 L10.22 5.27 L14.75 5.2 Z";
        var silver = new SolidColorBrush(Color.FromRgb(155, 169, 184));
        silver.Freeze();
        var glyph = new CombinedGeometry(GeometryCombineMode.Exclude,
            Geometry.Parse(badge),
            new EllipseGeometry(new Point(18, 13.05), 4.5, 4.5));
        glyph.Freeze();
        var drawing = new Canvas { Width = 36, Height = 36, IsHitTestVisible = false };
        drawing.Children.Add(new Path { Data = glyph, Fill = silver });
        return new Viewbox
        {
            Width = 12,
            Height = 12,
            Stretch = Stretch.Uniform,
            Child = drawing,
            IsHitTestVisible = false
        };
    }

    // Steam Community rare-asset CSS uses transparent and bright gold
    // repeating-conic stops, a 6s forward gradient rotation, a separate
    // 18s reverse star-mask rotation and a square outer mask. These colors
    // are reference-derived; the original native toast's compositor
    // equivalence remains unverified.
    private static readonly (double At, byte Alpha, byte Green, byte Blue)[] RareGoldStops =
    [
        (0.00, 45, 217, 0),
        (0.06, 0, 184, 78),
        (0.10, 45, 217, 0),
        (0.26, 255, 184, 78),
        (0.35, 45, 217, 0),
        (0.40, 255, 184, 78),
        (0.60, 45, 217, 0),
        (0.82, 255, 184, 78),
        (1.00, 45, 217, 0)
    ];

    private static readonly ImageSource RareConicSource = CreateRareConicSource();

    private static ImageSource CreateRareConicSource()
    {
        const int steps = 192;
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
            var brush = new SolidColorBrush(ConicGold((i + 0.5) / steps));
            brush.Freeze();
            drawing.Children.Add(new GeometryDrawing(brush, null, geometry));
        }
        drawing.Freeze();
        var img = new DrawingImage(drawing);
        img.Freeze();
        return img;
    }

    private static Color ConicGold(double fraction)
    {
        static byte Lerp(byte from, byte to, double t) =>
            (byte)Math.Round(from + (to - from) * t);
        for (var i = 1; i < RareGoldStops.Length; i++)
        {
            var next = RareGoldStops[i];
            if (fraction > next.At) continue;
            var prior = RareGoldStops[i - 1];
            var t = (fraction - prior.At) / (next.At - prior.At);
            return Color.FromArgb(Lerp(prior.Alpha, next.Alpha, t), 255,
                Lerp(prior.Green, next.Green, t),
                Lerp(prior.Blue, next.Blue, t));
        }
        return Color.FromArgb(45, 255, 217, 0);
    }

    private static Geometry CreateRotatingStarMask()
    {
        // Independently constructed soft-edged radial star approximation.
        // The actual Valve star-mask PNG has NOT been redistributed.
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            for (var i = 0; i < 24; i++)
            {
                var angle = 2 * Math.PI * i / 24 - Math.PI / 2;
                var radius = i % 2 == 0 ? 38.0 : 27.0;
                var pt = new Point(33 + radius * Math.Cos(angle),
                    33 + radius * Math.Sin(angle));
                if (i == 0) ctx.BeginFigure(pt, true, true);
                else ctx.LineTo(pt, true, false);
            }
        }
        g.Freeze();
        return g;
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

            // CSS-like nested mask: the star-shaped clipping field turns
            // backward every 18 seconds, while the conic light inside turns
            // forward every 6 seconds. The OUTER square ring stays fixed.
            var star = new Canvas
            {
                Width = 66, Height = 66,
                OpacityMask = new DrawingBrush(new GeometryDrawing(
                    Brushes.White, null, CreateRotatingStarMask()))
                {
                    Stretch = Stretch.Fill
                },
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform()
            };
            ((RotateTransform)star.RenderTransform).BeginAnimation(
                RotateTransform.AngleProperty,
                new DoubleAnimation(0, -360, new Duration(TimeSpan.FromSeconds(18)))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                });

            var diffuse = MakeRotatingGlow(6, false, .52, 3.4);
            Canvas.SetLeft(diffuse, -15);
            Canvas.SetTop(diffuse, -15);
            star.Children.Add(diffuse);

            var sharp = MakeRotatingGlow(6, false, .88, .8);
            Canvas.SetLeft(sharp, -15);
            Canvas.SetTop(sharp, -15);
            star.Children.Add(sharp);
            halo.Children.Add(star);
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
        if (rare)
        {
            // Original-style rare icon produces a diffuse golden box shadow,
            // never an opaque yellow border around the image.
            image.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(255, 184, 78),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = .43
            };
        }
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
            if (_closed || !IsVisible) return;
            var t = ForegroundWindowPlacement.CaptureFullscreen();
            if (t is { } fullscreen)
                fullscreen.Place(this, ModernToastScheduler.CardWidth,
                    ModernToastScheduler.CardHeight, _scale, _lastOffsetDip);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        base.OnClosed(e);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
