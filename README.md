# Asteroids-GC

A from-scratch clone of Atari's 1979 arcade classic *Asteroids*, built with C# on .NET 10 and [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/). It reproduces the original's vector-monitor look.

![asteroids-gc_0-1-3_main-menu](https://github.com/user-attachments/assets/6cdf1950-890c-4ff5-a6e8-a78a19437ce5)

- [How to play](#how-to-play)
- [Gameplay rules](#gameplay-rules)
- [Color schemes](#color-schemes)
- [Implementation notes](#implementation-notes)
- [Physics](#physics)
- [Project structure](#project-structure)
- [History](#history)

*Asteroids* was designed by Lyle Rains and Ed Logg and released by Atari, Inc. in November 1979. It ran on custom vector-graphics hardware (an Atari Digital Vector Generator) rather than a raster framebuffer — the monitor's electron beam literally drew glowing line segments directly onto the phosphor screen instead of lighting up a grid of pixels. That hardware is the reason for the game's iconic look: crisp, perfectly anti-aliased lines with a faint glow bleeding off their edges, on a pure black background.

The game was a commercial phenomenon, becoming one of the best-selling and most influential arcade games of the golden age. Its core loop — a lone ship drifting with Newtonian inertia through a field of tumbling rocks, shattering each one into smaller and smaller pieces — has been cloned, ported, and referenced in countless games since, and its wraparound playfield and "split on hit" asteroid mechanic have become genre staples in their own right.

This project is an homage to that original: it does not use any Atari code or assets (none survive in reusable form anyway — the original ran on discrete vector hardware), but it deliberately imitates the *presentation* — thin glowing vector-style outlines, a black void, procedurally irregular rock silhouettes — while running on entirely modern, unrelated technology (WPF's 2D retained/immediate-mode drawing over a GPU-composited desktop window).

## How to play

| Action | Keys |
| --- | --- |
| Rotate left / right | `←` `→` or `A` `D` |
| Thrust | `↑` or `W` |
| Fire | `Space` |
| Hyperspace jump | `Left Shift` |
| Pause / resume | `Esc` |
| Quit to title screen (while paused) | `Q` |
| Select color scheme (title screen) | `1`-`9` |
| Start / restart | `Enter` or `Space` |

Run it with:

```shell
dotnet run
```

## Gameplay rules

- **Lives and scoring.** You start with 3 lives. Destroying an asteroid awards points based on its size (small asteroids are worth more than large ones, rewarding precision), and destroying the saucer awards a flat bonus. An extra life is granted every 10,000 points.
- **Asteroids split when destroyed.** A **Large** asteroid breaks into two **Medium** ones, a **Medium** breaks into two **Small** ones, and a **Small** asteroid is destroyed outright. Every asteroid's outline is generated with a random number of vertices and randomized per-vertex radius, so no two ever look alike, and each spins at its own random rate.
- **Asteroids can also collide with each other.** Unlike the 1979 original — where rocks silently pass through one another — this version gives asteroids real momentum: on contact they bounce apart elastically. If two *same-sized* asteroids hit each other hard enough, the impact itself shatters both, exactly as a bullet would (this never awards score, since the player didn't cause it). Small asteroids never shatter this way — they only ever bounce.
- **A new large asteroid drifts in from the edge of the screen every 60 seconds**, on top of whatever a wave has left, so the field never fully empties out or goes stale on a slow level.
- **The flying saucer** appears periodically and comes in two sizes: the large saucer fires in random directions (low accuracy, low value), while the small saucer aims at the player with a bit of jitter (higher accuracy, higher value).
- **Hyperspace** teleports your ship to a random point on screen with a brief moment of invulnerability — a classic panic button, useful when cornered but with no guarantee you'll land somewhere safer. The jump leaves behind a trail of scattering particles strung out along the line from your old position to the new one, so the teleport reads as a disintegrate-and-reassemble rather than an instant cut.
- **Pausing lets you bail out.** Beyond resuming with `Esc`/`Enter`, pressing `Q` while paused ends the current run outright and drops you back on the title screen — no need to burn through your remaining lives or wait for Game Over just to back out.
- **Screen wraparound.** Every object — ship, asteroids, bullets, the saucer — that drifts off one edge of the screen reappears on the opposite edge.
- **Waves.** Once every asteroid and the saucer are gone, the next wave spawns with more asteroids than the last (up to a cap), so the game gradually escalates.
- **The game ends** when your last life is lost; `Enter` from the Game Over screen returns you to the title screen to start again.

## Color schemes

The game's entire palette — ship, asteroids, bullets, saucer, particles, HUD text, engine flame, and the two strokes of the title-screen "GC" logo — is data, not code. Each scheme is a small JSON file in `ColorSchemes/` with nine `#RRGGBB` fields:

- `ship`
- `asteroid`
- `bullet`
- `saucer`
- `particle`
- `text`
- `thrustFlame`
- `titleLogoPrimary`
- `titleLogoSecondary`

At startup `ColorScheme.DiscoverPresets` scans that folder alphabetically (up to nine) and the title screen lists whatever it finds, one line per file, selectable on the spot with `1`-`9` — the whole game repaints instantly, no restart needed. Nine presets ship out of the box:

1. Amber Neon
2. Black & White
3. Classic CRT (the original palette this game launched with)
4. Electric Violet/Lime
5. Hot Pink & Gold
6. Ice Blue
7. Spectrum ZX
8. Synthwave Magenta/Cyan
9. Toxic Lime

Adding your own is just dropping another `.json` file into `ColorSchemes/` — no code changes required.

```json
{
  "name": "Classic CRT",

  "ship": "#FFFFFF",
  "asteroid": "#D2FFDC",
  "bullet": "#FFFFFF",
  "saucer": "#A0FFAA",
  "particle": "#FFE1A0",
  "text": "#FFFFFF",
  "thrustFlame": "#FF4500",
  "titleLogoPrimary": "#D2FFDC",
  "titleLogoSecondary": "#D2FFDC"
}
```

## Implementation notes

- **Immediate-mode rendering, not Shapes.** Rather than adding a WPF `Shape`/`Path` element per asteroid/bullet/particle (which would mean dozens of live `DependencyObject`s with layout and property-invalidation overhead, and constant visual-tree churn as asteroids split and bullets expire), the whole scene is painted by a single custom `FrameworkElement` (`GameCanvas`) that overrides `OnRender(DrawingContext)`. Every entity is redrawn from scratch each frame with plain `DrawGeometry`/`DrawLine`/`DrawText` calls — cheap, GPU-composited, and free of per-object WPF overhead.
- **The CRT phosphor glow is faked, not computed.** WPF's `BitmapEffect` is deprecated, and a pixel-shader `Effect`/`BlurEffect` risks silently falling back to a slow *software* rasterization path on machines without solid GPU acceleration (a real concern for a desktop game meant to "just work" everywhere) — and it would have to re-rasterize an offscreen surface every single frame. Instead, `GlowRenderer.DrawGlowPolyline` draws every outline **five times**, back to front, with progressively larger pen thickness and lower opacity (from a soft 14px/14% halo down to a crisp 1.25px/100% core stroke). Layering several translucent strokes of the same hue over black naturally builds up a convincing soft-edged bloom, entirely with the standard 2D drawing API and no offscreen render targets.
- **The game loop rides `CompositionTarget.Rendering`, not a `DispatcherTimer`.** `CompositionTarget.Rendering` fires once per actual composed frame, so it stays in step with what's really being painted; a `DispatcherTimer`'s interval is only a *minimum* delay and drifts relative to the compositor, which would show up as visible jitter. A `Stopwatch`-measured delta time (clamped to avoid a huge jump after the window is minimized or a breakpoint pauses execution) is handed to the whole simulation every tick, so gameplay speed never depends on frame rate.
- **Held-key input is tracked manually.** WPF's `KeyDown` event auto-repeats at an OS-controlled delay-then-rate, which is the wrong shape for continuous rotation/thrust (a stutter-start, then a fixed cadence, instead of smooth per-frame response). `InputManager` instead accumulates currently-held keys into a `HashSet<Key>` from raw `KeyDown`/`KeyUp` events and is polled once per simulation tick; it's cleared on `Window.Deactivated` so alt-tabbing away can't leave a "stuck" key.
- **Sound needs no shipped audio files.** Every effect (fire, three sizes of explosion, engine rumble, saucer warble, extra-life jingle) is a short PCM waveform synthesized on the fly in memory (`Engine/SoundGenerator.cs` — square-wave beeps, filtered noise bursts, warbling tones) and handed to `System.Media.SoundPlayer` as a `MemoryStream`, so the whole game is a single self-contained executable with zero binary asset dependencies.
- **A CRT-style overlay finishes the look.** After the scene is drawn, `GameCanvas` layers on faint horizontal scanlines and a radial vignette (dark corners, clear center) at low opacity — a cheap final pass that sells the "old monitor" feel without touching the underlying vector art.
- **The "GC" logo is geometry, not a font.** There's no vector font anywhere in this codebase, so the title-screen watermark is built procedurally: a horseshoe-shaped ring (outer/inner arc with a gap) reads as a "C", and the same ring with a narrower gap plus a small connecting bar reads as a "G" (`Game/TitleLogo.cs`). Every vertex wobbles continuously as the sum of two sine waves with its own randomly-assigned frequency and phase, so the outline breathes forever with no shared cycle or reset point to "tick" on — a deliberate departure from an earlier reroll-and-lerp-every-second version, which visibly paused at each cycle boundary.
- **The parallax dust field is plain filled dots, not glow passes.** Three layers of points (`Game/Dustfield.cs`) sit at unit-space (0-1) coordinates, each layer shifted by a different small fraction of the ship's position (or, on the title screen, of a slow constant drift picked once at random) and wrapped back into the screen — the classic "distant stuff barely moves, near stuff moves more" trick, without ever scrolling an actual camera. They're drawn with a plain `DrawEllipse` fill rather than routing through the five-pass `GlowRenderer`: a hundred-plus points getting the full glow treatment every frame would mean hundreds of extra `Pen`/`Brush` allocations and would render as soft blurry blobs instead of crisp specks.
- **Color is state, not code.** `ColorScheme.Current` is a single swappable snapshot, replaced wholesale when a JSON preset loads (`System.Text.Json` plus `ColorConverter.ConvertFromString` for the `#RRGGBB` parsing), falling back safely to a built-in default palette if a file is missing or malformed. No drawing method holds a color of its own anymore — everything reads `ColorScheme.Current.*` at render time, so switching schemes live is just a matter of swapping that one reference.

## Physics

- **Vector math via `System.Numerics.Vector2`.** Position and velocity use the BCL's built-in 2D vector type rather than a hand-rolled one — it already provides everything needed (`+`, `-`, `*`, `Length()`, `DistanceSquared`, `Dot`), so there's no reason to reinvent it.
- **The ship has real inertia.** Thrust adds acceleration along the ship's current facing each frame the key is held; it does **not** set velocity directly. Between frames, velocity decays by an exponential drag factor (`Velocity *= Drag^dt`), so the ship coasts and slowly bleeds off speed rather than snapping to a stop — the same "sluggish, momentum-heavy" feel that makes the original game's flying so distinctive (and occasionally frustrating).
- **Collision detection is circle-vs-circle**, not polygon-accurate. Every object carries a single bounding-`Radius`, and two objects are considered touching when the distance between their centers is less than the sum of their radii. This is exactly the fidelity the original game itself used, and for rocks, bullets, a ship, and a saucer it's indistinguishable from "true" polygon collision in practice — at a tiny fraction of the computational cost, checked brute-force against every pair each frame (entity counts here are always small enough that this is effortless).
- **Asteroid-vs-asteroid collisions are a genuine elastic-impulse simulation.** `CollisionHelper.ResolveElasticCollision` treats each asteroid's mass as proportional to `Radius²` (a reasonable stand-in for a 2D disc's area), computes the standard elastic-collision impulse along the contact normal, and applies it to both bodies' velocities in opposite directions — real momentum exchange, not a scripted "bounce off." A **positional correction** step also nudges the two apart by half their overlap along that same normal, which stops them from continuously re-reporting the same collision on subsequent frames (a standard technique borrowed from general-purpose physics engines). The method returns the pre-impulse closing speed, which the game then compares against a threshold to decide whether the hit was hard enough to also shatter both rocks.
- **Screen wraparound is a physics detail, not a rendering trick.** Every object's `Update` step wraps its raw position back onto the opposite edge the instant it exits the bounds, so velocity and subsequent collision checks always operate on a single consistent coordinate — there's no seam-crossing special case anywhere else in the simulation.
- **Frame-rate independence throughout.** Every mutation of position, velocity, or a timer is scaled by the frame's `dt`, from thrust and drag to bullet lifetimes, invulnerability countdowns, and even the thrust flame's flicker animation (`Ship.GetThrustFlameShape` advances an internal clock by `dt` each call and derives the flame's length/width from two layered sine waves of that clock) — so the game behaves identically whether it's rendering at 60 Hz, 144 Hz, or briefly stuttering.

## Project structure

```plain
AsteroidsGC.csproj          net10.0-windows, WPF
App.xaml(.cs)               application entry point
MainWindow.xaml(.cs)        hosts GameCanvas, owns the game loop and input wiring
GameCanvas.cs               immediate-mode rendering: glow, entities, HUD, CRT overlay

Engine/                     reusable, game-agnostic building blocks
    GameLoop.cs             CompositionTarget.Rendering + delta-time
    GameObject.cs           base entity: position/velocity/rotation/radius
    InputManager.cs         held-key tracking
    CollisionHelper.cs      circle intersection + elastic collision response
    GlowRenderer.cs         the multi-pass phosphor-bloom drawing helper
    SoundGenerator.cs / SoundManager.cs   synthesized SFX, no audio assets
    ColorScheme.cs          loads/holds the active JSON color scheme

Game/                       the game itself
    GameManager.cs          state machine, waves, scoring, collision resolution
    Ship.cs / Asteroid.cs / Bullet.cs / Saucer.cs / Particle.cs
    GameState.cs
    TitleLogo.cs            procedural animated "GC" watermark for the title screen
    Dustfield.cs            parallax dust points for the game and title screens

ColorSchemes/               *.json — nine selectable color palettes, see "Color schemes" above
```

## History

The current build is shown as **v0.1.3** in the bottom-right corner of the title screen (`GameCanvas.Version`).

- Two more presets, Black & White and ZX Spectrum, bringing the total to nine — the full `1`-`9` hotkey range is now in use.
- A three-layer parallax dust field (`Game/Dustfield.cs`) shifts relative to the ship on the game screen, and drifts slowly on its own on the title screen — see [Implementation notes](#implementation-notes).
- Hyperspace jumps now scatter a trail of particles along the line from the old position to the new one instead of teleporting silently.
- `Q` while paused ends the run and returns to the title screen without needing to lose all remaining lives first.
- `Engine/ColorScheme.cs` plus seven JSON presets — the game's whole palette becomes swappable data, selectable live from the title screen. See [Color schemes](#color-schemes).
- A procedurally-built, continuously-deforming vector "GC" watermark (`Game/TitleLogo.cs`) sits behind the title text.
- Give the project a final name `AsteroidsGC`, and added this README.
- Introduced the version stamp shown in the bottom-right corner of the title screen, alongside a round of small gameplay/rendering cleanups.
- The engine flame is no longer a static triangle: `Ship.GetThrustFlameShape` advances an internal clock by each frame's delta time and drives the flame's length and width from two layered sine waves, so it flickers smoothly and continuously while thrusting instead of holding one fixed shape.
- The complete first playable build: ship with inertial thrust/drag, procedurally-shaped asteroids that split on impact, bullets, the flying saucer, particle debris, lives/score/level HUD, the title/pause/game-over screens, synthesized sound effects, and the multi-pass CRT-glow renderer.
