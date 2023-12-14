using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMirrorsScores.Logging;

public class Server : IDisposable
{
    public event EventHandler<string>? Message;

    public Server(int port = 27117)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Task.Run(Listen);
    }

    public void Dispose()
    {
        _listener.Stop();
        GC.SuppressFinalize(this);
    }

    // Internal

    readonly TcpListener _listener;
    readonly CancellationTokenSource _cts = new();

    private async void Listen()
    {
        try
        {
            while (true)
            {
                using TcpClient handler = await _listener.AcceptTcpClientAsync(_cts.Token);
                await using NetworkStream stream = handler.GetStream();

                byte[] buffer = new byte[1024];
                while (handler.Connected)
                {
                    int length = await stream.ReadAsync(buffer, _cts.Token);
                    var msg = Encoding.UTF8.GetString(buffer[0..length]).Trim();
                    Message?.Invoke(this, msg);
                }
            }
        }
        catch (Exception) { }
    }
}
