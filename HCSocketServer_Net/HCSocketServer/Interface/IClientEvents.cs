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
    public delegate void HCClientDataStateDelegate(HCDataStateEnmu state, HCMessage message);
    public delegate void HCClientStateDelegate(HCClientStateEnmu state, HCClient client);

    interface IClientEvents
    {
        /// <summary>
        /// 客户端数据操作的状态
        /// </summary>
        event HCClientDataStateDelegate ClientDataState;
        /// <summary>
        /// 客户端运行的状态
        /// </summary>
        event HCClientStateDelegate ClientState;
    }
}
