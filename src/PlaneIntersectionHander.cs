using PlaneFocusLogger.Plane;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PlaneFocusLogger;

class PlaneIntersectionHander
{
    public PlaneIntersectionHander(Dictionary<Panel, Plane.Plane> planes)
    {
        _planes = new PlaneCollection(planes.Values);
        _planeRenderer = new PlaneRenderer(planes.Keys);
    }

    public void Feed(SEClient.Tcp.Data.Sample sample)
    {
        HandleClosestIntersection(sample);
        HandleAllIntersections(sample);
    }

    public void Reset()
    {
        HandleClosestIntersection(new SEClient.Tcp.Data.Sample());
        HandleAllIntersections(new SEClient.Tcp.Data.Sample());
    }

    // Internal

    readonly PlaneCollection _planes;
    readonly PlaneRenderer _planeRenderer;

    string _currentIntersectionName = "";
    HashSet<string> _currentIntersectionNames = new();

    private void HandleClosestIntersection(SEClient.Tcp.Data.Sample sample)
    {
        var seClientOptions = SEClient.Options.Instance;
        var intersectionSource = (seClientOptions.IntersectionSource, seClientOptions.IntersectionSourceFiltered) switch
        {
            (SEClient.IntersectionSource.Gaze, false) => sample.ClosestWorldIntersection,
            (SEClient.IntersectionSource.Gaze, true) => sample.FilteredClosestWorldIntersection,
            (SEClient.IntersectionSource.AI, false) => sample.EstimatedClosestWorldIntersection,
            (SEClient.IntersectionSource.AI, true) => sample.FilteredEstimatedClosestWorldIntersection,
            _ => throw new Exception($"This intersection source is not implemented")
        };

        if (intersectionSource is SEClient.Tcp.WorldIntersection intersection)
        {
            var intersectionName = intersection.ObjectName.AsString;
            if (_currentIntersectionName != intersectionName)
            {
                _currentIntersectionName = intersectionName;
                _planes.Notify(Plane.Plane.Event.Enter, intersectionName);
            }
        }
        else if (!string.IsNullOrEmpty(_currentIntersectionName))
        {
            _planes.Notify(Plane.Plane.Event.Exit, _currentIntersectionName);
            _currentIntersectionName = "";
        }
    }

    private void HandleAllIntersections(SEClient.Tcp.Data.Sample sample)
    {
        var seClientOptions = SEClient.Options.Instance;
        var intersectionSource = (seClientOptions.IntersectionSource, seClientOptions.IntersectionSourceFiltered) switch
        {
            (SEClient.IntersectionSource.Gaze, false) => sample.AllWorldIntersections,
            (SEClient.IntersectionSource.Gaze, true) => sample.FilteredAllWorldIntersections,
            (SEClient.IntersectionSource.AI, false) => sample.EstimatedAllWorldIntersections,
            (SEClient.IntersectionSource.AI, true) => sample.FilteredEstimatedAllWorldIntersections,
            _ => throw new Exception($"This intersection source is not implemented")
        };

        var activePlanes = new HashSet<string>();
        if (intersectionSource is SEClient.Tcp.WorldIntersection[] intersections)
        {
            foreach (var intersection in intersections)
            {
                var intersectionName = intersection.ObjectName.AsString;
                activePlanes.Add(intersectionName);

                if (!_currentIntersectionNames.Contains(intersectionName))
                {
                    _planeRenderer.Enter(intersectionName);
                }
            }
        }

        _currentIntersectionNames.ExceptWith(activePlanes);
        foreach (var intersectionName in _currentIntersectionNames)
        {
            _planeRenderer.Exit(intersectionName);
        }

        _currentIntersectionNames = activePlanes;
    }
}
