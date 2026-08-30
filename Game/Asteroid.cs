using System.Numerics;
using System.Windows;
using AsteroidsGC.Engine;

namespace AsteroidsGC.Game;

public sealed class Asteroid : GameObject
{
    private Asteroid(AsteroidSize size, float baseRadius, Random rng)
    {
        Size = size;
        Radius = baseRadius;

        int vertexCount = rng.Next(MinVertices, MaxVertices + 1);
        _shape = new Point[vertexCount];
        float angleStep = MathF.Tau / vertexCount;
        for (int i = 0; i < vertexCount; i++)
        {
            float angle = i * angleStep + (float)(rng.NextDouble() - 0.5) * angleStep * 0.5f;
            float scale = MinRadiusScale + (float)rng.NextDouble() * (MaxRadiusScale - MinRadiusScale);
            float r = baseRadius * scale;
            _shape[i] = new Point(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
        }

        RotationSpeed = ((float)rng.NextDouble() * 2f - 1f) * 1.2f;
    }

    public static float BaseRadiusFor(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => 45f,
        AsteroidSize.Medium => 25f,
        AsteroidSize.Small => 13f,
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };

    private static (float min, float max) SpeedRangeFor(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => (20f, 50f),
        AsteroidSize.Medium => (40f, 80f),
        AsteroidSize.Small => (60f, 110f),
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };

    public static Asteroid CreateRandom(AsteroidSize size, Vector2 position, Random rng)
    {
        var asteroid = new Asteroid(size, BaseRadiusFor(size), rng)
        {
            Position = position,
        };

        var (min, max) = SpeedRangeFor(size);
        float speed = min + (float)rng.NextDouble() * (max - min);
        float direction = (float)rng.NextDouble() * MathF.Tau;
        asteroid.Velocity = new Vector2(MathF.Cos(direction), MathF.Sin(direction)) * speed;

        return asteroid;
    }

    public IEnumerable<Asteroid> Split(Random rng)
    {
        AsteroidSize? childSize = Size switch
        {
            AsteroidSize.Large => AsteroidSize.Medium,
            AsteroidSize.Medium => AsteroidSize.Small,
            AsteroidSize.Small => null,
            _ => null,
        };

        if (childSize is null)
        {
            yield break;
        }

        for (int i = 0; i < 2; i++)
        {
            var child = CreateRandom(childSize.Value, Position, rng);
            child.Velocity += Velocity * 0.5f;
            yield return child;
        }
    }

    public override Point[] GetLocalShape() => _shape;

    public readonly AsteroidSize Size;

    private readonly Point[] _shape;
    private const int MinVertices = 8;
    private const int MaxVertices = 14;
    private const float MinRadiusScale = 0.7f;
    private const float MaxRadiusScale = 1.3f;
}
