using PlaneFocusLogger.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlaneFocusLogger;

public partial class Main : Page, IDisposable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsDriverBusyWithTask
    {
        get => _isDriverBusyWithTask;
        set
        {
            _isDriverBusyWithTask = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDriverBusyWithTask)));
        }
    }

    public Main(SEClient.Tcp.Client? tcpClient)
    {
        InitializeComponent();

        DataContext = this;

        _tcpClient = tcpClient;

        if (_tcpClient != null)
        {
            _tcpClient.Disconnected += DataSource_Closed;
            _tcpClient.Sample += TcpClient_Sample;
        }

        ScreenLogger.Initialize(txbOutput, wrpScreenLogger);

        _handler = new PlaneIntersectionHander(new Dictionary<Panel, Plane.Plane>()
        {
            { stpWindshield, new Plane.Plane("Windshield") },
            { stpLeftMirror, new Plane.Plane("LeftMirror") },
            { stpLeftDashboard, new Plane.Plane("LeftDashboard") },
            { stpRearView, new Plane.Plane("RearView") },
            { stpCentralConsole, new Plane.Plane("CentralConsole") },
            { stpRightMirror, new Plane.Plane("RightMirror") },
        });
    }

    public void Finalize()
    {
        SaveLoggedData();

        if (_tcpClient?.IsConnected ?? false)
        {
            _tcpClient.Stop();
        }
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    // Internal

    readonly SEClient.Tcp.Client? _tcpClient;
    readonly PlaneIntersectionHander _handler;

    readonly FlowLogger _logger = FlowLogger.Instance;
    readonly Statistics _statistics = Statistics.Instance;

    bool _isDriverBusyWithTask = false;

    private void SaveLoggedData()
    {
        if (_logger.HasRecords)
        {
            _logger.IsEnabled = false;
            var timestamp = $"{DateTime.Now:u}";
            if (_logger.SaveTo($"richa_{timestamp}.txt".ToPath()) == SavingResult.Save)
            {
                _statistics.SaveTo($"richa_{timestamp}_stat.txt".ToPath());
            }
        }
    }

    // Handlers

    private void DataSource_Closed(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher.Invoke(SaveLoggedData);
        }
        catch (TaskCanceledException) { }
    }

    private void TcpClient_Sample(object? sender, SEClient.Tcp.Data.Sample sample)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                _handler.Feed(sample);
            });
        }
        catch (TaskCanceledException) { }
    }

    // UI

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugging)
        {
            lblDebug.Visibility = Visibility.Visible;
        }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        _handler.Reset();
        Finalize();
        Application.Current.Shutdown();
    }
}
