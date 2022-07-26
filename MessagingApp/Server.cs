using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagingApp
{
    internal class Server : NetworkBase
    {
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        private List<ClientHandler> _clientsList = new List<ClientHandler>();
        private object _clientsListLock = new object();
        private int _clientsCount = 0;

        private const int PORT = 5050;

        internal Server()
        {

        }

        internal override void Start(Action<int, string> onReceiveMessage)
        {
            base.Start(onReceiveMessage);

            /*Console.WriteLine(Environment.MachineName);

            string name = Dns.GetHostName();

            Console.WriteLine(name);

            IPHostEntry host = Dns.GetHostEntry(name);*/

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, PORT);

            /*foreach (IPAddress current in host.AddressList)
            {
                Console.WriteLine($"{current} - {Dns.GetHostEntry(current).HostName} - {current.AddressFamily}");
                if (current.AddressFamily == AddressFamily.InterNetwork)
                {
                    endPoint = new IPEndPoint(current, PORT);
                    break;
                }
            }

            if (endPoint == null)
            {
                Console.WriteLine($"No IP Addres of {AddressFamily.InterNetwork} found.");

                Console.ReadLine();

                return;
            }*/

            TcpListener listener = new TcpListener(endPoint);
            listener.Start();

            Debug.Print($"Listening on {endPoint}.");

            try
            {
                ValueTask<TcpClient> listenTask = listener.AcceptTcpClientAsync(_cancellationSource.Token);
                Task<TcpClient> task = listenTask.AsTask();
                Task genericTask = task.ContinueWith(OnListened, listener);
            }
            catch (Exception exception)
            {
                Debug.Print(exception.Message);
            }
            finally
            {

            }
        }

        private void OnListened(Task<TcpClient> taskTCP, object? state)
        {
            TcpClient client = taskTCP.Result;
            Debug.Print($"Accepted client on R:{client.Client.RemoteEndPoint} | L:{client.Client.LocalEndPoint}. Current count: {_clientsList.Count}. Total count: {++_clientsCount}.");

            ClientHandler handler = new ClientHandler(_clientsCount, client.GetStream(), OnMessageRead, OnConnectionLost);
            handler.Read();
            lock (_clientsListLock)
            {
                _clientsList.Add(handler);
            }

            TcpListener listener = (TcpListener)state;
            ValueTask<TcpClient> listenTask = listener.AcceptTcpClientAsync(_cancellationSource.Token);
            Task<TcpClient> task = listenTask.AsTask();
            Task genericTask = task.ContinueWith(OnListened, listener);
        }

        private void OnMessageRead(int senderID, string message)
        {
            OnMessageReceived(senderID, message);
            BroadcastMessage(senderID, message);
        }

        internal override void SendMessage(string message) => BroadcastMessage(0, message);

        private void BroadcastMessage(int senderID, string message)
        {
            ClientHandler[] clients;
            lock (_clientsListLock)
            {
                clients = _clientsList.ToArray();
            }

            for (int i = 0; i < clients.Length; i++)
            {
                ClientHandler handler = clients[i];
                if (handler.ID != senderID)
                    handler.Write(senderID, message);
            }
        }

        private void OnConnectionLost(int clientID, Exception exception)
        {
            lock (_clientsListLock)
            {

                for (int i = 0; i < _clientsList.Count; i++)
                {
                    if (_clientsList[i].ID == clientID)
                    {
                        _clientsList.RemoveAt(i);
                        break;
                    }
                }
            }

            Debug.Print($"Connection lost with ID {clientID}");
        }
    }
}
