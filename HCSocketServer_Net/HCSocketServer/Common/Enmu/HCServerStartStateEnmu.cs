using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Common.Enmu
{
    /// <summary>
    /// 服务器启动状态枚举
    /// </summary>
    public enum HCServerStartStateEnmu
    {
        /// <summary>
        /// 启动成功
        /// </summary>
        Success,
        /// <summary>
        /// 启动失败
        /// </summary>
        Failed,
        /// <summary>
        /// 运行异常
        /// </summary>
        RunningException,
    }
}
