
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new ServerObject();
            await server.ListenAsync();
        }
    }
}