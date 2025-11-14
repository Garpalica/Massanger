// Services/NetworkService.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Messenger.Shared.Models;

namespace Massanger.Services // Убедись, что пространство имен верное
{
    public class NetworkService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        public bool IsConnected => _client?.Connected ?? false;
        public event Action<string> MessageReceived;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);

                // Инициализируем объекты для чтения и записи.
                // Они будут жить, пока жив _client.
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                // Запускаем фоновую задачу, которая будет ПОСТОЯННО слушать сервер
                Task.Run(() => ListenForMessages());

                Console.WriteLine("Клиент успешно подключился к серверу!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        // Этот метод будет работать в фоне и ждать сообщения
        private async Task ListenForMessages()
        {
            try
            {
                while (_client.Connected)
                {
                    var jsonPacket = await _reader.ReadLineAsync();
                    if (jsonPacket == null)
                    {
                        // Сервер разорвал соединение
                        break;
                    }
                    // Уведомляем подписчиков (MainViewModel) о новом сообщении
                    MessageReceived?.Invoke(jsonPacket);
                }
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
            if (_client != null && _client.Connected)
            {
                try
                {
                    await _writer.WriteLineAsync(jsonPacket);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                }
            }
        }

        public void Disconnect()
        {
            _reader?.Close();
            _writer?.Close();
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("Клиент отключен.");
        }
    }
}