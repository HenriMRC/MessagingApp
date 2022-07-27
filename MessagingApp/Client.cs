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
        Handler _handler;

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
                        }
                        finally
                        {
                            Debug.Write($"{ip} ({name}) is up: ({e.Reply.RoundtripTime} ms). ");
                        }

                        try
                        {
                            TcpClient client = new TcpClient(ip, 5050);

                            _handler = new Handler(client.GetStream(), OnMessageRead, OnConnectionLost);
                            _handler.Read();
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

        internal override void SendMessage(string message) => _handler.Write(message);

        private void OnMessageRead(int senderID, string message)
        {
            OnMessageReceived(senderID, message);
        }

        private void OnConnectionLost(Exception exception)
        {
            Debug.Print($"Connection lost with the server.");
        }

        internal class Handler
        {
            private readonly NetworkStream _stream;

            private readonly Action<int, string> _onReadCallback;
            private readonly Action<Exception> _onConnectionLost;

            private readonly Queue<byte[]> _writeQueue;
            private readonly object _writeQueueLock;
            private bool _writing;

            internal Handler(NetworkStream stream, Action<int, string> onReadCallback, Action<Exception> onConnectionLost)
            {
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
                    int length = BitConverter.ToInt32(lenghtBuffer, 0) + sizeof(int);

                    byte[] buffer = new byte[length];
                    _stream.BeginRead(buffer, 0, buffer.Length, ReadMessageCallback, buffer);
                }
                catch (IOException exception)
                {
                    _onConnectionLost.Invoke(exception);
                }
            }

            private void ReadMessageCallback(IAsyncResult result)
            {
                try
                {
                    int length = _stream.EndRead(result);

                    List<byte> buffer = new List<byte>(result.AsyncState as byte[]);
                    List<byte> idBuffer = buffer.GetRange(0, sizeof(int));
                    int id = BitConverter.ToInt32(idBuffer.ToArray(), 0);

                    buffer.RemoveRange(0, sizeof(int));

                    string message = Encoder.GetString(buffer.ToArray(), 0, buffer.Count);

                    _onReadCallback.Invoke(id, message);

                    Read();
                }
                catch (IOException exception)
                {
                    _onConnectionLost.Invoke( exception);
                }
            }

            internal void Write(string message)
            {
                byte[] messageBytes = Encoder.GetBytes(message);
                List<byte> buffer = new List<byte>(sizeof(int) + messageBytes.Length);

                buffer.AddRange(BitConverter.GetBytes(messageBytes.Length));
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
                    _onConnectionLost.Invoke(exception);
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
