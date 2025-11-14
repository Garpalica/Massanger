using Massanger.Commands;
using Massanger.Services;
using Messenger.Shared;
using Messenger.Shared.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Massanger.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NetworkService _networkService;
        private string _currentMessageText;
        private MessageModel _messageToEdit;

        public UserModel CurrentUser { get; set; }
        public ObservableCollection<MessageModel> Messages { get; } = new ObservableCollection<MessageModel>();
        public ObservableCollection<UserStatus> OnlineUsers { get; } = new ObservableCollection<UserStatus>();

        // --- Свойства для управления интерфейсом ---
        public bool IsEditing => _messageToEdit != null;
        public string SendButtonContent => IsEditing ? "Сохранить" : "➤";

        public string CurrentMessageText
        {
            get => _currentMessageText;
            set
            {
                if (_currentMessageText == value) return;
                _currentMessageText = value;
                OnPropertyChanged();

                // Отправляем статусы "печатает" / "не печатает"
                var statusCommand = string.IsNullOrEmpty(_currentMessageText) ? CommandType.UserStoppedTyping : CommandType.UserIsTyping;
                var packet = new NetworkPacket { Command = statusCommand };
                _networkService.SendMessageAsync(JsonSerializer.Serialize(packet));
            }
        }

        // --- Команды ---
        public ICommand SendMessageCommand { get; }
        public ICommand AttachFileCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand InsertEmojiCommand { get; }
        public ICommand StartEditMessageCommand { get; }
        public ICommand CancelEditCommand { get; }

        public MainViewModel()
        {
            _networkService = new NetworkService();
            _networkService.MessageReceived += OnMessageReceived;

            // TODO: Заменить на реальную логику входа пользователя
            CurrentUser = new UserModel { Username = $"User{new Random().Next(100, 999)}" };

            SendMessageCommand = new RelayCommand(SendMessage, (p) => !string.IsNullOrWhiteSpace(CurrentMessageText));
            AttachFileCommand = new RelayCommand(AttachFile);
            DeleteMessageCommand = new RelayCommand(DeleteMessage);
            InsertEmojiCommand = new RelayCommand((p) => CurrentMessageText += p?.ToString());
            StartEditMessageCommand = new RelayCommand(StartEditMessage);
            CancelEditCommand = new RelayCommand(CancelEdit);

            ConnectToServerAsync();
        }

        private async void ConnectToServerAsync()
        {
            await _networkService.ConnectAsync("127.0.0.1", 8888);
            if (_networkService.IsConnected)
            {
                // Отправляем свое имя на сервер при подключении
                var packet = new NetworkPacket
                {
                    Command = CommandType.SetUsername,
                    Payload = CurrentUser.Username
                };
                await _networkService.SendMessageAsync(JsonSerializer.Serialize(packet));
            }
        }

        private async void SendMessage(object parameter)
        {
            if (string.IsNullOrWhiteSpace(CurrentMessageText)) return;

            NetworkPacket packet;

            if (IsEditing) // РЕЖИМ РЕДАКТИРОВАНИЯ
            {
                _messageToEdit.Text = CurrentMessageText;
                packet = new NetworkPacket { Command = CommandType.EditMessage, Payload = JsonSerializer.Serialize(_messageToEdit) };
            }
            else // РЕЖИМ ОТПРАВКИ НОВОГО СООБЩЕНИЯ
            {
                var message = new MessageModel
                {
                    Author = CurrentUser,
                    Text = CurrentMessageText,
                    Timestamp = DateTime.Now
                };
                packet = new NetworkPacket { Command = CommandType.NewMessage, Payload = JsonSerializer.Serialize(message) };
            }

            await _networkService.SendMessageAsync(JsonSerializer.Serialize(packet));

            // Сбрасываем поле ввода и выходим из режима редактирования
            CancelEdit(null);
        }

        private async void AttachFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Images|*.png;*.jpg;*.jpeg;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var fileName = Path.GetFileName(filePath);
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var base64Content = Convert.ToBase64String(fileBytes);

                var message = new MessageModel
                {
                    Id = Guid.NewGuid(),
                    Author = CurrentUser,
                    Timestamp = DateTime.Now,
                    IsFileMessage = true,
                    FileName = fileName,
                    FileContentBase64 = base64Content
                };

                var packet = new NetworkPacket { Command = CommandType.FileMessage, Payload = JsonSerializer.Serialize(message) };
                await _networkService.SendMessageAsync(JsonSerializer.Serialize(packet));
            }
        }

        private async void DeleteMessage(object parameter)
        {
            if (parameter is MessageModel messageToDelete)
            {
                var packet = new NetworkPacket { Command = CommandType.DeleteMessage, Payload = JsonSerializer.Serialize(messageToDelete) };
                await _networkService.SendMessageAsync(JsonSerializer.Serialize(packet));
            }
        }

        private void StartEditMessage(object parameter)
        {
            if (parameter is MessageModel message)
            {
                _messageToEdit = message;
                CurrentMessageText = message.Text;
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(SendButtonContent));
            }
        }

        private void CancelEdit(object parameter)
        {
            _messageToEdit = null;
            CurrentMessageText = string.Empty;
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(SendButtonContent));
        }

        private void OnMessageReceived(string jsonPacket)
        {
            try
            {
                var packet = JsonSerializer.Deserialize<NetworkPacket>(jsonPacket);
                if (packet == null) return;

                App.Current.Dispatcher.Invoke(() =>
                {
                    switch (packet.Command)
                    {
                        case CommandType.NewMessage:
                        case CommandType.FileMessage:
                            var newMessage = JsonSerializer.Deserialize<MessageModel>(packet.Payload);
                            if (newMessage != null) Messages.Add(newMessage);
                            break;

                        case CommandType.DeleteMessage:
                            var messageToDelete = JsonSerializer.Deserialize<MessageModel>(packet.Payload);
                            var foundMessage = Messages.FirstOrDefault(m => m.Id == messageToDelete.Id);
                            if (foundMessage != null) Messages.Remove(foundMessage);
                            break;

                        case CommandType.EditMessage:
                            var editedMessage = JsonSerializer.Deserialize<MessageModel>(packet.Payload);
                            var messageToUpdate = Messages.FirstOrDefault(m => m.Id == editedMessage.Id);
                            if (messageToUpdate != null)
                            {
                                messageToUpdate.Text = editedMessage.Text; // Свойство Text само вызовет OnPropertyChanged
                            }
                            break;

                        case CommandType.UpdateUserList:
                            var users = JsonSerializer.Deserialize<List<UserStatus>>(packet.Payload);
                            OnlineUsers.Clear();
                            if (users != null)
                            {
                                foreach (var user in users)
                                {
                                    // Не добавляем себя в список онлайн-пользователей
                                    if (user.Username != CurrentUser.Username)
                                    {
                                        OnlineUsers.Add(user);
                                    }
                                }
                            }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                // Обработка ошибки десериализации
                Console.WriteLine($"Error processing received message: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}