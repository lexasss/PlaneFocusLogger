using PlaneFocusLogger.Utils;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlaneFocusLogger.Plane;

/// <summary>
/// Visually represents SmartEye planes/screens
/// </summary>
internal class PlaneRenderer
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="panels">UI panels that represent planes/screen</param>
    public PlaneRenderer(params Panel[] panels) : this((IEnumerable<Panel>)panels) { }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="panels">UI panels that represent planes/screen</param>
    public PlaneRenderer(IEnumerable<Panel> panels)
    {
        foreach (var panel in panels)
        {
            if (panel.Children.Count == 2 && panel.Children[1] is Label counter)
            {
                _planes.Add(panel.Tag, new Plane(panel, counter));
            }
        }
    }

    /// <summary>
    /// Must be called for each gaze-on event
    /// </summary>
    /// <param name="name">plane/screen name</param>
    public void Enter(string name)
    {
        if (!_planes.ContainsKey(name))
        {
            return;
        }

        _planes[name].Enter();
    }

    /// <summary>
    /// Must be called for each gaze-off event
    /// </summary>
    /// <param name="name">plane/screen name</param>
    public void Exit(string name)
    {
        if (!_planes.ContainsKey(name))
        {
            return;
        }

        _planes[name].Exit();
    }

    /// <summary>
    /// Resets the state of all planes/screens
    /// </summary>
    public void Reset()
    {
        foreach (var plane in _planes.Values)
        {
            plane.Reset();
        }
    }

    // Internal

    class Plane
    {
        public Panel Panel { get; }
        public Label Counter { get; }
        public long Time { get; private set; } = 0;

        public Plane(Panel panel, Label counter)
        {
            Panel = panel;
            Counter = counter;
        }

        public void Enter()
        {
            Panel.Background = ACTIVE_BACKGROUND;
            _startMs = Timestamp.Ms;
        }

        public void Exit()
        {
            Panel.Background = NORMAL_BACKGROUND;
            if (_startMs > 0)
            {
                Time += Timestamp.Ms - _startMs;
                Counter.Content = Time;
            }
        }

        public void Reset()
        {
            Panel.Background = NORMAL_BACKGROUND;
            Counter.Content = 0;
            Time = 0;

            _startMs = 0;
        }

        // Internal

        readonly Brush NORMAL_BACKGROUND = Brushes.Bisque;
        readonly Brush ACTIVE_BACKGROUND = Brushes.Tomato;

        long _startMs = 0;
    }

    readonly Dictionary<object, Plane> _planes = new();
}
