namespace EMirrorsScores.Logging;

/// <summary>
/// Implements a logger use to log syncronized data from various routines 
/// </summary>
public class FlowLogger : Logger<FlowLogger.Record>
{
    public class Record
    {
        public static string DELIM => "\t";
        public static string HEADER => $"ts{DELIM}source{DELIM}type{DELIM}data";

        public long Timestamp { get; private set; }
        public LogSource Source { get; private set; }
        public string Type { get; private set; }
        public string[] Data { get; private set; }

        public Record(LogSource source, string type, string[] data)
        {
            Timestamp = Utils.Timestamp.Ms;
            Source = source;
            Type = type;
            Data = data;
        }

        public override string ToString()
        {
            var result = $"{Timestamp}{DELIM}{Source}{DELIM}{Type}";
            if (Data != null && Data.Length > 0)
            {
                result += DELIM + string.Join(DELIM, Data);
            }

            return result;
        }
    }

    public static FlowLogger Instance => _instance ??= new ();

    public bool IsEnabled { get; set; } = false;

    public bool HasRecords => _records.Count > 0;
    public string Folder => _folder;

    public void Add(LogSource source, string type, params string[] data)
    {
        if (IsEnabled)
        {
            _records.Add(new Record(source, type, data));
        }
    }

    // Internal methods

    static FlowLogger? _instance = null;

    protected override string Header => Record.HEADER;
}
