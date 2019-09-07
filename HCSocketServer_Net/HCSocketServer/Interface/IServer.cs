using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Interface
{
    interface IServer : IServerEvents
    {
        void Init();
        void Start(IPEndPoint iPEndPoint);
        void CloseClient(HCClient client);
        void SendMsgByClientID(string clientid, byte[] data);
        void SendMsgByClientID(string clientid, string data);
        void SendMsgToAllClient(byte[] data);
        void SendMsgToAllClient(string data);
    }
}
