namespace EMirrorsScores.Plane;

using SEClient.Tcp;

public struct Intersection
{
    public int ID;
    public string PlaneName;
    public Point3D Gaze;
    public Point2D Point;
}


/// <summary>
/// Base for objects representing SmartEye plane/screen
/// </summary>
public class Plane
{
    public enum Event
    {
        Enter,
        Exit
    }

    public bool IsEnabled { get; set; } = true;

    public string PlaneName => _planeName;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="planeName">SmartEye plane or screen name</param>
    public Plane(string planeName)
    {
        _planeName = planeName;
    }

    /// <summary>
    /// Notifies about gaze-on and gaze-off events
    /// </summary>
    /// <param name="evt">Gaze event</param>
    public void Notify(Event evt)
    {
        if (!IsEnabled)
            return;

        // _logger.Add(Logging.LogSource.Tracker, evt.ToString(), PlaneName);
        _statistics.Feed(PlaneName, evt);

        HandleEvent(evt);
    }

    /// <summary>
    /// Consumes the gaze point that fell onto a plane
    /// </summary>
    /// <param name="intersection"></param>
    public void Feed(Intersection intersection)
    {
        if (!IsEnabled)
            return;

        HandleIntersection(intersection);
    }

    // Internal

    readonly string _planeName;
    // readonly Logging.FlowLogger _logger = Logging.FlowLogger.Instance;
    readonly Logging.Statistics _statistics = Logging.Statistics.Instance;

    protected virtual void HandleIntersection(Intersection intersection) { }
    protected virtual void HandleEvent(Event evt) { }
}
