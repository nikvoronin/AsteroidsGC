using System.Windows;
using System.Windows.Media;

namespace AsteroidsGC.Engine;

public static class GlowRenderer
{
    private record struct GlowPass(double Thickness, byte Alpha);

    private static readonly GlowPass[] Passes =
    [
        new GlowPass(14.0, 35),
        new GlowPass(9.0, 60),
        new GlowPass(5.0, 110),
        new GlowPass(2.5, 180),
        new GlowPass(1.25, 255),
    ];

    public static void DrawGlowPolyline(DrawingContext dc, Point[] points, bool closed, Color color)
    {
        if (points.Length < 2)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], false, closed);
            ctx.PolyLineTo(points[1..], true, true);
        }
        geometry.Freeze();

        foreach (var pass in Passes)
        {
            var brush = new SolidColorBrush(Color.FromArgb(pass.Alpha, color.R, color.G, color.B));
            brush.Freeze();
            var pen = new Pen(brush, pass.Thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };
            pen.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }

    public static void DrawGlowLine(DrawingContext dc, Point a, Point b, Color color)
    {
        DrawGlowPolyline(dc, [a, b], false, color);
    }

    public static void DrawGlowEllipse(DrawingContext dc, Point center, double radiusX, double radiusY, Color color)
    {
        foreach (var pass in Passes)
        {
            var brush = new SolidColorBrush(Color.FromArgb(pass.Alpha, color.R, color.G, color.B));
            brush.Freeze();
            var pen = new Pen(brush, pass.Thickness);
            pen.Freeze();
            dc.DrawEllipse(null, pen, center, radiusX, radiusY);
        }
    }
}
