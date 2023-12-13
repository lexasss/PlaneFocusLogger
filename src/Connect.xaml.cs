using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlaneFocusLogger;

public partial class Connect : Page
{
    public event EventHandler<SEClient.Tcp.Client?>? Connected;

    public Connect()
    {
        InitializeComponent();

        var settings = Properties.Settings.Default;
        txbHost.Text = settings.Host;
        txbPort.Text = settings.Port;

        BindUIControls();
    }

    // Internal

    private void BindUIControls()
    {
        var seClientOptions = SEClient.Options.Instance;

        Utils.UIHelper.InitComboBox(cmbSource, seClientOptions.IntersectionSource, (value) =>
        {
            seClientOptions.IntersectionSource = value;
        });
        Utils.UIHelper.InitCheckBox(chkSourceFiltered, seClientOptions.IntersectionSourceFiltered, (value) =>
        {
            seClientOptions.IntersectionSourceFiltered = value;
        });
    }

    // UI events

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        window.KeyDown += Page_KeyDown;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        var settings = Properties.Settings.Default;
        settings.Host = txbHost.Text;
        settings.Port = txbPort.Text;
        settings.Save();
    }

    private void Page_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            App.IsDebugging = true;
            Connect_Click(this, new RoutedEventArgs());
        }
    }

    private async void Connect_Click(object? _, RoutedEventArgs e)
    {
        if (App.IsDebugging)
        {
            Connected?.Invoke(this, null);
            return;
        }

        if (txbHost.Text.Split('.').Where(p => byte.TryParse(p, out byte _)).Count() != 4)
        {
            MessageBox.Show("IP is not valid", Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!ushort.TryParse(txbPort.Text, out ushort value) || value < 1024)
        {
            MessageBox.Show("Port is not valid", Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        btnConnect.IsEnabled = false;

        var tcpClient = new SEClient.Tcp.Client();
        await tcpClient.Connect(txbHost.Text, int.Parse(txbPort.Text), App.IsDebugging);

        if (tcpClient.IsConnected)
        {
            Connected?.Invoke(this, tcpClient);
        }
        else
        {
            MessageBox.Show("Cannot connect to SmartEye", Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        btnConnect.IsEnabled = true;
    }
}
