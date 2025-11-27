
namespace Messenger.Shared
{
    public class Packet
    {
        public string Command { get; set; }

        //используем object Data.
        // Теперь сюда можно положить сам объект, а не его JSON-представление.
        public object Data { get; set; }
    }
}