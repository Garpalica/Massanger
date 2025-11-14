// WpfMessenger/Services/NetworkService.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMessenger.Services
{
    public class NetworkService
    {
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;

        public event Action<string> MessageReceived;
        public bool IsConnected => _client?.Connected ?? false;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);

                var stream = _client.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private async Task ListenForMessages()
        {
            try
            {
                while (IsConnected)
                {
                    var jsonPacket = await _reader.ReadLineAsync();
                    if (jsonPacket == null) break;
                    MessageReceived?.Invoke(jsonPacket);
                }
            }
            catch (IOException)
            {
                // Соединение было принудительно разорвано, это нормально при закрытии.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Соединение потеряно: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public async Task SendMessageAsync(string jsonPacket)
        {
            if (IsConnected)
            {
                await _writer.WriteLineAsync(jsonPacket);
            }
        }

        public void Disconnect()
        {
            _client?.Close();
        }
    }
}