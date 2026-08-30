using System.Numerics;
using System.Windows;

namespace AsteroidsGC.Engine;

public abstract class GameObject
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public float RotationSpeed;
    public float Radius;
    public bool IsAlive = true;

    public abstract Point[] GetLocalShape();

    public virtual void Update(float dt, Rect bounds)
    {
        Position += Velocity * dt;
        Rotation += RotationSpeed * dt;
        CollisionHelper.ScreenWrap(ref Position, bounds);
    }

    public Point[] GetWorldShape()
    {
        var local = GetLocalShape();
        var world = new Point[local.Length];
        float cos = MathF.Cos(Rotation);
        float sin = MathF.Sin(Rotation);
        for (int i = 0; i < local.Length; i++)
        {
            float lx = (float)local[i].X;
            float ly = (float)local[i].Y;
            float wx = lx * cos - ly * sin + Position.X;
            float wy = lx * sin + ly * cos + Position.Y;
            world[i] = new Point(wx, wy);
        }
        return world;
    }
}
