using System.Windows;

namespace AsteroidsGC.Game;

public sealed class TitleLogo
{
    public TitleLogo()
    {
        const float cGapHalfAngle = 45f;
        const float gGapHalfAngle = 16f;

        _cParts = [CreatePart(BuildRing(OuterRadius, InnerRadius, cGapHalfAngle, Segments))];
        _gParts =
        [
            CreatePart(BuildRing(OuterRadius, InnerRadius, gGapHalfAngle, Segments)),
            CreatePart(BuildGBar(InnerRadius, gGapHalfAngle)),
        ];
    }

    public void Update(float dt)
    {
        _time += dt;
    }

    public IReadOnlyList<Point[]> GetLetterG(Point origin, float scale) =>
        BuildWorldParts(_gParts, origin, scale);

    public IReadOnlyList<Point[]> GetLetterC(Point origin, float scale) => 
        BuildWorldParts(_cParts, origin, scale);

    private List<Point[]> BuildWorldParts(LogoPart[] parts, Point origin, float scale)
    {
        var result = new List<Point[]>(parts.Length);
        foreach (var part in parts)
        {
            var world = new Point[part.BasePoints.Length];
            for (int i = 0; i < part.BasePoints.Length; i++)
            {
                var basePoint = part.BasePoints[i];
                float offset = JitterAmount * (
                    0.6f * MathF.Sin(part.Freq1[i] * _time + part.Phase1[i]) +
                    0.4f * MathF.Sin(part.Freq2[i] * _time + part.Phase2[i]));
                float scaleFactor = 1f + offset;

                double px = basePoint.X * scaleFactor;
                double py = basePoint.Y * scaleFactor;

                world[i] = new Point(origin.X + px * scale, origin.Y + py * scale);
            }
            result.Add(world);
        }
        return result;
    }

    private LogoPart CreatePart(Point[] basePoints)
    {
        int count = basePoints.Length;
        var freq1 = new float[count];
        var phase1 = new float[count];
        var freq2 = new float[count];
        var phase2 = new float[count];

        for (int i = 0; i < count; i++)
        {
            freq1[i] = 0.3f + (float)_rng.NextDouble() * 0.6f;
            phase1[i] = (float)_rng.NextDouble() * MathF.Tau;
            freq2[i] = 0.9f + (float)_rng.NextDouble() * 0.9f;
            phase2[i] = (float)_rng.NextDouble() * MathF.Tau;
        }

        return new LogoPart(basePoints, freq1, phase1, freq2, phase2);
    }

    private static Point[] BuildRing(
        float outerR, 
        float innerR, 
        float gapHalfAngleDeg, 
        int segments)
    {
        var points = new Point[segments * 2];
        float startDeg = gapHalfAngleDeg;
        float endDeg = 360f - gapHalfAngleDeg;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            float angle = DegToRad(startDeg + (endDeg - startDeg) * t);
            points[i] = new Point(
                MathF.Cos(angle) * outerR, 
                MathF.Sin(angle) * outerR);
        }

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            float angle = DegToRad(endDeg - (endDeg - startDeg) * t);
            points[segments + i] = new Point(
                MathF.Cos(angle) * innerR, 
                MathF.Sin(angle) * innerR);
        }

        return points;
    }

    private static Point[] BuildGBar(float innerR, float gapHalfAngleDeg)
    {
        const float halfThickness = 0.09f;
        const float lengthFraction = 0.85f;

        float anchorAngle = DegToRad(gapHalfAngleDeg);
        var anchor = new Point(
            MathF.Cos(anchorAngle) * innerR, 
            MathF.Sin(anchorAngle) * innerR);

        double dirX = -anchor.X;
        double dirY = -anchor.Y;
        double len = Math.Sqrt(dirX * dirX + dirY * dirY);
        dirX /= len;
        dirY /= len;

        double perpX = -dirY * halfThickness;
        double perpY = dirX * halfThickness;

        var tip = new Point(
            anchor.X + dirX * len * lengthFraction, 
            anchor.Y + dirY * len * lengthFraction);

        return
        [
            new Point(anchor.X + perpX, anchor.Y + perpY),
            new Point(anchor.X - perpX, anchor.Y - perpY),
            new Point(tip.X - perpX, tip.Y - perpY),
            new Point(tip.X + perpX, tip.Y + perpY),
        ];
    }

    private sealed class LogoPart(
        Point[] basePoints, 
        float[] freq1, 
        float[] phase1, 
        float[] freq2, 
        float[] phase2)
    {
        public Point[] BasePoints { get; } = basePoints;
        public float[] Freq1 { get; } = freq1;
        public float[] Phase1 { get; } = phase1;
        public float[] Freq2 { get; } = freq2;
        public float[] Phase2 { get; } = phase2;
    }

    private readonly Random _rng = new();
    private readonly LogoPart[] _gParts;
    private readonly LogoPart[] _cParts;
    private float _time;

    private static float DegToRad(float deg) => deg * MathF.PI / 180f;

    private const float JitterAmount = 0.16f;
    private const float OuterRadius = 1f;
    private const float InnerRadius = 0.55f;
    private const int Segments = 20;
}
