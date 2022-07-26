using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MessagingApp
{
    internal class ClientHandler
    {
        internal int ID => _clientID;
        private readonly int _clientID;

        private readonly NetworkStream _stream;

        private readonly Action<int, string> _onReadCallback;
        private readonly Action<int, Exception> _onConnectionLost;

        private readonly Queue<byte[]> _writeQueue;
        private readonly object _writeQueueLock;
        private bool _writing;

        internal ClientHandler(int clientID, NetworkStream stream, Action<int, string> onReadCallback, Action<int, Exception> onConnectionLost)
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
                string message = Encoding.ASCII.GetString(buffer, 0, buffer.Length);

                _onReadCallback.Invoke(_clientID, message);

                Read();
            }
            catch (IOException exception)
            {
                _onConnectionLost.Invoke(_clientID, exception);
            }
        }

        internal void Write(int senderID, string messageSize)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(messageSize);
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
                    _stream.BeginWrite(buffer.ToArray(), 0, buffer.Count, WriteMessageCallbac, buffer);
                    _writing = true;
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
