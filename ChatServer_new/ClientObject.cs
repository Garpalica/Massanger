
using Messenger.Shared; 
using Newtonsoft.Json;         
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ClientObject
    {
        public Guid Id { get; } = Guid.NewGuid();
        public StreamWriter Writer { get; }
        public StreamReader Reader { get; }

        private readonly TcpClient _client;
        private readonly ServerObject _server;

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            _client = tcpClient;
            _server = serverObject;
            var stream = _client.GetStream();
            Reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            Writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
        }

        public async Task ProcessAsync()
        {
            try
            {
                var username = Guid.NewGuid().ToString().Substring(0, 5);
                Console.WriteLine($"Клиент {username} ({Id}) подключился.");

                while (true)
                {
                    var jsonPacket = await Reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonPacket)) break;

                    // Мы все еще десериализуем пакет, чтобы можно было логировать команду
                    var packet = JsonConvert.DeserializeObject<Packet>(jsonPacket, _jsonSettings);
                    if (packet == null) continue;

                    Console.WriteLine($"Получена команда '{packet.Command}' от {username}");

                    // ✔️ ВЫЗЫВАЕМ УПРОЩЕННЫЙ МЕТОД РАССЫЛКИ
                    await _server.BroadcastPacketAsync(jsonPacket);
                }
            }
            catch (IOException) { Console.WriteLine($"Клиент {Id} отключился (соединение разорвано)."); }
            catch (Exception e) { Console.WriteLine($"Ошибка с клиентом {Id}: {e.Message}"); }
            finally
            {
                _server.RemoveConnection(this.Id);
                Close();
            }
        }
        public void Close()
        {
            try
            {
                Writer?.Close();
                Reader?.Close();
                _client?.Close();
            }
            catch { /* Игнорируем ошибки при закрытии */ }
        }
    }
}