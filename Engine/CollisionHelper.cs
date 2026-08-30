using System.Numerics;
using System.Windows;

namespace AsteroidsGC.Engine;

public static class CollisionHelper
{
    public static bool CirclesIntersect(
        Vector2 posA, 
        float radiusA, 
        Vector2 posB, 
        float radiusB)
    {
        float r = radiusA + radiusB;
        return Vector2.DistanceSquared(posA, posB) <= r * r;
    }

    /// <summary>
    /// Elastically bounces two bodies apart (mass ~ Radius^2) and separates any overlap. 
    /// Returns the pre-impulse closing speed.
    /// </summary>
    public static float ResolveElasticCollision(GameObject a, GameObject b)
    {
        Vector2 delta = b.Position - a.Position;
        float distance = delta.Length();

        Vector2 normal;
        if (distance < 0.0001f)
        {
            normal = new Vector2(1f, 0f);
            distance = 0f;
        }
        else
        {
            normal = delta / distance;
        }

        float overlap = (a.Radius + b.Radius) - distance;
        if (overlap > 0f)
        {
            Vector2 correction = normal * (overlap / 2f);
            a.Position -= correction;
            b.Position += correction;
        }

        Vector2 relativeVelocity = b.Velocity - a.Velocity;
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
        if (velocityAlongNormal > 0f)
        {
            return 0f;
        }

        float massA = a.Radius * a.Radius;
        float massB = b.Radius * b.Radius;

        float impulseScalar = -2f * velocityAlongNormal / (1f / massA + 1f / massB);
        Vector2 impulse = impulseScalar * normal;

        a.Velocity -= impulse / massA;
        b.Velocity += impulse / massB;

        return -velocityAlongNormal;
    }

    public static void ScreenWrap(ref Vector2 position, Rect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        if (position.X < 0)
        {
            position.X += width;
        }
        else if (position.X >= width)
        {
            position.X -= width;
        }

        if (position.Y < 0)
        {
            position.Y += height;
        }
        else if (position.Y >= height)
        {
            position.Y -= height;
        }
    }
}
