using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BDFR.LogonUI.Demo;

public sealed class WelcomeAnimationOverlay : Grid
{
    private readonly Grid _scene = new();
    private readonly TextBlock _title = new();
    private readonly TextBlock _subtitle = new();

    public WelcomeAnimationOverlay()
    {
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = true;
        ClipToBounds = true;

        Children.Add(_scene);

        _title.Text = "به BDFR خوش آمدید";
        _title.FontSize = 42;
        _title.FontWeight = FontWeights.SemiBold;
        _title.Foreground = Brushes.White;
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _title.TextAlignment = TextAlignment.Center;
        _title.FlowDirection = FlowDirection.RightToLeft;
        _title.Opacity = 0;

        _subtitle.Text = "ورود موفق بود";
        _subtitle.FontSize = 15;
        _subtitle.Foreground = new SolidColorBrush(Color.FromRgb(210, 222, 235));
        _subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        _subtitle.VerticalAlignment = VerticalAlignment.Center;
        _subtitle.TextAlignment = TextAlignment.Center;
        _subtitle.Margin = new Thickness(0, 76, 0, 0);
        _subtitle.FlowDirection = FlowDirection.RightToLeft;
        _subtitle.Opacity = 0;

        Children.Add(_title);
        Children.Add(_subtitle);
    }

    public async Task PlayAsync(
        WelcomeAnimationStyle style,
        double durationSeconds,
        string? title = null,
        string? subtitle = null)
    {
        durationSeconds = Math.Clamp(durationSeconds, 1.5, 8.0);

        _title.Text = string.IsNullOrWhiteSpace(title)
            ? "به BDFR خوش آمدید"
            : title;

        _subtitle.Text = string.IsNullOrWhiteSpace(subtitle)
            ? "ورود موفق بود"
            : subtitle;

        Visibility = Visibility.Visible;
        Opacity = 1;
        _scene.Children.Clear();
        _scene.Background = Brushes.Black;
        _title.Opacity = 0;
        _subtitle.Opacity = 0;
        _title.RenderTransform = Transform.Identity;
        _subtitle.RenderTransform = Transform.Identity;

        BuildScene(style, durationSeconds);
        AnimateText(durationSeconds);

        var fadeOutStart = Math.Max(.9, durationSeconds - .55);
        Begin(
            this,
            OpacityProperty,
            1,
            0,
            fadeOutStart,
            .5,
            new QuadraticEase { EasingMode = EasingMode.EaseIn });

        await Task.Delay(TimeSpan.FromSeconds(durationSeconds + .05));

        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        Visibility = Visibility.Collapsed;
        _scene.Children.Clear();
    }

    private void BuildScene(
        WelcomeAnimationStyle style,
        double duration)
    {
        switch (style)
        {
            case WelcomeAnimationStyle.PersianSunrise:
                BuildPersianSunrise(duration);
                break;

            case WelcomeAnimationStyle.ElegantFade:
                BuildElegantFade(duration);
                break;

            case WelcomeAnimationStyle.ParticleBloom:
                BuildParticleBloom(duration);
                break;

            case WelcomeAnimationStyle.AuroraFlow:
                BuildAuroraFlow(duration);
                break;

            case WelcomeAnimationStyle.GlassPanels:
                BuildGlassPanels(duration);
                break;

            case WelcomeAnimationStyle.TypographyWave:
                BuildTypographyWave(duration);
                break;

            case WelcomeAnimationStyle.NatureSeasons:
                BuildNatureSeasons(duration);
                break;

            case WelcomeAnimationStyle.MinimalCircle:
                BuildMinimalCircle(duration);
                break;

            case WelcomeAnimationStyle.CityToDesktop:
                BuildCityToDesktop(duration);
                break;

            default:
                BuildElegantFade(duration);
                break;
        }
    }

    private void BuildPersianSunrise(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(10, 24, 48),
            Color.FromRgb(89, 55, 73),
            Color.FromRgb(223, 125, 74));

        var glow = new Ellipse
        {
            Width = 560,
            Height = 560,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, -420),
            Fill = Radial(
                Color.FromArgb(210, 255, 207, 126),
                Color.FromArgb(0, 255, 156, 82))
        };
        _scene.Children.Add(glow);

        var sun = new Ellipse
        {
            Width = 78,
            Height = 78,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 105),
            Fill = new SolidColorBrush(Color.FromRgb(255, 214, 140)),
            RenderTransform = new TranslateTransform(0, 170),
            Opacity = 0
        };
        _scene.Children.Add(sun);

        var translate = (TranslateTransform)sun.RenderTransform;
        Begin(translate, TranslateTransform.YProperty, 170, 0, .05, duration * .62,
            new CubicEase { EasingMode = EasingMode.EaseOut });
        Begin(sun, OpacityProperty, 0, 1, .05, duration * .4);

        for (var i = 0; i < 18; i++)
        {
            var dot = Dot(2 + (i % 4), Color.FromArgb(170, 255, 226, 174));
            dot.HorizontalAlignment = HorizontalAlignment.Center;
            dot.VerticalAlignment = VerticalAlignment.Bottom;
            dot.Margin = new Thickness((i - 9) * 42, 0, 0, 80 + (i % 5) * 18);
            dot.Opacity = 0;
            _scene.Children.Add(dot);

            Begin(dot, OpacityProperty, 0, .9, .15 + i * .025, .55);
            Begin(dot, UIElement.OpacityProperty, .9, 0, duration * .62, duration * .28);
        }
    }

    private void BuildElegantFade(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(5, 13, 29),
            Color.FromRgb(9, 27, 48),
            Color.FromRgb(10, 16, 28));

        AddFlowBand(
            Color.FromArgb(130, 52, 153, 255),
            -18,
            -180,
            .08,
            duration * .72);

        AddFlowBand(
            Color.FromArgb(115, 255, 184, 95),
            15,
            160,
            .16,
            duration * .74);

        AddFlowBand(
            Color.FromArgb(65, 109, 214, 255),
            4,
            -60,
            .24,
            duration * .65);
    }

    private void BuildParticleBloom(double duration)
    {
        _scene.Background = RadialBackground(
            Color.FromRgb(37, 18, 33),
            Color.FromRgb(7, 11, 20));

        const int count = 42;

        for (var i = 0; i < count; i++)
        {
            var angle = i * (Math.PI * 2 / count);
            var radius = 135 + (i % 7) * 18;
            var size = 3 + (i % 5);

            var particle = Dot(
                size,
                i % 3 == 0
                    ? Color.FromArgb(220, 255, 177, 194)
                    : Color.FromArgb(190, 255, 221, 173));

            particle.HorizontalAlignment = HorizontalAlignment.Center;
            particle.VerticalAlignment = VerticalAlignment.Center;
            particle.RenderTransform = new TranslateTransform();
            particle.Opacity = 0;
            _scene.Children.Add(particle);

            var t = (TranslateTransform)particle.RenderTransform;
            var endX = Math.Cos(angle) * radius;
            var endY = Math.Sin(angle) * radius;

            Begin(t, TranslateTransform.XProperty, 0, endX, .05 + i * .009, duration * .56,
                new CubicEase { EasingMode = EasingMode.EaseOut });
            Begin(t, TranslateTransform.YProperty, 0, endY, .05 + i * .009, duration * .56,
                new CubicEase { EasingMode = EasingMode.EaseOut });
            Begin(particle, OpacityProperty, 0, .95, .05 + i * .006, .45);
            Begin(particle, OpacityProperty, .95, 0, duration * .64, duration * .25);
        }
    }

    private void BuildAuroraFlow(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(5, 14, 31),
            Color.FromRgb(7, 19, 35),
            Color.FromRgb(3, 8, 20));

        AddAuroraBand(
            Color.FromArgb(110, 36, 219, 189),
            -110,
            -15,
            duration);

        AddAuroraBand(
            Color.FromArgb(95, 48, 171, 255),
            20,
            20,
            duration);

        AddAuroraBand(
            Color.FromArgb(75, 194, 86, 255),
            130,
            -12,
            duration);
    }

    private void BuildGlassPanels(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(9, 20, 38),
            Color.FromRgb(13, 34, 56),
            Color.FromRgb(8, 17, 31));

        for (var i = 0; i < 5; i++)
        {
            var panel = new Border
            {
                Width = 150,
                Height = 260 - i * 16,
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 158, 213, 255)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromArgb(45, 102, 169, 230)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = new TranslateTransform(
                    i < 2 ? -520 - i * 80 : i > 2 ? 520 + i * 45 : 0,
                    (i - 2) * 12),
                Opacity = 0
            };

            _scene.Children.Add(panel);

            var tx = (TranslateTransform)panel.RenderTransform;
            var finalX = (i - 2) * 165;

            Begin(tx, TranslateTransform.XProperty, tx.X, finalX, i * .08, duration * .58,
                new CubicEase { EasingMode = EasingMode.EaseOut });
            Begin(panel, OpacityProperty, 0, .78, .05 + i * .06, duration * .38);
        }
    }

    private void BuildTypographyWave(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(7, 9, 14),
            Color.FromRgb(18, 16, 21),
            Color.FromRgb(8, 9, 14));

        for (var i = 0; i < 3; i++)
        {
            var wave = new Border
            {
                Width = 920,
                Height = 4 + i * 2,
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 110 + i * 18, 0, 0),
                Background = LinearHorizontal(
                    Color.FromArgb(0, 255, 187, 96),
                    Color.FromArgb(230 - i * 35, 255, 190, 93),
                    Color.FromArgb(0, 255, 187, 96)),
                RenderTransform = new TranslateTransform(-560 + i * 130, 0),
                Opacity = .15
            };

            _scene.Children.Add(wave);
            var t = (TranslateTransform)wave.RenderTransform;

            Begin(t, TranslateTransform.XProperty, t.X, 420 - i * 90, .08 + i * .08, duration * .72,
                new SineEase { EasingMode = EasingMode.EaseInOut });
            Begin(wave, OpacityProperty, .15, .85, .15, duration * .38);
            Begin(wave, OpacityProperty, .85, .12, duration * .67, duration * .25);
        }
    }

    private void BuildNatureSeasons(double duration)
    {
        _scene.Background = new SolidColorBrush(Color.FromRgb(7, 16, 23));

        var colors = new[]
        {
            (Color.FromRgb(230, 139, 178), Color.FromRgb(90, 166, 116)),
            (Color.FromRgb(59, 159, 100), Color.FromRgb(77, 180, 210)),
            (Color.FromRgb(209, 116, 45), Color.FromRgb(125, 78, 38)),
            (Color.FromRgb(104, 159, 208), Color.FromRgb(225, 239, 250))
        };

        for (var i = 0; i < 4; i++)
        {
            var season = new Border
            {
                Width = 330,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Linear(colors[i].Item1, colors[i].Item2, Color.FromRgb(13, 28, 38)),
                RenderTransform = new TranslateTransform((i - 1.5) * 460, 0),
                Opacity = 0
            };

            _scene.Children.Add(season);

            var t = (TranslateTransform)season.RenderTransform;
            Begin(t, TranslateTransform.XProperty, t.X, (i - 1.5) * 290, i * .06, duration * .55,
                new CubicEase { EasingMode = EasingMode.EaseOut });
            Begin(season, OpacityProperty, 0, .9, .05 + i * .05, duration * .35);
        }

        var sweep = new Border
        {
            Width = 90,
            Background = LinearHorizontal(
                Color.FromArgb(0, 255, 255, 255),
                Color.FromArgb(105, 255, 242, 198),
                Color.FromArgb(0, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Left,
            RenderTransform = new TranslateTransform(-120, 0),
            Opacity = .8
        };

        _scene.Children.Add(sweep);
        Begin(
            (TranslateTransform)sweep.RenderTransform,
            TranslateTransform.XProperty,
            -120,
            1600,
            duration * .2,
            duration * .58,
            new SineEase { EasingMode = EasingMode.EaseInOut });
    }

    private void BuildMinimalCircle(double duration)
    {
        _scene.Background = new SolidColorBrush(Color.FromRgb(4, 11, 22));

        for (var i = 0; i < 4; i++)
        {
            var ring = new Ellipse
            {
                Width = 170 + i * 58,
                Height = 170 + i * 58,
                Stroke = new SolidColorBrush(
                    i % 2 == 0
                        ? Color.FromArgb(150 - i * 18, 84, 189, 255)
                        : Color.FromArgb(145 - i * 15, 255, 190, 102)),
                StrokeThickness = i == 0 ? 2.5 : 1.2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new TransformGroup
                {
                    Children =
                    {
                        new ScaleTransform(.45, .45),
                        new RotateTransform(i % 2 == 0 ? -28 : 28)
                    }
                },
                Opacity = 0
            };

            _scene.Children.Add(ring);

            var group = (TransformGroup)ring.RenderTransform;
            var scale = (ScaleTransform)group.Children[0];
            var rotate = (RotateTransform)group.Children[1];

            Begin(scale, ScaleTransform.ScaleXProperty, .45, 1, i * .05, duration * .5,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = .25 });
            Begin(scale, ScaleTransform.ScaleYProperty, .45, 1, i * .05, duration * .5,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = .25 });
            Begin(rotate, RotateTransform.AngleProperty, rotate.Angle, -rotate.Angle * .35, .08, duration * .72,
                new SineEase { EasingMode = EasingMode.EaseInOut });
            Begin(ring, OpacityProperty, 0, .9 - i * .12, .05 + i * .04, duration * .3);
        }
    }

    private void BuildCityToDesktop(double duration)
    {
        _scene.Background = Linear(
            Color.FromRgb(8, 24, 47),
            Color.FromRgb(19, 52, 76),
            Color.FromRgb(34, 25, 42));

        var city = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 210,
            Opacity = .75
        };

        _scene.Children.Add(city);

        for (var i = 0; i < 18; i++)
        {
            var building = new Rectangle
            {
                Width = 42 + (i % 4) * 16,
                Height = 70 + (i % 7) * 22,
                Fill = new SolidColorBrush(Color.FromArgb(180, 8, 18, 30))
            };
            Canvas.SetLeft(building, i * 85 - 20);
            Canvas.SetBottom(building, 0);
            city.Children.Add(building);
        }

        for (var i = 0; i < 6; i++)
        {
            var slice = new Border
            {
                Width = 170,
                Background = new SolidColorBrush(Color.FromArgb(45, 130, 190, 240)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 186, 224, 255)),
                BorderThickness = new Thickness(1, 0, 1, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                RenderTransform = new TranslateTransform((i - 2.5) * 170, 0),
                Opacity = .8
            };

            _scene.Children.Add(slice);
            var t = (TranslateTransform)slice.RenderTransform;
            var sign = i < 3 ? -1 : 1;

            Begin(t, TranslateTransform.XProperty, t.X, t.X + sign * 620, duration * .32 + i * .025, duration * .46,
                new CubicEase { EasingMode = EasingMode.EaseIn });
            Begin(slice, OpacityProperty, .8, 0, duration * .38, duration * .34);
        }

        Begin(city, OpacityProperty, .75, .18, duration * .48, duration * .35);
    }

    private void AnimateText(double duration)
    {
        var titleTranslate = new TranslateTransform(0, 22);
        _title.RenderTransform = titleTranslate;

        var subtitleTranslate = new TranslateTransform(0, 14);
        _subtitle.RenderTransform = subtitleTranslate;

        Begin(_title, OpacityProperty, 0, 1, duration * .18, duration * .28,
            new QuadraticEase { EasingMode = EasingMode.EaseOut });
        Begin(titleTranslate, TranslateTransform.YProperty, 22, 0, duration * .16, duration * .33,
            new CubicEase { EasingMode = EasingMode.EaseOut });

        Begin(_subtitle, OpacityProperty, 0, .9, duration * .32, duration * .26,
            new QuadraticEase { EasingMode = EasingMode.EaseOut });
        Begin(subtitleTranslate, TranslateTransform.YProperty, 14, 0, duration * .3, duration * .3,
            new CubicEase { EasingMode = EasingMode.EaseOut });
    }

    private void AddFlowBand(
        Color color,
        double angle,
        double initialX,
        double begin,
        double duration)
    {
        var band = new Border
        {
            Width = 1050,
            Height = 46,
            CornerRadius = new CornerRadius(40),
            Background = LinearHorizontal(
                Color.FromArgb(0, color.R, color.G, color.B),
                color,
                Color.FromArgb(0, color.R, color.G, color.B)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new RotateTransform(angle),
                    new TranslateTransform(initialX, 0)
                }
            },
            Opacity = .15
        };

        _scene.Children.Add(band);

        var group = (TransformGroup)band.RenderTransform;
        var move = (TranslateTransform)group.Children[1];

        Begin(move, TranslateTransform.XProperty, initialX, -initialX * .75, begin, duration,
            new SineEase { EasingMode = EasingMode.EaseInOut });
        Begin(band, OpacityProperty, .15, .72, begin, duration * .42);
        Begin(band, OpacityProperty, .72, .08, begin + duration * .58, duration * .34);
    }

    private void AddAuroraBand(
        Color color,
        double initialX,
        double skew,
        double duration)
    {
        var band = new Border
        {
            Width = 680,
            Height = 210,
            CornerRadius = new CornerRadius(105),
            Background = Radial(
                color,
                Color.FromArgb(0, color.R, color.G, color.B)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new TransformGroup
            {
                Children =
                {
                    new SkewTransform(skew, -4),
                    new TranslateTransform(initialX, 0)
                }
            },
            Opacity = .32
        };

        _scene.Children.Add(band);

        var group = (TransformGroup)band.RenderTransform;
        var move = (TranslateTransform)group.Children[1];

        Begin(move, TranslateTransform.XProperty, initialX, -initialX * 1.25, .05, duration * .8,
            new SineEase { EasingMode = EasingMode.EaseInOut });
        Begin(band, OpacityProperty, .25, .66, .12, duration * .38);
        Begin(band, OpacityProperty, .66, .16, duration * .62, duration * .28);
    }

    private static Ellipse Dot(double size, Color color) =>
        new()
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color)
        };

    private static Brush Linear(Color top, Color middle, Color bottom) =>
        new LinearGradientBrush(
            new GradientStopCollection
            {
                new(top, 0),
                new(middle, .55),
                new(bottom, 1)
            },
            new Point(.5, 0),
            new Point(.5, 1));

    private static Brush LinearHorizontal(Color left, Color middle, Color right) =>
        new LinearGradientBrush(
            new GradientStopCollection
            {
                new(left, 0),
                new(middle, .5),
                new(right, 1)
            },
            new Point(0, .5),
            new Point(1, .5));

    private static Brush Radial(Color center, Color edge) =>
        new RadialGradientBrush(
            new GradientStopCollection
            {
                new(center, 0),
                new(edge, 1)
            });

    private static Brush RadialBackground(Color center, Color edge) =>
        new RadialGradientBrush(
            new GradientStopCollection
            {
                new(center, 0),
                new(edge, 1)
            })
        {
            RadiusX = .82,
            RadiusY = .82
        };

    private static void Begin(
        Animatable target,
        DependencyProperty property,
        double from,
        double to,
        double beginSeconds,
        double durationSeconds,
        IEasingFunction? easing = null)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            BeginTime = TimeSpan.FromSeconds(Math.Max(0, beginSeconds)),
            Duration = TimeSpan.FromSeconds(Math.Max(.05, durationSeconds)),
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };

        target.BeginAnimation(
            property,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }
}
