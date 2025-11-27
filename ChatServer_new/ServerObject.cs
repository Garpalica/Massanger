
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ServerObject
    {
        private TcpListener _tcpListener;
        private readonly List<ClientObject> _clients = new List<ClientObject>();

        protected internal void AddConnection(ClientObject clientObject)
        {
            _clients.Add(clientObject);
        }

        protected internal void RemoveConnection(Guid id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                _clients.Remove(client);
                Console.WriteLine($"Клиент {id} удален из списка.");
            }
        }

        public async Task ListenAsync()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, 8888);
                _tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    var clientObject = new ClientObject(tcpClient, this);
                    AddConnection(clientObject);
                    Task.Run(clientObject.ProcessAsync);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }


        protected internal async Task BroadcastPacketAsync(string jsonPacket)
        {
            Console.WriteLine($"Рассылка пакета всем клиентам...");
            var disconnectedClients = new List<ClientObject>();

            // Просто проходим по всем клиентам и отправляем им пакет
            foreach (var client in _clients.ToList())
            {
                try
                {
                    await client.Writer.WriteLineAsync(jsonPacket);
                    await client.Writer.FlushAsync();
                }
                catch
                {
                    // Если отправка не удалась, клиент скорее всего отключился
                    disconnectedClients.Add(client);
                }
            }

            // Удаляем "мертвых" клиентов
            foreach (var client in disconnectedClients)
            {
                RemoveConnection(client.Id);
            }
        }

        protected internal void Disconnect()
        {
            foreach (var client in _clients) client.Close();
            _tcpListener?.Stop();
            Environment.Exit(0);
        }
    }
}