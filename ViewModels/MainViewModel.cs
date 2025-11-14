// Файл: WpfMessenger/ViewModels/MainViewModel.cs

using Messenger.Shared;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMessenger.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ✔️ НОВОЕ: Настройки для сериализации с информацией о типах
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;

        private string _currentMessageText;
        private string _sendButtonContent = "Отправить";
        private bool _isEditing;
        private MessageModel _editingMessage;

        public string CurrentMessageText { get => _currentMessageText; set { _currentMessageText = value; OnPropertyChanged(); } }
        public string SendButtonContent { get => _sendButtonContent; set { _sendButtonContent = value; OnPropertyChanged(); } }
        public bool IsEditing { get => _isEditing; set { _isEditing = value; OnPropertyChanged(); } }
        public ObservableCollection<MessageModel> Messages { get; } = new ObservableCollection<MessageModel>();
        public ObservableCollection<UserModel> OnlineUsers { get; } = new ObservableCollection<UserModel>();
        public UserModel CurrentUser { get; set; }

        public RelayCommand SendMessageCommand { get; }
        public RelayCommand AttachFileCommand { get; }
        public RelayCommand StartEditMessageCommand { get; }
        public RelayCommand CancelEditCommand { get; }
        public RelayCommand DeleteMessageCommand { get; }
        public RelayCommand InsertEmojiCommand { get; }

        public MainViewModel()
        {
            CurrentUser = new UserModel { Username = $"User{new Random().Next(100, 999)}" };
            SendMessageCommand = new RelayCommand(async _ => await SendOrUpdateMessageAsync(), _ => CanSendMessage());
            AttachFileCommand = new RelayCommand(async _ => await AttachFileAsync());
            StartEditMessageCommand = new RelayCommand(StartEditMessage);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteMessageCommand = new RelayCommand(async _ => await DeleteMessageAsync(_));
            InsertEmojiCommand = new RelayCommand(param => CurrentMessageText += param?.ToString());
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 8888);
                var stream = _client.GetStream();
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream);
                Task.Run(ListenForMessages);
            }
            catch (Exception e) { MessageBox.Show($"Ошибка подключения: {e.Message}"); }
        }

        private async Task ListenForMessages()
        {
            while (true)
            {
                try
                {
                    var jsonPacket = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonPacket)) continue;

                    // ✔️ ИЗМЕНЕНИЕ: Десериализуем с новыми настройками
                    var packet = JsonConvert.DeserializeObject<Packet>(jsonPacket, _jsonSettings);

                    if (packet.Command == "NewMessage" && packet.Data is MessageModel message)
                    {
                        // Теперь не нужно ничего дополнительно распаковывать!
                        // Объект message уже готов к использованию.
                        Application.Current.Dispatcher.Invoke(() => Messages.Add(message));
                    }
                }
                catch (Exception e)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Связь потеряна: {e.Message}"));
                    break;
                }
            }
        }

        private async Task SendOrUpdateMessageAsync()
        {
            var message = new MessageModel
            {
                Author = CurrentUser,
                Text = CurrentMessageText,
                Timestamp = DateTime.Now
            };

            var packet = new Packet
            {
                Command = "NewMessage",
                // ✔️ ИЗМЕНЕНИЕ: Просто кладём объект в поле Data
                Data = message
            };

            await SendPacketAsync(packet);
            CurrentMessageText = string.Empty;
        }

        private async Task AttachFileAsync()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var base64Content = Convert.ToBase64String(fileBytes);

                var message = new MessageModel
                {
                    Author = CurrentUser,
                    Timestamp = DateTime.Now,
                    FileName = Path.GetFileName(filePath),
                    FileContentBase64 = base64Content
                };

                var packet = new Packet
                {
                    Command = "NewMessage",
                    // ✔️ ИЗМЕНЕНИЕ: Просто кладём объект в поле Data
                    Data = message
                };
                await SendPacketAsync(packet);
            }
        }

        private async Task SendPacketAsync(Packet packet)
        {
            try
            {
                // ✔️ ИЗМЕНЕНИЕ: Сериализуем с новыми настройками
                string jsonPacket = JsonConvert.SerializeObject(packet, _jsonSettings);
                await _writer.WriteLineAsync(jsonPacket);
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        // Остальной код (редактирование, удаление и т.д.) без изменений
        private void StartEditMessage(object messageObj) { /* ... */ }
        private void CancelEdit(object obj) { /* ... */ }
        private async Task DeleteMessageAsync(object messageObj) { /* ... */ }
        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(CurrentMessageText);
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    }
}