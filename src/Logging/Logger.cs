using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace EMirrorsScores.Logging;

[Flags]
public enum LogSource
{
    Tracker = 1,
    Driver = 2,
    Distractor = 3,
    Lane = 4,
    Carla = 5,
}

public enum SavingResult
{
    Save,
    Discard,
}

/// <summary>
/// Base class for logging classes
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Logger<T> : IDisposable where T : class
{
    /// <summary>
    /// Saves the log with a specified file name.
    /// Shows the dilaog box to select the file location and name
    /// </summary>
    /// <param name="defaultFileName">Default file name</param>
    /// <param name="greeting">Optional header placed in the beginning of the log file</param>
    /// <returns>saving result</returns>
    public SavingResult SaveTo(string defaultFileName, string greeting = "")
    {
        var filename = defaultFileName;
        var result = PromptToSave(ref filename, greeting);
        if (result == SavingResult.Save)
        {
            result = Save(filename, _records, Header) ? SavingResult.Save : SavingResult.Discard;
        }

        _records.Clear();

        return result;
    }

    /// <summary>
    /// Clears th elog buffer
    /// </summary>
    public void Clear()
    {
        _records.Clear();
    }

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }


    // Internal

    protected readonly List<T> _records = new ();

    protected abstract string Header { get; }

    protected string _folder;

    protected readonly Server _server = new Server();

    protected Logger()
    {
        _folder = Properties.Settings.Default.DataFolder;
        if (string.IsNullOrEmpty(_folder))
        {
            _folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }

    protected SavingResult PromptToSave(ref string? filename, string greeting = "")
    {
        if (!string.IsNullOrEmpty(greeting))
        {
            greeting += "\n";
        }

        var answer = MessageBox.Show(
            $"{greeting}Save data into\n'{_folder}\\{filename}'?\n\n" +
            $"Press 'No' to change the name or folder\n" +
            $"Press 'Cancel' to abandon data",
            Application.Current.MainWindow.Title + " - Logger",
            MessageBoxButton.YesNoCancel, 
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Cancel)
        {
            return SavingResult.Discard;
        }
        else if (answer == MessageBoxResult.No)
        {
            filename = AskFileName(filename);
            return string.IsNullOrEmpty(filename) ? SavingResult.Discard : SavingResult.Save;
        }
        else
        {
            filename = Path.Combine(_folder, filename ?? "");
            return SavingResult.Save;
        }
    }

    protected string? AskFileName(string? defaultFileName)
    {
        var savePicker = new Microsoft.Win32.SaveFileDialog()
        {
            DefaultExt = "txt",
            FileName = defaultFileName,
            Filter = "Log files (*.txt)|*.txt"
        };

        if (savePicker.ShowDialog() ?? false)
        {
            _folder = Path.GetDirectoryName(savePicker.FileName) ?? "";
            Properties.Settings.Default.DataFolder = _folder;
            Properties.Settings.Default.Save();

            return savePicker.FileName;
        }

        return null;
    }

    protected bool Save(string? filename, IEnumerable<object> records, string header = "")
    {
        if (string.IsNullOrEmpty(filename))
        {
            return false;
        }

        var folder = Path.GetDirectoryName(filename) ?? "";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        using (StreamWriter writer = File.CreateText(filename))
        {
            try
            {
                if (!string.IsNullOrEmpty(header))
                {
                    writer.WriteLine(header);
                }

                writer.WriteLine(string.Join("\n", records));

                MessageBox.Show(
                    $"Data saved into\n'{filename}'",
                    Application.Current.MainWindow.Title + " - Logger",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                var answer = MessageBox.Show(
                    $"Failed to save data into\n'{filename}':\n\n{ex.Message}\n\n" +
                    "Retry?",
                    Application.Current.MainWindow.Title + " - Logger",
                    MessageBoxButton.YesNo);
                if (answer == MessageBoxResult.Yes)
                {
                    return Save(AskFileName(filename), records, header);
                }
            }
        }

        return false;
    }
}
