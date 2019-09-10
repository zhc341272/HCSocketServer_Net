using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Common.Enmu
{
    /// <summary>
    /// 客户端启动状态枚举
    /// </summary>
    public enum HCClientStateEnmu
    {
        /// <summary>
        /// 客户端连接成功
        /// </summary>
        Connected,
        /// <summary>
        /// 客户端连接失败
        /// </summary>
        Failed,
        /// <summary>
        /// 客户端断开连接
        /// </summary>
        Disconnected,
        /// <summary>
        /// 运行异常
        /// </summary>
        RunningException,
    }
}
