using System.Windows;
using AsteroidsGC.Engine;

namespace AsteroidsGC.Game;

public sealed class Bullet : GameObject
{
    public const float Speed = 480f;
    private const float LifetimeSeconds = 1.1f;

    public readonly BulletOwner Owner;
    private float _timeToLive = LifetimeSeconds;

    public Bullet(BulletOwner owner)
    {
        Owner = owner;
        Radius = 2f;
    }

    public override void Update(float dt, Rect bounds)
    {
        base.Update(dt, bounds);

        _timeToLive -= dt;
        if (_timeToLive <= 0f)
        {
            IsAlive = false;
        }
    }

    public override Point[] GetLocalShape() =>
    [
        new Point(-Radius, 0),
        new Point(Radius, 0),
    ];
}
