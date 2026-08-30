using System.Windows.Input;

namespace AsteroidsGC.Engine;

public sealed class InputManager
{
    private readonly HashSet<Key> _held = [];
    private readonly HashSet<Key> _pressedThisFrame = [];

    public void OnKeyDown(Key key)
    {
        if (_held.Add(key))
        {
            _pressedThisFrame.Add(key);
        }
    }

    public void OnKeyUp(Key key)
    {
        _held.Remove(key);
    }

    public void Clear()
    {
        _held.Clear();
        _pressedThisFrame.Clear();
    }

    public bool IsDown(Key key) => _held.Contains(key);

    public bool WasPressed(Key key) => _pressedThisFrame.Contains(key);

    public void EndFrame()
    {
        _pressedThisFrame.Clear();
    }
}
