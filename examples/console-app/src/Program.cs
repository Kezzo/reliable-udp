using System;
using System.Threading;
using System.Threading.Tasks;
using ReliableUdp;
using ReliableUdp.MessageFactory;

namespace ConsoleExampleApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"ReliableUdp console example app started! UserDomainName: { Environment.UserDomainName }");

            if(!int.TryParse(Environment.GetEnvironmentVariable("BIND-PORT"), out int bindPort))
            {
                Console.WriteLine($"ERROR could not parse env var 'BIND-PORT'");
                Environment.Exit(1);
                return;
            }

            var targetHost = Environment.GetEnvironmentVariable("TARGET-HOST");
            if(string.IsNullOrEmpty(targetHost))
            {
                Console.WriteLine($"ERROR could not parse env var 'HOST'");
                Environment.Exit(1);
                return;
            }

            if(!int.TryParse(Environment.GetEnvironmentVariable("TARGET-PORT"), out int targetPort))
            {
                Console.WriteLine($"ERROR could not parse env var 'TARGET-PORT'");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"Start communication to {targetHost}:{ targetPort }");

            // set up
            ExampleUdpClient udpClient = new ExampleUdpClient(bindPort, targetHost, targetPort);
            ReliableUdpHub hub = new ReliableUdpHub(udpClient);
            hub.RegisterMessageFactory<HelloMessage>(0, new MessageFactory<HelloMessage>());

            // make sure other console app started
            Thread.Sleep(1000);

            var msgToSend = new HelloMessage {
                MessageText = $"Hello from example console app: { Environment.UserDomainName }"
            };

            while(true)
            {
                var recvMsgs = await hub.GetReceivedMessages();

                foreach (var recvMsg in recvMsgs)
                {
                    if(recvMsg is HelloMessage)
                    {
                        var helloMessage = recvMsg as HelloMessage;
                        Console.WriteLine("Received message: " + helloMessage.MessageText);
                    }
                }

                hub.QueueMessage(msgToSend);

                await hub.SendQueuedMessages();
                Console.WriteLine($"Sent message: { msgToSend.MessageText }");

                Thread.Sleep(500);
            }
        }
    }
}