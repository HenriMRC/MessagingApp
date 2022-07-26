using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingApp
{
    internal abstract class NetworkBase
    {
        private Action<int, string> _onReceiveMessage;

        protected void OnMessageReceived(int senderID, string message)
        {
            _onReceiveMessage?.Invoke(senderID, message);
        }

        internal virtual void Start(Action<int, string> onReceiveMessage)
        {
            _onReceiveMessage = onReceiveMessage;
        }

        internal abstract void SendMessage(string message);
    }
}
