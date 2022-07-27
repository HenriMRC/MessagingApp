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

        private List<Handler> _clientsList = new List<Handler>();
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

            Handler handler = new Handler(_clientsCount, client.GetStream(), OnMessageRead, OnConnectionLost);
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
            Handler[] clients;
            lock (_clientsListLock)
            {
                clients = _clientsList.ToArray();
            }

            for (int i = 0; i < clients.Length; i++)
            {
                Handler handler = clients[i];
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

        internal class Handler
        {
            internal int ID => _clientID;
            private readonly int _clientID;

            private readonly NetworkStream _stream;

            private readonly Action<int, string> _onReadCallback;
            private readonly Action<int, Exception> _onConnectionLost;

            private readonly Queue<byte[]> _writeQueue;
            private readonly object _writeQueueLock;
            private bool _writing;

            internal Handler(int clientID, NetworkStream stream, Action<int, string> onReadCallback, Action<int, Exception> onConnectionLost)
            {
                _clientID = clientID;
                _stream = stream;

                _onReadCallback = onReadCallback;
                _onConnectionLost = onConnectionLost;

                _writeQueue = new Queue<byte[]>();
                _writeQueueLock = new object();

                _writing = false;
            }

            internal void Read()
            {
                byte[] buffer = new byte[sizeof(int)];
                _stream.BeginRead(buffer, 0, buffer.Length, MessageSizeCallback, buffer);
            }

            private void MessageSizeCallback(IAsyncResult result)
            {
                try
                {
                    _stream.EndRead(result);
                    byte[] lenghtBuffer = result.AsyncState as byte[];
                    int length = BitConverter.ToInt32(lenghtBuffer, 0);

                    byte[] buffer = new byte[length];
                    _stream.BeginRead(buffer, 0, buffer.Length, ReadMessageCallback, buffer);
                }
                catch (IOException exception)
                {
                    _onConnectionLost.Invoke(_clientID, exception);
                }
            }

            private void ReadMessageCallback(IAsyncResult result)
            {
                try
                {
                    int length = _stream.EndRead(result);

                    byte[] buffer = result.AsyncState as byte[];
                    string message = Encoder.GetString(buffer, 0, buffer.Length);

                    _onReadCallback.Invoke(_clientID, message);

                    Read();
                }
                catch (IOException exception)
                {
                    _onConnectionLost.Invoke(_clientID, exception);
                }
            }

            internal void Write(int senderID, string message)
            {
                byte[] messageBytes = Encoder.GetBytes(message);
                List<byte> buffer = new List<byte>(2 * sizeof(int) + messageBytes.Length);

                buffer.AddRange(BitConverter.GetBytes(messageBytes.Length));
                buffer.AddRange(BitConverter.GetBytes(senderID));
                buffer.AddRange(messageBytes);

                lock (_writeQueueLock)
                {
                    if (_writing)
                    {
                        _writeQueue.Enqueue(buffer.ToArray());

                    }
                    else
                    {
                        _writing = true;
                        _stream.BeginWrite(buffer.ToArray(), 0, buffer.Count, WriteMessageCallbac, buffer);
                    }
                }
            }

            private void WriteMessageCallbac(IAsyncResult result)
            {
                try
                {
                    _stream.EndWrite(result);
                }
                catch (IOException exception)
                {
                    _onConnectionLost.Invoke(_clientID, exception);
                }
                finally
                {
                    lock (_writeQueueLock)
                    {

                        if (_writeQueue.Count > 0)
                        {
                            byte[] buffer = _writeQueue.Dequeue();
                            _stream.BeginWrite(buffer, 0, buffer.Length, WriteMessageCallbac, buffer);
                        }
                        else
                            _writing = false;
                    }
                }
            }
        }
    }
}
