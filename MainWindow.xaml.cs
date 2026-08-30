using System.IO;
using System.Windows;
using AsteroidsGC.Engine;
using AsteroidsGC.Game;

namespace AsteroidsGC;

public partial class MainWindow : Window
{
    private readonly InputManager _input = new();
    private readonly SoundManager _sound = new();
    private readonly GameManager _gameManager;
    private readonly GameLoop _gameLoop = new();

    public MainWindow()
    {
        InitializeComponent();

        ColorScheme.Load(Path.Combine(AppContext.BaseDirectory, "ColorSchemes", "synthwave-magenta-cyan.json"));

        _gameManager = new GameManager(_sound);
        Canvas.GameManager = _gameManager;

        KeyDown += (_, e) => _input.OnKeyDown(e.Key);
        KeyUp += (_, e) => _input.OnKeyUp(e.Key);
        Deactivated += (_, _) => _input.Clear();

        Loaded += (_, _) => Canvas.Focus();

        _gameLoop.Tick += OnTick;
        _gameLoop.Start();
        Closed += (_, _) => _gameLoop.Stop();
    }

    private void OnTick(float dt)
    {
        var bounds = new Rect(0, 0, Canvas.ActualWidth, Canvas.ActualHeight);
        _gameManager.Update(dt, _input, bounds);
        Canvas.InvalidateVisual();
    }
}
