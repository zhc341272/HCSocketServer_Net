using HCSocketServer.Common.Enmu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Interface
{
    public delegate void HCServerStateDelegate(HCServerStateEnmu state, string msg);

    /// <summary>
    /// 服务器通用事件接口
    /// </summary>
    interface IServerEvents : IClientEvents
    {
        /// <summary>
        /// 服务器运行状态
        /// </summary>
        event HCServerStateDelegate ServerState;
    }
}
