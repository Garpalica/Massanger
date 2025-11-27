
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Messenger.Shared.Models
{
    public class MessageModel : INotifyPropertyChanged
    {
        private string _text;
        private bool _isEdited;

        public Guid Id { get; set; } = Guid.NewGuid();
        public UserModel Author { get; set; }
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; }
        public string FileContentBase64 { get; set; }
        public bool IsFileMessage => !string.IsNullOrEmpty(FileName);
        //rrrrarpapa
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public bool IsEdited
        {
            get => _isEdited;
            set { _isEdited = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}