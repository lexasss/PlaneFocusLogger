using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EMirrorsScores;

[ValueConversion(typeof(object), typeof(int))]
public class ObjectPresenceToBorderWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((string)value)?.Length > 0 ? 2 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SEClient.Options.Load(SE_CLIENT_OPTIONS_FILENAME);

        _connectPage.Connected += (s, e) =>
        {
            _mainPage = new(e);
            Content = _mainPage;
        };

        Content = _connectPage;
    }


    // Internal

    const string SE_CLIENT_OPTIONS_FILENAME = "se_client_options.json";

    readonly Connect _connectPage = new();
    
    Main? _mainPage;


    // UI handlers

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _mainPage?.Finalize();
        _mainPage?.Dispose();
    }
}
