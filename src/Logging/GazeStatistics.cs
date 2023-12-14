using EMirrorsScores.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Windows;

namespace EMirrorsScores.Logging;

internal class GazeStatistics
{
    public static GazeStatistics Instance => _instance ??= new();

    public bool IsEnabled
    { 
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (_isEnabled)
            {
                _startedAt = Timestamp.Ms;
            }
            else
            {
                foreach (var plane in _planes)
                    plane.Value.Feed(Plane.Plane.Event.Exit);
            }
        }
    }

    public void Feed(string name, Plane.Plane.Event evt)
    {
        if (!IsEnabled)
            return;

        if (!_planes.ContainsKey(name))
        {
            _planes.Add(name, new Entry());
        }

        var entry = _planes[name];
        entry.Feed(evt, _startedAt);

        _startedAt = 0;
    }

    public bool SaveTo(string filename)
    {
        var lines = _planes.Select(item =>
        {
            return $"{item.Key}\t{item.Value.TotalTime}";
        });

        return Save(filename, lines.ToImmutableSortedSet());
    }

    // Internal methods

    class Entry
    {
        public long TotalTime => _totalTime;

        public void Feed(Plane.Plane.Event evt, long loggingStartedAt = 0)
        {
            if (evt == Plane.Plane.Event.Enter)
            {
                _startedAt = Timestamp.Ms;
            }
            else if (_startedAt > 0)
            {
                _totalTime += Timestamp.Ms - _startedAt;
                _startedAt = 0;
            }
            else if (loggingStartedAt > 0)
            {
                _totalTime += Timestamp.Ms - loggingStartedAt;
            }
        }

        // Internal

        private long _totalTime = 0;
        private long _startedAt = 0;
    }

    static GazeStatistics? _instance = null;

    readonly Dictionary<string, Entry> _planes = new();

    bool _isEnabled = false;
    long _startedAt = 0;

    private static bool Save(string filename, IEnumerable<object> records, string header = "")
    {
        if (!Path.IsPathFullyQualified(filename))
        {
            filename = Path.Combine(FlowLogger.Instance.Folder, filename);
        }

        var folder = Path.GetDirectoryName(filename) ?? "";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using StreamWriter writer = File.CreateText(filename);

        try
        {
            if (!string.IsNullOrEmpty(header))
            {
                writer.WriteLine(header);
            }

            writer.WriteLine(string.Join("\n", records));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save data into\n'{filename}':\n\n{ex.Message}",
                Application.Current.MainWindow.Title + " - Statistics",
                MessageBoxButton.OK);
        }

        return false;
    }
}
