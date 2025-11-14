using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Messenger.Shared; // Убедись, что using правильный
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WpfMessenger.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;

        private string _currentMessageText;
        private string _sendButtonContent = "Отправить";
        private bool _isEditing;
        private MessageModel _editingMessage; // Сообщение, которое редактируется

        // --- Свойства для привязки к интерфейсу ---
        public string CurrentMessageText
        {
            get => _currentMessageText;
            set { _currentMessageText = value; OnPropertyChanged(); }
        }

        public string SendButtonContent
        {
            get => _sendButtonContent;
            set { _sendButtonContent = value; OnPropertyChanged(); }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MessageModel> Messages { get; } = new ObservableCollection<MessageModel>();
        public ObservableCollection<UserModel> OnlineUsers { get; } = new ObservableCollection<UserModel>();
        public UserModel CurrentUser { get; set; }

        // --- Команды ---
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
            // Эта часть пока остается простой, без обработки команд редактирования от сервера
            while (true)
            {
                try
                {
                    var jsonPacket = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonPacket)) continue;

                    var packet = JsonConvert.DeserializeObject<Packet>(jsonPacket);

                    if (packet.Command == "NewMessage")
                    {
                        var message = JsonConvert.DeserializeObject<MessageModel>(packet.JsonData);
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

        // --- Логика команд ---

        private async Task SendOrUpdateMessageAsync()
        {
            // Здесь будет логика для отправки нового или обновленного сообщения
            var message = new MessageModel
            {
                Author = CurrentUser,
                Text = CurrentMessageText,
                Timestamp = DateTime.Now
            };

            var packet = new Packet
            {
                Command = "NewMessage", // Пока отправляем все как новые
                JsonData = JsonConvert.SerializeObject(message)
            };

            await SendPacketAsync(packet);

            Messages.Add(message); // Добавляем себе локально
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

                var packet = new Packet { Command = "NewMessage", JsonData = JsonConvert.SerializeObject(message) };
                await SendPacketAsync(packet);
                Messages.Add(message);
            }
        }

        private void StartEditMessage(object messageObj)
        {
            if (messageObj is MessageModel message)
            {
                _editingMessage = message;
                CurrentMessageText = message.Text;
                IsEditing = true;
                SendButtonContent = "Сохранить";
            }
        }

        private void CancelEdit(object obj)
        {
            IsEditing = false;
            CurrentMessageText = string.Empty;
            _editingMessage = null;
            SendButtonContent = "Отправить";
        }

        private async Task DeleteMessageAsync(object messageObj)
        {
            // Логика удаления пока будет только локальной для демонстрации
            if (messageObj is MessageModel message)
            {
                MessageBox.Show("Функция удаления сообщения в разработке!");
                // В будущем здесь будет отправка пакета на сервер
                // Application.Current.Dispatcher.Invoke(() => Messages.Remove(message));
            }
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(CurrentMessageText);

        private async Task SendPacketAsync(Packet packet)
        {
            try
            {
                string jsonPacket = JsonConvert.SerializeObject(packet);
                await _writer.WriteLineAsync(jsonPacket);
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}