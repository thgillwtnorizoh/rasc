using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Rasc.Core;

namespace Rasc.Desktop;

/// <summary>
/// Individually clipped 283x70 Modern *card content*, not a native HWND.
/// All active cards are children of one ModernToastWindow, guaranteeing that
/// card boundaries share ONE WPF compositor surface and device-pixel origin.
/// Original gradient, logo placement, icon/rare and progress content retained.
/// </summary>
internal sealed class ModernToastCard : UserControl
{
    private readonly Canvas _surface;

    internal ModernToastCard(ModernToastContent content)
    {
        Width = ModernToastScheduler.CardWidth;
        Height = ModernToastScheduler.CardHeight;
        IsHitTestVisible = false;

        _surface = new Canvas
        {
            Width = ModernToastScheduler.CardWidth,
            Height = ModernToastScheduler.CardHeight,
            // A SINGLE shared gradient is painted by ModernToastWindow.
            // Child cards have no opaque fill or per-card gradient seam.
            Background = Brushes.Transparent,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        // Decorative background and Steam watermark are drawn only ONCE
        // across the occupied wave by the shared parent HWND.
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

    internal static Brush CreateBackdropGradient()
    {
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


        return gradient;
    }

    internal static Canvas CreateWatermark()
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

    // Owner-approved visual target is Dataminer's CodePen "Steam Colors"
    // LegendaryAchievement, not the separate SteamCommunity conic-gradient
    // implementation. Its glow is a RADIAL gradient: nine source-matched
    // stops, nested 10-unit masks, 18s reverse mask and 6s forward light.
    // Source PNG alpha masks are isolated in this owner-test branch; see
    // Assets/SteamRareMasks/NOTICE.md for the unresolved rights gate.
    private static readonly Brush LegendaryRadial = CreateLegendaryRadial();

    private static Brush CreateLegendaryRadial()
    {
        LegendaryGlowSpec.Validate();
        var radial = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.5),
            GradientOrigin = new Point(0.5, 0.5),
            // CSS ellipse farthest-corner for a square: radius sqrt(.5).
            RadiusX = Math.Sqrt(0.5),
            RadiusY = Math.Sqrt(0.5)
        };
        foreach (var stop in LegendaryGlowSpec.RadialStops)
            radial.GradientStops.Add(new GradientStop(
                Color.FromArgb(stop.Alpha, stop.Red, stop.Green, stop.Blue),
                stop.Offset));
        radial.Freeze();
        return radial;
    }

    // The owner recovered the exact CodePen 64x64 alpha-mask PNGs. Retain
    // their smooth edge/brightness patterns instead of the old WPF hard-edged
    // subtractive rectangle and 14-spoke polygon (which visually erased
    // most moving light). Refer to Assets/SteamRareMasks/NOTICE.md for the
    // experimental asset-rights gate before a public release.

    private static FrameworkElement CreateLegendaryHalo()
    {
        var root = new Canvas
        {
            Width = LegendaryGlowSpec.OuterSize,
            Height = LegendaryGlowSpec.OuterSize,
            // A CSS mask constrains the rasterized element and its
            // overflowing descendants to the element's mask box. WPF's
            // OpacityMask alone does NOT enforce this box: C06 screenshot
            // diagnostics showed rotating rays reaching the title area.
            // This explicit WPF clip restores the source's masked box.
            ClipToBounds = true,
            Tag = "SteamRareGlowRoot",
            OpacityMask = ModernRareMaskAssets.AlphaBrush(ModernRareMaskAssets.Square),
            IsHitTestVisible = false
        };

        var container = new Canvas
        {
            Width = LegendaryGlowSpec.MiddleSize,
            Height = LegendaryGlowSpec.MiddleSize,
            RenderTransformOrigin = new Point(.5, .5),
            Tag = "SteamRareGlowRotatingMask",
            OpacityMask = ModernRareMaskAssets.AlphaBrush(ModernRareMaskAssets.Star),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(container, -LegendaryGlowSpec.MaskPadding);
        Canvas.SetTop(container, -LegendaryGlowSpec.MaskPadding);

        var light = new Border
        {
            Width = LegendaryGlowSpec.LightSize,
            Height = LegendaryGlowSpec.LightSize,
            Background = LegendaryRadial,
            RenderTransformOrigin = new Point(.5, .5),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(light, -LegendaryGlowSpec.MaskPadding);
        Canvas.SetTop(light, -LegendaryGlowSpec.MaskPadding);

        var spinMask = new RotateTransform();
        container.RenderTransform = spinMask;
        spinMask.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(0, -360, new Duration(LegendaryGlowSpec.MaskReverse))
            {
                RepeatBehavior = RepeatBehavior.Forever
            });

        var spinLight = new RotateTransform();
        light.RenderTransform = spinLight;
        spinLight.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, new Duration(LegendaryGlowSpec.LightForward))
            {
                RepeatBehavior = RepeatBehavior.Forever
            });

        container.Children.Add(light);
        root.Children.Add(container);
        return root;
    }

    private static FrameworkElement CreateIcon(bool rare)
    {
        // The CodePen's real wrapper is 48x48. The former extra 54x54
        // WPF canvas introduced its own geometry and shadow layer offsets.
        // Its only outer clipping is the accepted 283x70 toast viewport.
        // Place its 68px masked glow at x=0..68 (48px icon x=10..58).
        const double wrapperLeft = 10;
        const double wrapperTop = 11;

        var wrapper = new Canvas
        {
            Width = LegendaryGlowSpec.IconSize,
            Height = LegendaryGlowSpec.IconSize,
            ClipToBounds = false,
            Tag = rare ? "LegendaryIconWrapper" : "OrdinaryIconWrapper",
            IsHitTestVisible = false
        };
        Canvas.SetLeft(wrapper, wrapperLeft);
        Canvas.SetTop(wrapper, wrapperTop);

        if (rare)
        {
            // Exact CodePen nesting: fixed 68px root at -10,-10; a
            // reverse-spinning 88px star at another -10; and the
            // forward-spinning 108px radial light at one more -10.
            var halo = CreateLegendaryHalo();
            Canvas.SetLeft(halo, -LegendaryGlowSpec.MaskPadding);
            Canvas.SetTop(halo, -LegendaryGlowSpec.MaskPadding);
            wrapper.Children.Add(halo);

            // BOTH shadows are painted onto ONE precomposed transparent
            // layer, behind the one real 48px icon. This avoids the two
            // independently OPAQUE WPF Border/DropShadowEffect render
            // passes that made the C05 screenshot too aggressive.
            var shadow = new Image
            {
                Width = ModernCssBoxShadow.SpriteSize,
                Height = ModernCssBoxShadow.SpriteSize,
                Source = ModernCssBoxShadow.Shadow,
                Tag = "CodePenDualBoxShadow",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(shadow, -ModernCssBoxShadow.IconOrigin);
            Canvas.SetTop(shadow, -ModernCssBoxShadow.IconOrigin);
            wrapper.Children.Add(shadow);
        }

        // The actual RASC achievement icon is artwork, not the CodePen's
        // demonstration-only blank #222 fakeIcon. Keep its existing dark
        // face and yellow rare star to isolate glow fidelity from icon art.
        var icon = new Border
        {
            Width = LegendaryGlowSpec.IconSize,
            Height = LegendaryGlowSpec.IconSize,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Color.FromRgb(13, 16, 22)),
            Tag = "AchievementIconFace",
            Child = new TextBlock
            {
                Text = "★", // Deliberate preview icon until catalog assets.
                FontSize = 23,
                Foreground = rare
                    ? new SolidColorBrush(Color.FromRgb(255, 216, 70))
                    : new SolidColorBrush(Color.FromRgb(179, 196, 222)),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        wrapper.Children.Add(icon);
        return wrapper;
    }

    internal void Apply(ModernToastFrame frame)
    {
        // The bottommost card can be partially visible while sliding in.
        // Every card's content remains independently clipped, but the card
        // itself is part of the common parent HWND (no DWM per-card edges).
        var visible = Math.Clamp(-frame.BottomRelativeY,
            0.0, ModernToastScheduler.CardHeight);
        _surface.Clip = new RectangleGeometry(new Rect(
            0, 0, ModernToastScheduler.CardWidth, visible));
    }
}
