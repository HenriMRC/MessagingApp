using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagingApp
{
    internal class Client : NetworkBase
    {
        ClientHandler _handler;

        internal override void Start(Action<int, string> onReceiveMessage)
        {
            base.Start(onReceiveMessage);

            string ipBase = GetIPAddress();

            ipBase = ipBase.Substring(0, ipBase.LastIndexOf('.') + 1);

            for (int i = 0; i < 256; i++)
            {
                string ip = ipBase + i.ToString();

                Ping ping = new Ping();
                ping.PingCompleted += new PingCompletedEventHandler(PingCompleted);
                ping.SendAsync(ip, 1000, ip);
            }
        }

        public static string GetIPAddress()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            return null;
        }

        private void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = e.UserState as string;
            if (e.Reply != null)
            {
                switch (e.Reply.Status)
                {
                    case IPStatus.Success:
                        string name = null;
                        try
                        {
                            IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                            name = hostEntry.HostName;
                        }
                        catch (SocketException ex)
                        {
                            name = "?";
                            throw;
                        }
                        finally
                        {
                            Console.WriteLine($"{ip} ({name}) is up: ({e.Reply.RoundtripTime} ms). ");
                        }

                        try
                        {
                            TcpClient client = new TcpClient(ip, 5050);

                            _handler = new ClientHandler(0, client.GetStream(), OnMessageRead, OnConnectionLost);
                        }
                        catch (SocketException exception)
                        {
                            //Console.WriteLine($"{ip} - SocketException");
                        }
                        break;
                    default:
                        //Console.WriteLine($"Pinging {ip} completed with state: {e.Reply.Status}");
                        break;
                }
            }
            else
            {
                //Console.WriteLine($"Pinging {ip} failed. (Null Reply object?)");
            }
        }

        internal override void SendMessage(string message) => _handler.Write(0, message);

        private void OnMessageRead(int senderID, string message)
        {
            OnMessageReceived(senderID, message);
        }

        private void OnConnectionLost(int clientID, Exception exception)
        {
            Debug.Print($"Connection lost with ID {clientID}");
        }
    }
}
