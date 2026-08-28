using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AsteroidsWpf.Engine;
using AsteroidsWpf.Game;

namespace AsteroidsWpf;

public sealed class GameCanvas : FrameworkElement
{
    public GameManager? GameManager { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(Brushes.Black, null, bounds);

        var gm = GameManager;
        if (gm is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (gm.State is GameState.Playing or GameState.Paused or GameState.GameOver)
        {
            DrawEntities(dc, gm);
        }

        DrawScanlinesAndVignette(dc, bounds);

        switch (gm.State)
        {
            case GameState.TitleScreen:
                DrawTitleScreen(dc, bounds);
                break;

            case GameState.Paused:
                DrawHud(dc, gm, bounds);
                DrawCenteredText(dc, bounds, "PAUSED", 42, 0);
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
            GlowRenderer.DrawGlowPolyline(dc, asteroid.GetWorldShape(), true, AsteroidColor);
        }

        foreach (var bullet in gm.Bullets)
        {
            var color = bullet.Owner == BulletOwner.Player ? BulletColor : SaucerColor;
            GlowRenderer.DrawGlowPolyline(dc, bullet.GetWorldShape(), false, color);
        }

        if (gm.Saucer is { IsAlive: true } saucer)
        {
            GlowRenderer.DrawGlowPolyline(dc, saucer.GetWorldShape(), true, SaucerColor);
        }

        foreach (var particle in gm.Particles)
        {
            var color = Color.FromArgb(
                (byte)(particle.LifeFraction * 255), 
                ParticleColor.R, 
                ParticleColor.G, 
                ParticleColor.B);
            
            var tail = 
                particle.Position - System.Numerics.Vector2.Normalize(
                    particle.Velocity == System.Numerics.Vector2.Zero 
                        ? new System.Numerics.Vector2(1, 0) 
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
                GlowRenderer.DrawGlowPolyline(dc, ship.GetWorldShape(), true, ShipColor);
                if (ship.IsThrusting)
                {
                    GlowRenderer.DrawGlowPolyline(
                        dc,
                        RotateAndTranslate(
                            ship.GetThrustFlameShape(gm.LastDeltaTime),
                            ship),
                        false,
                        Colors.OrangeRed);
                }
            }
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

    private static void DrawHud(DrawingContext dc, GameManager gm, Rect bounds)
    {
        DrawText(dc, $"SCORE {gm.Score:D5}", 18, new Point(20, 16), TextColor);
        DrawText(dc, $"LEVEL {gm.Level}", 16, new Point(20, 44), TextColor);

        var lifeShip = new Point[]
        {
            new(10, 0), new(-6, 5.5), new(-3, 0), new(-6, -5.5),
        };

        for (int i = 0; i < gm.Lives; i++)
        {
            var offset = new Point(bounds.Width - 30 - i * 26, 26);
            GlowRenderer.DrawGlowPolyline(dc, RotateLifeIcon(lifeShip, offset), true, ShipColor);
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

    private static void DrawTitleScreen(DrawingContext dc, Rect bounds)
    {
        DrawCenteredText(dc, bounds, "ASTEROIDS", 64, -60);
        DrawCenteredText(dc, bounds, "PRESS ENTER TO START", 20, 10);
        DrawCenteredText(dc, bounds, "ARROWS / WASD: ROTATE + THRUST   SPACE: FIRE   SHIFT: HYPERSPACE   ESC: PAUSE", 14, 45);

        var versionText = CreateFormattedText(Version, 13, TextColor);
        var versionOrigin = new Point(bounds.Width - versionText.Width - 14, bounds.Height - versionText.Height - 12);
        dc.DrawText(versionText, versionOrigin);
    }

    private static void DrawCenteredText(
        DrawingContext dc, 
        Rect bounds, 
        string text, 
        double size, 
        double verticalOffset)
    {
        var formatted = CreateFormattedText(text, size, TextColor);
        
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

    private static readonly Color ShipColor = Colors.White;
    private static readonly Color AsteroidColor = Color.FromRgb( 210, 255, 220 );
    private static readonly Color BulletColor = Colors.White;
    private static readonly Color SaucerColor = Color.FromRgb( 160, 255, 170 );
    private static readonly Color ParticleColor = Color.FromRgb( 255, 225, 160 );
    private static readonly Color TextColor = Colors.White;
    private static readonly Typeface HudTypeface = new(
        new FontFamily( "Consolas" ),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal );

    public const string Version = "v0.1.2";
}
