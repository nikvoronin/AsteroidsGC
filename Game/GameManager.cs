using System.Numerics;
using System.Windows;
using System.Windows.Input;
using AsteroidsGC.Engine;
using InputManager = AsteroidsWpf.Engine.InputManager;

namespace AsteroidsGC.Game;

public sealed class GameManager
{
    private const int StartingLives = 3;
    private const float MinSaucerSpawnSeconds = 14f;
    private const float MaxSaucerSpawnSeconds = 24f;
    private const float ShipSafeSpawnRadius = 160f;
    private const int ExtraLifeScoreStep = 10000;
    private const float AsteroidFragmentationSpeed = 100f;
    private const float ExtraAsteroidIntervalSeconds = 60f;

    private readonly Random _rng = new();
    private readonly SoundManager _sound;
    private readonly List<Asteroid> _asteroids = [];
    private readonly List<Bullet> _bullets = [];
    private readonly List<Particle> _particles = [];

    private Rect _bounds;
    private Saucer? _saucer;
    private float _saucerSpawnTimer;
    private float _extraAsteroidTimer;
    private int _nextExtraLifeScore = ExtraLifeScoreStep;

    public GameManager(SoundManager sound)
    {
        _sound = sound;
        Ship = new Ship();
    }

    public GameState State { get; private set; } = GameState.TitleScreen;

    public Ship Ship { get; }

    public int Score { get; private set; }

    public int Lives { get; private set; }

    public int Level { get; private set; }

    public IReadOnlyList<Asteroid> Asteroids => _asteroids;

    public IReadOnlyList<Bullet> Bullets => _bullets;

    public IReadOnlyList<Particle> Particles => _particles;

    public Saucer? Saucer => _saucer;

    public float LastDeltaTime { get; private set; }

    public void Update(float dt, InputManager input, Rect bounds)
    {
        LastDeltaTime = dt;
        _bounds = bounds;

        switch (State)
        {
            case GameState.TitleScreen:
                if (input.WasPressed(Key.Enter) || input.WasPressed(Key.Space))
                {
                    StartNewGame();
                }
                break;

            case GameState.Playing:
                UpdatePlaying(dt, input);
                if (input.WasPressed(Key.Escape))
                {
                    State = GameState.Paused;
                    _sound.StopThrust();
                }
                break;

            case GameState.Paused:
                if (input.WasPressed(Key.Escape) || input.WasPressed(Key.Enter))
                {
                    State = GameState.Playing;
                }
                break;

            case GameState.GameOver:
                if (input.WasPressed(Key.Enter) || input.WasPressed(Key.Space))
                {
                    State = GameState.TitleScreen;
                }
                break;
        }

        input.EndFrame();
    }

    private void StartNewGame()
    {
        Score = 0;
        Lives = StartingLives;
        Level = 0;
        _nextExtraLifeScore = ExtraLifeScoreStep;
        _asteroids.Clear();
        _bullets.Clear();
        _particles.Clear();
        _saucer = null;
        _sound.StopSaucer();
        _sound.StopThrust();
        Ship.ResetForRespawn(BoundsCenter());
        Ship.InvulnerabilityRemaining = 0f;
        _extraAsteroidTimer = ExtraAsteroidIntervalSeconds;
        State = GameState.Playing;
        StartNextWave();
    }

    private void StartNextWave()
    {
        Level++;
        int count = Math.Min(3 + Level, 11);
        for (int i = 0; i < count; i++)
        {
            _asteroids.Add(Asteroid.CreateRandom(AsteroidSize.Large, RandomEdgePosition(), _rng));
        }
        ResetSaucerTimer();
    }

    private void ResetSaucerTimer()
    {
        _saucerSpawnTimer = MinSaucerSpawnSeconds + (float)_rng.NextDouble() * (MaxSaucerSpawnSeconds - MinSaucerSpawnSeconds);
    }

    private Vector2 BoundsCenter() => new((float)_bounds.Width / 2f, (float)_bounds.Height / 2f);

    private Vector2 RandomEdgePosition()
    {
        Vector2 center = BoundsCenter();
        Vector2 position;
        int attempts = 0;
        do
        {
            double edge = _rng.NextDouble() * 4;
            position = edge switch
            {
                < 1 => new Vector2((float)(_rng.NextDouble() * _bounds.Width), 0f),
                < 2 => new Vector2((float)_bounds.Width, (float)(_rng.NextDouble() * _bounds.Height)),
                < 3 => new Vector2((float)(_rng.NextDouble() * _bounds.Width), (float)_bounds.Height),
                _ => new Vector2(0f, (float)(_rng.NextDouble() * _bounds.Height)),
            };
            attempts++;
        } while (Vector2.Distance(position, center) < ShipSafeSpawnRadius && attempts < 8);

        return position;
    }

    private void UpdatePlaying(float dt, InputManager input)
    {
        HandleShipInput(dt, input);

        Ship.Update(dt, _bounds);

        foreach (var asteroid in _asteroids)
        {
            asteroid.Update(dt, _bounds);
        }

        ResolveAsteroidCollisions();

        foreach (var bullet in _bullets)
        {
            bullet.Update(dt, _bounds);
        }
        _bullets.RemoveAll(b => !b.IsAlive);

        UpdateSaucer(dt);
        UpdateParticles(dt);
        UpdateExtraAsteroidTimer(dt);

        ResolveCollisions();

        if (_asteroids.Count == 0 && _saucer is null)
        {
            StartNextWave();
        }
    }

    private void UpdateExtraAsteroidTimer(float dt)
    {
        _extraAsteroidTimer -= dt;
        if (_extraAsteroidTimer <= 0f)
        {
            _extraAsteroidTimer += ExtraAsteroidIntervalSeconds;
            _asteroids.Add(Asteroid.CreateRandom(AsteroidSize.Large, RandomEdgePosition(), _rng));
        }
    }

    private void HandleShipInput(float dt, InputManager input)
    {
        if (input.IsDown(Key.Left) || input.IsDown(Key.A))
        {
            Ship.RotateLeft(dt);
        }
        if (input.IsDown(Key.Right) || input.IsDown(Key.D))
        {
            Ship.RotateRight(dt);
        }

        bool thrusting = input.IsDown(Key.Up) || input.IsDown(Key.W);
        if (thrusting)
        {
            Ship.ApplyThrust(dt);
            _sound.StartThrust();
        }
        else
        {
            Ship.IsThrusting = false;
            _sound.StopThrust();
        }

        if (input.IsDown(Key.Space) 
            && Ship.TryFire(out var bullet) 
            && bullet is not null)
        {
            _bullets.Add(bullet);
            _sound.PlayFire();
        }

        if (input.WasPressed(Key.LeftShift))
        {
            Ship.Hyperspace(_rng, _bounds);
        }
    }

    private void UpdateSaucer(float dt)
    {
        if (_saucer is null)
        {
            _saucerSpawnTimer -= dt;
            if (_saucerSpawnTimer <= 0f)
            {
                SpawnSaucer();
            }
            return;
        }

        _saucer.Update(dt, _bounds);
        _sound.StartSaucer();

        if (_saucer.TryFire(Ship.Position, out var bullet) && bullet is not null)
        {
            _bullets.Add(bullet);
        }
    }

    private void SpawnSaucer()
    {
        var size = _rng.NextDouble() < 0.6 ? SaucerSize.Big : SaucerSize.Small;
        float direction = _rng.NextDouble() < 0.5 ? 1f : -1f;
        float y = (float)(_rng.NextDouble() * _bounds.Height);
        float x = direction > 0 ? 0f : (float)_bounds.Width;
        _saucer = new Saucer(size, new Vector2(x, y), direction, _rng);
    }

    private void UpdateParticles(float dt)
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            var particle = _particles[i];
            particle.Position += particle.Velocity * dt;
            particle.LifeRemaining -= dt;
            _particles[i] = particle;
        }
        _particles.RemoveAll(p => p.LifeRemaining <= 0f);
    }

    private void ResolveAsteroidCollisions()
    {
        for (int i = 0; i < _asteroids.Count; i++)
        {
            var a = _asteroids[i];
            if (!a.IsAlive)
            {
                continue;
            }

            for (int j = i + 1; j < _asteroids.Count; j++)
            {
                var b = _asteroids[j];
                if (!b.IsAlive)
                {
                    continue;
                }

                if (!CollisionHelper.CirclesIntersect(a.Position, a.Radius, b.Position, b.Radius))
                {
                    continue;
                }

                float closingSpeed = CollisionHelper.ResolveElasticCollision(a, b);
                if (a.Size == b.Size && a.Size != AsteroidSize.Small && closingSpeed >= AsteroidFragmentationSpeed)
                {
                    DestroyAsteroid(a, awardScore: false);
                    DestroyAsteroid(b, awardScore: false);
                }

                if (!a.IsAlive)
                {
                    break;
                }
            }
        }

        _asteroids.RemoveAll(a => !a.IsAlive);
    }

    private void ResolveCollisions()
    {
        bool shipHitThisFrame = false;
        bool shipWasVulnerable = !Ship.IsInvulnerable;

        if (shipWasVulnerable)
        {
            foreach (var asteroid in _asteroids)
            {
                if (CollisionHelper.CirclesIntersect(Ship.Position, Ship.Radius, asteroid.Position, asteroid.Radius))
                {
                    DestroyAsteroid(asteroid, awardScore: true);
                    shipHitThisFrame = true;
                    break;
                }
            }

            if (!shipHitThisFrame && _saucer is { IsAlive: true } saucer &&
                CollisionHelper.CirclesIntersect(Ship.Position, Ship.Radius, saucer.Position, saucer.Radius))
            {
                DestroySaucer(saucer, awardScore: true);
                shipHitThisFrame = true;
            }
        }

        for (int bi = _bullets.Count - 1; bi >= 0; bi--)
        {
            var bullet = _bullets[bi];
            if (!bullet.IsAlive)
            {
                continue;
            }

            if (bullet.Owner == BulletOwner.Player)
            {
                for (int ai = _asteroids.Count - 1; ai >= 0; ai--)
                {
                    var asteroid = _asteroids[ai];
                    if (CollisionHelper.CirclesIntersect(bullet.Position, bullet.Radius, asteroid.Position, asteroid.Radius))
                    {
                        bullet.IsAlive = false;
                        DestroyAsteroid(asteroid, awardScore: true);
                        break;
                    }
                }

                if (bullet.IsAlive && _saucer is { IsAlive: true } saucer &&
                    CollisionHelper.CirclesIntersect(bullet.Position, bullet.Radius, saucer.Position, saucer.Radius))
                {
                    bullet.IsAlive = false;
                    DestroySaucer(saucer, awardScore: true);
                }
            }
            else if (bullet.Owner == BulletOwner.Saucer && !shipHitThisFrame && shipWasVulnerable &&
                     CollisionHelper.CirclesIntersect(bullet.Position, bullet.Radius, Ship.Position, Ship.Radius))
            {
                bullet.IsAlive = false;
                shipHitThisFrame = true;
            }
        }

        if (shipHitThisFrame)
        {
            KillShip();
        }

        _bullets.RemoveAll(b => !b.IsAlive);
        _asteroids.RemoveAll(a => !a.IsAlive);
    }

    private void DestroyAsteroid(Asteroid asteroid, bool awardScore)
    {
        asteroid.IsAlive = false;
        SpawnDebris(asteroid.Position, asteroid.Radius);

        switch (asteroid.Size)
        {
            case AsteroidSize.Large:
                _sound.PlayExplosionLarge();
                if (awardScore) AddScore(20);
                break;
            case AsteroidSize.Medium:
                _sound.PlayExplosionMedium();
                if (awardScore) AddScore(50);
                break;
            case AsteroidSize.Small:
                _sound.PlayExplosionSmall();
                if (awardScore) AddScore(100);
                break;
        }

        _asteroids.AddRange(asteroid.Split(_rng));
    }

    private void DestroySaucer(Saucer saucer, bool awardScore)
    {
        saucer.IsAlive = false;
        SpawnDebris(saucer.Position, saucer.Radius);
        _sound.PlayExplosionMedium();
        _sound.StopSaucer();
        if (awardScore)
        {
            AddScore(saucer.PointValue);
        }
        _saucer = null;
        ResetSaucerTimer();
    }

    private void KillShip()
    {
        if (!Ship.IsAlive)
        {
            return;
        }

        SpawnDebris(Ship.Position, Ship.Radius * 1.5f);
        _sound.PlayExplosionLarge();
        _sound.StopThrust();
        Lives--;

        if (Lives <= 0)
        {
            State = GameState.GameOver;
            Ship.IsAlive = false;
        }
        else
        {
            Ship.ResetForRespawn(BoundsCenter());
        }
    }

    private void AddScore(int points)
    {
        Score += points;
        if (Score >= _nextExtraLifeScore)
        {
            Lives++;
            _nextExtraLifeScore += ExtraLifeScoreStep;
            _sound.PlayExtraLife();
        }
    }

    private void SpawnDebris(Vector2 position, float scale)
    {
        int count = 8 + _rng.Next(6);
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rng.NextDouble() * MathF.Tau);
            float speed = 40f + (float)_rng.NextDouble() * 90f;
            float life = 0.3f + (float)_rng.NextDouble() * 0.4f;
            _particles.Add(new Particle
            {
                Position = position,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                LifeRemaining = life,
                MaxLife = life,
            });
        }
    }
}
