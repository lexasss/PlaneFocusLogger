using EMirrorsScores.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Windows;

namespace EMirrorsScores.Logging;

internal class Statistics
{
    public static Statistics Instance => _instance ??= new();

    public void Feed(string name, Plane.Plane.Event evt)
    {
        if (!_planes.ContainsKey(name))
        {
            _planes.Add(name, new Entry());
        }

        var entry = _planes[name];
        entry.Feed(evt);
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

        public void Feed(Plane.Plane.Event evt)
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
        }

        // Internal

        private long _totalTime = 0;
        private long _startedAt = 0;
    }

    static Statistics? _instance = null;

    readonly Dictionary<string, Entry> _planes = new();

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
