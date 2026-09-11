using System.IO;
using System.Media;

namespace AsteroidsGC.Engine;

public sealed class SoundManager
{
    public SoundManager()
    {
        _fire = Load(SoundGenerator.GenerateBeep(1100, 0.07, 0.4));

        _explosionLargePool = CreatePool(() =>
            SoundGenerator.GenerateNoiseBurst(0.5, 0.55, 300));
        _explosionMediumPool = CreatePool(() =>
            SoundGenerator.GenerateNoiseBurst(0.35, 0.55, 600));
        _explosionSmallPool = CreatePool(() =>
            SoundGenerator.GenerateNoiseBurst(0.22, 0.55, 1200));

        _thrust = Load(SoundGenerator.GenerateLoopingRumble(0.4, 65, 0.22));
        _saucer = Load(SoundGenerator.GenerateWarble(480, 620, 6, 1.0, 0.28));
        _extraLife = Load(SoundGenerator.GenerateArpeggio([440, 554, 659, 880], 0.09, 0.4));
        _soundOnChime = Load(SoundGenerator.GenerateArpeggio([660, 880, 1100], 0.06, 0.35));
    }

    public bool SoundEnabled { get; private set; } = true;

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        if (SoundEnabled)
        {
            TryPlay(_soundOnChime);
        }
        else
        {
            StopThrust();
            StopSaucer();
        }
    }

    private static SoundPlayer[] CreatePool(Func<MemoryStream> factory, int size = 2)
    {
        var pool = new SoundPlayer[size];
        for (int i = 0; i < size; i++)
        {
            pool[i] = Load(factory());
        }
        return pool;
    }

    private static SoundPlayer Load(MemoryStream stream)
    {
        var player = new SoundPlayer(stream);
        try
        {
            player.Load();
        }
        catch (Exception)
        {
            // Audio device unavailable; sound effects become no-ops.
        }
        return player;
    }

    public void PlayFire() => TryPlay(_fire);

    public void PlayExplosionLarge() =>
        TryPlay(NextInPool(_explosionLargePool, ref _explosionLargeIndex));

    public void PlayExplosionMedium() =>
        TryPlay(NextInPool(_explosionMediumPool, ref _explosionMediumIndex));

    public void PlayExplosionSmall() =>
        TryPlay(NextInPool(_explosionSmallPool, ref _explosionSmallIndex));

    public void PlayExtraLife() => TryPlay(_extraLife);

    public void StartThrust()
    {
        if (_thrustPlaying)
        {
            return;
        }
        _thrustPlaying = true;
        TryPlayLooping(_thrust);
    }

    public void StopThrust()
    {
        if (!_thrustPlaying)
        {
            return;
        }
        _thrustPlaying = false;
        TryStop(_thrust);
    }

    public void StartSaucer()
    {
        if (_saucerPlaying)
        {
            return;
        }
        _saucerPlaying = true;
        TryPlayLooping(_saucer);
    }

    public void StopSaucer()
    {
        if (!_saucerPlaying)
        {
            return;
        }
        _saucerPlaying = false;
        TryStop(_saucer);
    }

    private static SoundPlayer NextInPool(SoundPlayer[] pool, ref int index)
    {
        var player = pool[index];
        index = (index + 1) % pool.Length;
        return player;
    }

    private void TryPlay(SoundPlayer player)
    {
        if (!SoundEnabled)
        {
            return;
        }
        try { player.Play(); } catch (Exception) { }
    }

    private void TryPlayLooping(SoundPlayer player)
    {
        if (!SoundEnabled)
        {
            return;
        }
        try { player.PlayLooping(); } catch (Exception) { }
    }

    private static void TryStop(SoundPlayer player)
    {
        try { player.Stop(); } catch (Exception) { }
    }

    private readonly SoundPlayer _fire;
    private readonly SoundPlayer[] _explosionLargePool;
    private readonly SoundPlayer[] _explosionMediumPool;
    private readonly SoundPlayer[] _explosionSmallPool;
    private readonly SoundPlayer _thrust;
    private readonly SoundPlayer _saucer;
    private readonly SoundPlayer _extraLife;
    private readonly SoundPlayer _soundOnChime;

    private int _explosionLargeIndex;
    private int _explosionMediumIndex;
    private int _explosionSmallIndex;
    private bool _thrustPlaying;
    private bool _saucerPlaying;
}
