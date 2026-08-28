using System.Numerics;
using System.Windows;
using AsteroidsWpf.Engine;

namespace AsteroidsWpf.Game;

public sealed class Ship : GameObject
{
    private const float RotationRateRadPerSec = 4.2f;
    private const float ThrustAcceleration = 280f;
    private const float Drag = 0.35f; // fraction of velocity retained after 1 full second
    private const float FireCooldownSeconds = 0.22f;
    private const float RespawnInvulnerabilitySeconds = 2.5f;
    private const float HyperspaceInvulnerabilitySeconds = 1.0f;

    public float FireCooldownRemaining;
    public float InvulnerabilityRemaining;
    public bool IsThrusting;

    public Ship()
    {
        Radius = 12f;
        Rotation = -MathF.PI / 2f;
    }

    public bool IsInvulnerable => InvulnerabilityRemaining > 0f;

    public void ResetForRespawn(Vector2 center)
    {
        IsAlive = true;
        Position = center;
        Velocity = Vector2.Zero;
        Rotation = -MathF.PI / 2f;
        RotationSpeed = 0f;
        FireCooldownRemaining = 0f;
        InvulnerabilityRemaining = RespawnInvulnerabilitySeconds;
        IsThrusting = false;
    }

    public void RotateLeft(float dt) => Rotation -= RotationRateRadPerSec * dt;

    public void RotateRight(float dt) => Rotation += RotationRateRadPerSec * dt;

    public void ApplyThrust(float dt)
    {
        var forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        Velocity += forward * ThrustAcceleration * dt;
        IsThrusting = true;
    }

    public bool TryFire(out Bullet? bullet)
    {
        bullet = null;
        if (FireCooldownRemaining > 0f)
        {
            return false;
        }

        FireCooldownRemaining = FireCooldownSeconds;
        var forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        bullet = new Bullet(BulletOwner.Player)
        {
            Position = Position + forward * Radius,
            Velocity = Velocity + forward * Bullet.Speed,
            Rotation = Rotation,
        };
        return true;
    }

    public void Hyperspace(Random rng, Rect bounds)
    {
        Position = new Vector2(
            (float)(rng.NextDouble() * bounds.Width),
            (float)(rng.NextDouble() * bounds.Height));
        Velocity = Vector2.Zero;
        InvulnerabilityRemaining = MathF.Max(InvulnerabilityRemaining, HyperspaceInvulnerabilitySeconds);
    }

    public override void Update(float dt, Rect bounds)
    {
        base.Update(dt, bounds);

        Velocity *= MathF.Pow(Drag, dt);

        if (FireCooldownRemaining > 0f)
        {
            FireCooldownRemaining = MathF.Max(0f, FireCooldownRemaining - dt);
        }

        if (InvulnerabilityRemaining > 0f)
        {
            InvulnerabilityRemaining = MathF.Max(0f, InvulnerabilityRemaining - dt);
        }
    }

    public override Point[] GetLocalShape()
    {
        float r = Radius;
        return
        [
            new Point(r, 0),
            new Point(-r * 0.6, r * 0.55),
            new Point(-r * 0.3, 0),
            new Point(-r * 0.6, -r * 0.55),
        ];
    }

    public Point[] GetThrustFlameShape()
    {
        float r = Radius;
        return
        [
            new Point(-r * 0.75, r * 0.3),
            new Point(-r * 1.75, 0),
            new Point(-r * 0.75, -r * 0.3),
        ];
    }
}
