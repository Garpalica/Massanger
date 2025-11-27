
namespace Messenger.Shared
{
    public enum CommandType
    {
        // Клиент -> Сервер
        SetUsername,
        NewMessage,
        FileMessage,
        DeleteMessage,
        EditMessage,
        UserIsTyping,
        UserStoppedTyping,

        // Сервер -> Клиент
        MessageReceived,
        MessageDeleted,
        MessageEdited,
        UpdateUserList
    }

    public class NetworkPacket
    {
        public CommandType Command { get; set; }
        public string Payload { get; set; } // Данные в формате JSON
    }
}