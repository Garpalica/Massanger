
using Messenger.Shared;
using Messenger.Shared.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMessenger.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;

        private string _currentMessageText;
        private string _sendButtonContent = "Отправить";
        private bool _isEditing;
        private MessageModel _editingMessage; // Храним сообщение, которое редактируем

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
            DeleteMessageCommand = new RelayCommand(async param => await DeleteMessageAsync(param));
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
                _reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                _writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
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

                    var packet = JsonConvert.DeserializeObject<Packet>(jsonPacket, _jsonSettings);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        switch (packet.Command)
                        {
                            case "NewMessage" when packet.Data is MessageModel newMessage:
                                Messages.Add(newMessage);
                                break;

                            case "UpdateMessage" when packet.Data is MessageModel updatedMessage:
                                var messageToUpdate = Messages.FirstOrDefault(m => m.Id == updatedMessage.Id);
                                if (messageToUpdate != null)
                                {
                                    messageToUpdate.Text = updatedMessage.Text;
                                    messageToUpdate.IsEdited = true;
                                }
                                break;

                            case "DeleteMessage" when packet.Data is string messageIdStr:
                                // Пытаемся преобразовать строку обратно в Guid
                                if (Guid.TryParse(messageIdStr, out Guid messageId))
                                {
                                    // Если получилось, ищем сообщение с таким Id
                                    var messageToRemove = Messages.FirstOrDefault(m => m.Id == messageId);
                                    if (messageToRemove != null)
                                    {
                                        // И удаляем его
                                        Messages.Remove(messageToRemove);
                                        Console.WriteLine($"Сообщение {messageId} успешно удалено из интерфейса."); // Для отладки
                                    }
                                }
                                break;
                        }
                    });
                }
                catch (Exception e)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Связь потеряна: {e.Message}"));
                    break;
                }
            }
        }

        // Редактирование и отправка
        private async Task SendOrUpdateMessageAsync()
        {
            if (IsEditing)
            {
                // Логика обновления
                _editingMessage.Text = CurrentMessageText;
                var packet = new Packet { Command = "UpdateMessage", Data = _editingMessage };
                await SendPacketAsync(packet);
                CancelEdit(null); // Сбрасываем режим редактирования
            }
            else
            {
                //отправка нового сообщения
                var message = new MessageModel { Author = CurrentUser, Text = CurrentMessageText, Timestamp = DateTime.Now };
                var packet = new Packet { Command = "NewMessage", Data = message };
                await SendPacketAsync(packet);
                CurrentMessageText = string.Empty;
            }
        }

        //метод удалаения

        private async Task DeleteMessageAsync(object messageObj)
        {
            if (messageObj is MessageModel messageToDelete)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить это сообщение?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // Отправляем команду на сервер для других клиентов
                    var packet = new Packet { Command = "DeleteMessage", Data = messageToDelete.Id };
                    await SendPacketAsync(packet);
                }
            }
        }
        private void StartEditMessage(object messageObj)
        {
            if (messageObj is MessageModel messageToEdit)
            {
                _editingMessage = messageToEdit;
                CurrentMessageText = messageToEdit.Text;
                IsEditing = true;
                SendButtonContent = "Сохранить";
            }
        }

        // отмена редактирования
        private void CancelEdit(object obj)
        {
            IsEditing = false;
            CurrentMessageText = string.Empty;
            _editingMessage = null;
            SendButtonContent = "Отправить";
        }

        private async Task AttachFileAsync()
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var fileBytes = await File.ReadAllBytesAsync(openFileDialog.FileName);
                var base64Content = Convert.ToBase64String(fileBytes);
                var message = new MessageModel
                {
                    Author = CurrentUser,
                    Timestamp = DateTime.Now,
                    FileName = Path.GetFileName(openFileDialog.FileName),
                    FileContentBase64 = base64Content
                };
                var packet = new Packet { Command = "NewMessage", Data = message };
                await SendPacketAsync(packet);
            }
        }

        private async Task SendPacketAsync(Packet packet)
        {
            try
            {
                string jsonPacket = JsonConvert.SerializeObject(packet, _jsonSettings);
                await _writer.WriteLineAsync(jsonPacket);
                await _writer.FlushAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка отправки: {ex.Message}"); }
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(CurrentMessageText);
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    }
}