using System.Numerics;
using System.Windows;
using AsteroidsWpf.Engine;

namespace AsteroidsWpf.Game;

public enum SaucerSize
{
    Big,
    Small,
}

public sealed class Saucer : GameObject
{
    private const float DirectionChangeIntervalSeconds = 1.4f;
    private const float BigFireIntervalSeconds = 1.7f;
    private const float SmallFireIntervalSeconds = 1.2f;
    private const float SmallAimJitterRadians = 0.35f;

    public readonly SaucerSize Size;
    private readonly Random _rng;
    private float _directionChangeTimer;
    private float _fireTimer;

    public Saucer(SaucerSize size, Vector2 position, float horizontalDirection, Random rng)
    {
        Size = size;
        _rng = rng;
        Radius = size == SaucerSize.Big ? 18f : 10f;
        Position = position;

        float speed = size == SaucerSize.Big ? 60f : 100f;
        Velocity = new Vector2(horizontalDirection * speed, 0f);

        _directionChangeTimer = DirectionChangeIntervalSeconds;
        _fireTimer = FireInterval;
    }

    private float FireInterval => 
        Size == SaucerSize.Big 
            ? BigFireIntervalSeconds 
            : SmallFireIntervalSeconds;

    public int PointValue => 
        Size == SaucerSize.Big ? 200 : 1000;

    public override void Update(float dt, Rect bounds)
    {
        base.Update(dt, bounds);

        _directionChangeTimer -= dt;
        if (_directionChangeTimer <= 0f)
        {
            _directionChangeTimer = DirectionChangeIntervalSeconds;
            float verticalSpeed = ((float)_rng.NextDouble() * 2f - 1f) * 60f;
            Velocity = new Vector2(Velocity.X, verticalSpeed);
        }

        if (_fireTimer > 0f)
        {
            _fireTimer -= dt;
        }
    }

    public bool TryFire(Vector2 playerPosition, out Bullet? bullet)
    {
        bullet = null;
        if (_fireTimer > 0f)
        {
            return false;
        }

        _fireTimer = FireInterval;

        float angle;
        if (Size == SaucerSize.Big)
        {
            angle = (float)(_rng.NextDouble() * MathF.Tau);
        }
        else
        {
            var toPlayer = playerPosition - Position;
            angle = MathF.Atan2(toPlayer.Y, toPlayer.X) + ((float)_rng.NextDouble() - 0.5f) * SmallAimJitterRadians;
        }

        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        bullet = new Bullet(BulletOwner.Saucer)
        {
            Position = Position + direction * Radius,
            Velocity = direction * Bullet.Speed * 0.75f,
            Rotation = angle,
        };
        return true;
    }

    public override Point[] GetLocalShape()
    {
        float w = Radius;
        float wTop = Radius * 0.5f;
        float h = Radius * 0.4f;
        return
        [
            new Point(-w, 0),
            new Point(-wTop, -h),
            new Point(wTop, -h),
            new Point(w, 0),
            new Point(wTop, h),
            new Point(-wTop, h),
        ];
    }
}
