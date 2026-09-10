using System.Numerics;
using System.Windows;

namespace AsteroidsGC.Game;

public sealed class Dustfield
{
    private const float TitleDriftSpeed = 15f;

    private readonly record struct DustLayer(int Count, float Depth, double Radius, byte Alpha);

    private static readonly DustLayer[] Layers =
    [
        new DustLayer(80, 0.04f, 1.0, 90),
        new DustLayer(50, 0.12f, 1.4, 150),
        new DustLayer(25, 0.28f, 2.0, 220),
    ];

    private readonly Vector2[][] _unitPositionsByLayer;
    private readonly Vector2 _titleDriftVelocity;
    private Vector2 _titleDriftOffset;

    public Dustfield()
    {
        var rng = new Random();
        _unitPositionsByLayer = new Vector2[Layers.Length][];

        for (int l = 0; l < Layers.Length; l++)
        {
            var positions = new Vector2[Layers[l].Count];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            }
            _unitPositionsByLayer[l] = positions;
        }

        float angle = (float)(rng.NextDouble() * MathF.Tau);
        _titleDriftVelocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * TitleDriftSpeed;
    }

    public Vector2 TitleDriftOffset => _titleDriftOffset;

    public void Update(float dt)
    {
        _titleDriftOffset += _titleDriftVelocity * dt;
    }

    public IEnumerable<DustPoint> GetDust(Vector2 referenceOffset, Rect bounds)
    {
        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        for (int l = 0; l < Layers.Length; l++)
        {
            var layer = Layers[l];
            var positions = _unitPositionsByLayer[l];

            foreach (var unit in positions)
            {
                float x = Wrap(unit.X * width - referenceOffset.X * layer.Depth, width);
                float y = Wrap(unit.Y * height - referenceOffset.Y * layer.Depth, height);

                yield return new DustPoint(new Point(x, y), layer.Radius, layer.Alpha);
            }
        }
    }

    private static float Wrap(float value, float max)
    {
        if (max <= 0f)
        {
            return 0f;
        }

        float result = value % max;
        return result < 0f ? result + max : result;
    }
}
