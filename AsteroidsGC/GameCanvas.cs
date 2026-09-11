using AsteroidsGC.Engine;
using AsteroidsGC.Game;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace AsteroidsGC;

public sealed class GameCanvas : FrameworkElement
{
    public GameManager? GameManager { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(Brushes.Black, null, bounds);

        var gm = GameManager;
        if (gm is null
            || bounds.Width <= 0
            || bounds.Height <= 0)
        {
            return;
        }

        if (gm.State is GameState.Playing
            or GameState.Paused
            or GameState.GameOver)
        {
            DrawDustfield(dc, bounds, gm.Dustfield, gm.Ship.Position);
            DrawEntities(dc, gm);
        }

        DrawScanlinesAndVignette(dc, bounds);

        switch (gm.State)
        {
            case GameState.TitleScreen:
                DrawTitleScreen(dc, bounds, gm);
                DrawSoundToggle(dc, bounds, gm.SoundEnabled);
                break;

            case GameState.Paused:
                DrawHud(dc, gm, bounds);
                DrawCenteredText(dc, bounds, "PAUSED", 42, 0);
                DrawCenteredText(dc, bounds, "ESC / ENTER: RESUME • Q: QUIT TO TITLE", 16, 40);
                break;

            case GameState.GameOver:
                DrawHud(dc, gm, bounds);
                DrawCenteredText(dc, bounds, "GAME OVER", 48, -20);
                DrawCenteredText(dc, bounds, "PRESS ENTER", 20, 30);
                break;

            case GameState.Playing:
                DrawHud(dc, gm, bounds);
                break;
        }
    }

    private static void DrawEntities(DrawingContext dc, GameManager gm)
    {
        foreach (var asteroid in gm.Asteroids)
        {
            GlowRenderer.DrawGlowPolyline(
                dc,
                asteroid.GetWorldShape(),
                true,
                ColorScheme.Current.Asteroid);
        }

        foreach (var bullet in gm.Bullets)
        {
            var color =
                bullet.Owner == BulletOwner.Player
                    ? ColorScheme.Current.Bullet
                    : ColorScheme.Current.Saucer;

            GlowRenderer.DrawGlowPolyline(dc, bullet.GetWorldShape(), false, color);
        }

        if (gm.Saucer is { IsAlive: true } saucer)
        {
            GlowRenderer.DrawGlowPolyline(
                dc,
                saucer.GetWorldShape(),
                true,
                ColorScheme.Current.Saucer);
        }

        foreach (var particle in gm.Particles)
        {
            var particleColor = ColorScheme.Current.Particle;
            var color = Color.FromArgb(
                (byte)(particle.LifeFraction * 255),
                particleColor.R,
                particleColor.G,
                particleColor.B);

            var tail =
                particle.Position - Vector2.Normalize(
                    particle.Velocity == Vector2.Zero
                        ? new Vector2(1, 0)
                        : particle.Velocity) * 4f;

            GlowRenderer.DrawGlowLine(
                dc,
                new Point(particle.Position.X, particle.Position.Y),
                new Point(tail.X, tail.Y), color);
        }

        var ship = gm.Ship;
        if (ship.IsAlive)
        {
            bool visible =
                !ship.IsInvulnerable
                || ((int)(ship.InvulnerabilityRemaining * 12) % 2 == 0);

            if (visible)
            {
                GlowRenderer.DrawGlowPolyline(
                    dc,
                    ship.GetWorldShape(),
                    true,
                    ColorScheme.Current.Ship);

                if (ship.IsThrusting)
                {
                    GlowRenderer.DrawGlowPolyline(
                        dc,
                        RotateAndTranslate(
                            ship.GetThrustFlameShape(gm.LastDeltaTime),
                            ship),
                        false,
                        ColorScheme.Current.ThrustFlame);
                }
            }
        }
    }

    private static void DrawDustfield(
        DrawingContext dc,
        Rect bounds,
        Dustfield dustfield,
        Vector2 referenceOffset)
    {
        var baseColor = ColorScheme.Current.Text;
        var brushCache = new Dictionary<byte, SolidColorBrush>();

        foreach (var dust in dustfield.GetDust(referenceOffset, bounds))
        {
            if (!brushCache.TryGetValue(dust.Alpha, out var brush))
            {
                brush = new SolidColorBrush(
                    Color.FromArgb(dust.Alpha, baseColor.R, baseColor.G, baseColor.B));
                brush.Freeze();
                brushCache[dust.Alpha] = brush;
            }

            dc.DrawEllipse(brush, null, dust.Position, dust.Radius, dust.Radius);
        }
    }

    private static Point[] RotateAndTranslate(Point[] local, Ship ship)
    {
        var world = new Point[local.Length];
        float cos = MathF.Cos(ship.Rotation);
        float sin = MathF.Sin(ship.Rotation);
        for (int i = 0; i < local.Length; i++)
        {
            float lx = (float)local[i].X;
            float ly = (float)local[i].Y;
            float wx = lx * cos - ly * sin + ship.Position.X;
            float wy = lx * sin + ly * cos + ship.Position.Y;
            world[i] = new Point(wx, wy);
        }
        return world;
    }

    private static void DrawScanlinesAndVignette(DrawingContext dc, Rect bounds)
    {
        var scanBrush = new SolidColorBrush(Color.FromArgb(28, 0, 0, 0));
        for (double y = 0; y < bounds.Height; y += 3)
        {
            dc.DrawRectangle(scanBrush, null, new Rect(0, y, bounds.Width, 1));
        }

        var vignette = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.75,
            RadiusY = 0.75,
        };
        vignette.GradientStops.Add(
            new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.6));
        vignette.GradientStops.Add(
            new GradientStop(Color.FromArgb(140, 0, 0, 0), 1.0));

        vignette.Freeze();
        dc.DrawRectangle(vignette, null, bounds);
    }

    private static void DrawSoundToggle(DrawingContext dc, Rect bounds, bool soundEnabled)
    {
        var text = soundEnabled ? "SOUND ON [M]" : "SOUND OFF [M]";
        var baseColor = ColorScheme.Current.Text;
        var color = soundEnabled
            ? baseColor
            : Color.FromArgb(110, baseColor.R, baseColor.G, baseColor.B);

        var formatted = CreateFormattedText(text, 14, color);
        var origin = new Point(bounds.Width - formatted.Width - 20, 16);

        dc.DrawText(formatted, origin);
    }

    private static void DrawHud(DrawingContext dc, GameManager gm, Rect bounds)
    {
        DrawText(dc, $"SCORE {gm.Score:D5}", 18, new Point(20, 16), ColorScheme.Current.Text);
        DrawText(dc, $"LEVEL {gm.Level}", 16, new Point(20, 44), ColorScheme.Current.Text);

        var lifeShip = new Point[]
        {
            new(10, 0), new(-6, 5.5), new(-3, 0), new(-6, -5.5),
        };

        for (int i = 0; i < gm.Lives; i++)
        {
            var offset = new Point(bounds.Width - 30 - i * 26, 26);
            GlowRenderer.DrawGlowPolyline(
                dc,
                RotateLifeIcon(lifeShip, offset),
                true,
                ColorScheme.Current.Ship);
        }
    }

    private static Point[] RotateLifeIcon(Point[] local, Point offset)
    {
        double angle = -Math.PI / 2;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        var result = new Point[local.Length];

        for (int i = 0; i < local.Length; i++)
        {
            double x = local[i].X * cos - local[i].Y * sin;
            double y = local[i].X * sin + local[i].Y * cos;
            result[i] = new Point(x + offset.X, y + offset.Y);
        }

        return result;
    }

    private static void DrawTitleScreen(DrawingContext dc, Rect bounds, GameManager gm)
    {
        DrawDustfield(dc, bounds, gm.Dustfield, gm.Dustfield.TitleDriftOffset);
        DrawTitleLogo(dc, bounds, gm.TitleLogo);
        DrawColorSchemeMenu(dc, gm);

        DrawCenteredText(dc, bounds, "ASTEROIDS", 64, -60);
        DrawCenteredText(dc, bounds, "PRESS ENTER TO START", 20, 10);
        DrawCenteredText(dc, bounds, "ARROWS / WASD: ROTATE + THRUST • SPACE: FIRE • SHIFT: HYPERSPACE • ESC: PAUSE", 14, 45);

        var versionText = CreateFormattedText(Version, 13, ColorScheme.Current.Text);

        var versionOrigin = new Point(
            bounds.Width - versionText.Width - 14,
            bounds.Height - versionText.Height - 12);

        dc.DrawText(versionText, versionOrigin);
    }

    private static void DrawColorSchemeMenu(DrawingContext dc, GameManager gm)
    {
        DrawText(dc, "COLOR SCHEME", 14, new Point(20, 16), ColorScheme.Current.Text);

        var presets = gm.ColorSchemePresets;
        for (int i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            bool isActive = preset.FilePath == ColorScheme.Current.SourcePath;
            var color =
                isActive
                    ? ColorScheme.Current.TitleLogoPrimary
                    : ColorScheme.Current.Text;

            DrawText(dc, $"{i + 1}. {preset.Name}", 14, new Point(20, 40 + i * 20), color);
        }
    }

    private static void DrawTitleLogo(DrawingContext dc, Rect bounds, TitleLogo logo)
    {
        float scale = (float)Math.Min(bounds.Width, bounds.Height) * 0.22f;
        var center = new Point(bounds.Width / 2.0, bounds.Height / 2.0);
        double letterSpacing = scale * 1.05;
        var gOrigin = new Point(center.X - letterSpacing, center.Y);
        var cOrigin = new Point(center.X + letterSpacing, center.Y);

        dc.PushOpacity(0.5);

        foreach (var part in logo.GetLetterG(gOrigin, scale))
        {
            GlowRenderer.DrawGlowPolyline(dc, part, true, ColorScheme.Current.TitleLogoPrimary);
        }

        foreach (var part in logo.GetLetterC(cOrigin, scale))
        {
            GlowRenderer.DrawGlowPolyline(dc, part, true, ColorScheme.Current.TitleLogoSecondary);
        }

        dc.Pop();
    }

    private static void DrawCenteredText(
        DrawingContext dc,
        Rect bounds,
        string text,
        double size,
        double verticalOffset)
    {
        var formatted = CreateFormattedText(text, size, ColorScheme.Current.Text);

        var origin = new Point(
            (bounds.Width - formatted.Width) / 2.0,
            (bounds.Height - formatted.Height) / 2.0 + verticalOffset);

        dc.DrawText(formatted, origin);
    }

    private static void DrawText(
        DrawingContext dc,
        string text,
        double size,
        Point origin,
        Color color)
    {
        dc.DrawText(CreateFormattedText(text, size, color), origin);
    }

    private static FormattedText CreateFormattedText(
        string text,
        double size,
        Color color)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            HudTypeface,
            size,
            new SolidColorBrush(color),
            1.0);
    }

    private static readonly Typeface HudTypeface = new(
        new FontFamily("Consolas"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    public const string Version = "v0.1.4";
}
