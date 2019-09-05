using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Common.Enmu
{
    /// <summary>
    /// 通用数据状态枚举
    /// </summary>
    public enum HCDataStateEnmu
    {
        /// <summary>
        /// 发送成功
        /// </summary>
        SendSuccessed,
        /// <summary>
        /// 发送失败
        /// </summary>
        SendFailed,
        /// <summary>
        /// 接收到数据
        /// </summary>
        Received,
    }
}
