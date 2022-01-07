using System;
using System.Threading.Tasks;

namespace ServerExample
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting example reliable udp server");

            if(!int.TryParse(Environment.GetEnvironmentVariable("BIND-PORT"), out int bindPort))
            {
                Console.WriteLine($"ERROR could not parse env var 'BIND-PORT'");
                Environment.Exit(1);
                return;
            }

            ServerUdpHandler serverUdpHandler = new ServerUdpHandler(bindPort);

            while(true)
            {
                var recvMsgs = await serverUdpHandler.ReceiveMessages();
                serverUdpHandler.SendToAll(recvMsgs);
            }
        }
    }
}

