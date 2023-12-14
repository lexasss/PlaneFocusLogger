using EMirrorsScores.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EMirrorsScores;

public partial class Main : Page, IDisposable
{
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

        _server.Message += CommandServer_Message;

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

        foreach (var button in grdQuestionnaire.Children.OfType<Button>())
        {
            button.Click += QuestionnaireAnswer_Click;
        }

        grdQuestionnaire.Visibility = Visibility.Collapsed;
    }

    public async Task Finalize()
    {
        if (_tcpClient?.IsConnected ?? false)
        {
            await _tcpClient.Stop();
        }

        SaveLoggedData();
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    // Internal

    static class CarlaCommands
    {
        public static string Start => "start";
        public static string Stop => "stop";
        public static string Pause => "pause";
        public static string Continue => "continue";
    }

    readonly SEClient.Tcp.Client? _tcpClient;
    readonly PlaneIntersectionHander _handler;

    readonly FlowLogger _logger = FlowLogger.Instance;
    readonly GazeStatistics _statistics = GazeStatistics.Instance;

    readonly Server _server = new Server();

    int _trafficConeCount = 0;
    int _answersCount = 1;

    QuestionnaireStage _questionnaireStage = QuestionnaireStage.NoAnswers;

    enum QuestionnaireStage
    {
        NoAnswers,
        OneAnswer,
        TwoAnswers
    }


    private void SaveLoggedData()
    {
        if (_logger.HasRecords)
        {
            _logger.IsEnabled = false;
            var timestamp = $"{DateTime.Now:u}";
            if (_logger.SaveTo($"emirrors_{timestamp}.txt".ToPath()) == SavingResult.Save)
            {
                _statistics.SaveTo($"emirrors_{timestamp}_gaze.txt".ToPath());
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

    private void CommandServer_Message(object? sender, string e)
    {
        if (e == CarlaCommands.Start)
        {
            _logger.IsEnabled = true;
            GazeStatistics.Instance.IsEnabled = true;
            Dispatcher.Invoke(() => grdControl.IsEnabled = true);
        }
        else if (e == CarlaCommands.Stop)
        {
            _logger.Add(LogSource.Carla, e);
            _logger.IsEnabled = false;
            GazeStatistics.Instance.IsEnabled = false;
            Dispatcher.Invoke(() => Quit_Click(this, new RoutedEventArgs()));
        }
        else if (e.StartsWith(CarlaCommands.Pause))
        {
            GazeStatistics.Instance.IsEnabled = false;
            Dispatcher.Invoke(() => Questionnaire_Click(this, new RoutedEventArgs()));
        }
        else if (e.StartsWith(CarlaCommands.Continue))
        {
            GazeStatistics.Instance.IsEnabled = true;
        }

        _logger.Add(LogSource.Carla, e);
    }

    // UI

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugging)
        {
            lblDebug.Visibility = Visibility.Visible;
        }
    }

    private void QuestionnaireAnswer_Click(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        var id = (string)button.Tag;
        var lane = id[0] == '0' ? "left" : "right";
        var answer = id[1];

        _logger.Add(LogSource.Lane, _answersCount.ToString(), lane, answer.ToString());

        _questionnaireStage = _questionnaireStage switch
        {
            QuestionnaireStage.NoAnswers => QuestionnaireStage.OneAnswer,
            QuestionnaireStage.OneAnswer => QuestionnaireStage.TwoAnswers,
            _ => throw new Exception("Impossible")
        };

        if (_questionnaireStage == QuestionnaireStage.OneAnswer)
        {
            var IsEnabled = (string? tag) => tag?[0] != id[0];
            foreach (var btn in grdQuestionnaire.Children.OfType<Button>())
            {
                if (!IsEnabled(btn.Tag.ToString()))
                {
                    btn.IsEnabled = false;
                }
            }
        }
        if (_questionnaireStage == QuestionnaireStage.TwoAnswers)
        {
            grdQuestionnaire.Visibility = Visibility.Collapsed;
            grdDistractors.Visibility = Visibility.Visible;

            _questionnaireStage = QuestionnaireStage.NoAnswers;
            _answersCount += 1;

            foreach (var btn in grdQuestionnaire.Children.OfType<Button>())
            {
                btn.IsEnabled = true;
            }
        }
    }

    private void TrafficCone_Click(object sender, RoutedEventArgs e)
    {
        _trafficConeCount += 1;
        _logger.Add(LogSource.Distractor, _trafficConeCount.ToString());
        lblTrafficConeCount.Content = _trafficConeCount.ToString();
    }

    private void Questionnaire_Click(object sender, RoutedEventArgs e)
    {
        grdDistractors.Visibility = Visibility.Collapsed;
        grdQuestionnaire.Visibility = Visibility.Visible;
    }

    private async void Quit_Click(object sender, RoutedEventArgs e)
    {
        _handler.Reset();
        await Finalize();
        Application.Current.Shutdown();
    }
}
