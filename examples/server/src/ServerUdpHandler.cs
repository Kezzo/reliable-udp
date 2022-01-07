using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ReliableUdp;
using ReliableUdp.MessageFactory;

namespace ServerExample
{
    public class ServerUdpHandler
    {
        private UdpClient udpClient;
        private ConcurrentDictionary<IPEndPoint, UserSession> userSessions;

        private class UserSession
        {
            public UserSessionUdpClient UdpClient;
            public ReliableUdpHub ReliableHub;
        }

        public ServerUdpHandler(int listenPort)
        {
            udpClient = new UdpClient(listenPort);
            udpClient.BeginReceive(OnDatagramReceived, null);

            userSessions = new ConcurrentDictionary<IPEndPoint, UserSession>();
        }

        private void OnDatagramReceived(IAsyncResult result)
        {
            IPEndPoint endpoint = null;
            byte[] bytes = udpClient.EndReceive(result, ref endpoint);

            Console.WriteLine($"Received datagram from: { endpoint }");

            if(userSessions.TryGetValue(endpoint, out UserSession existingSession))
            {
                existingSession.UdpClient.AddReceivedDatagram(bytes);
            }
            else
            {
                UserSessionUdpClient client = new UserSessionUdpClient(endpoint, SendDatagram);
                ReliableUdpHub reliableHub = new ReliableUdpHub(client);
                reliableHub.RegisterMessageFactory<HelloMessage>(0, new MessageFactory<HelloMessage>());

                client.AddReceivedDatagram(bytes);
                userSessions.TryAdd(endpoint, new UserSession{
                    UdpClient = client,
                    ReliableHub = reliableHub
                });
            }

            udpClient.BeginReceive(OnDatagramReceived, null);
        }

        private Task<int> SendDatagram(IPEndPoint endpoint, byte[] payload)
        {
            return udpClient.SendAsync(payload, payload.Length, endpoint);
        }

        public void SendToAll(List<HelloMessage> messages)
        {
            foreach (var session in userSessions.Values)
            {
                foreach (var msg in messages)
                {
                    session.ReliableHub.QueueMessage(msg);
                }
                
                session.ReliableHub.SendQueuedMessages();
            }
        }

        public async Task<List<HelloMessage>> ReceiveMessages()
        {
            List<HelloMessage> messages = new List<HelloMessage>();
            HelloMessage HelloMessage = null;

            foreach (var session in userSessions.Values)
            {
                foreach (var msg in await session.ReliableHub.GetReceivedMessages())
                {
                    HelloMessage = msg as HelloMessage;
                    if(HelloMessage != null)
                    {
                        messages.Add(HelloMessage);
                    }
                } 
            }

            return messages;
        }
    }
}