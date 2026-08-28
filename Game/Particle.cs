using System.Numerics;

namespace AsteroidsWpf.Game;

public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float LifeRemaining;
    public float MaxLife;

    public readonly float LifeFraction => 
        MaxLife <= 0f 
            ? 0f 
            : Math.Clamp(LifeRemaining / MaxLife, 0f, 1f);
}
