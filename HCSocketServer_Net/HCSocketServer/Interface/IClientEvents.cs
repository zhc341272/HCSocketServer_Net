using HCSocketServer.Common.Enmu;
using HCSocketServer.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Interface
{
    public delegate void HCClientDataStateDelegate(HCDataStateEnmu state, HCClient client, HCMessage message);

    interface IClientEvents
    {
        /// <summary>
        /// 服务器中关于客户端数据操作的状态
        /// </summary>
        event HCClientDataStateDelegate ClientDataState;
    }
}
