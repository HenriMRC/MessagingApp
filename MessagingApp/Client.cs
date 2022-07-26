using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingApp
{
    internal class Client : NetworkBase
    {
        internal override void Start(Action<int, string> onReceiveMessage)
        {
            base.Start(onReceiveMessage);
        }

        internal override void SendMessage(string message)
        {
            throw new NotImplementedException();
        }
    }
}
