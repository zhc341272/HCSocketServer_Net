using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HCSocketServer
{
    class AsyncUserToken
    {
        /// <summary>  
        /// 客户端IP地址  
        /// </summary>  
        public IPAddress IPAddress { get; set; }

        /// <summary>  
        /// 远程地址  
        /// </summary>  
        public EndPoint Remote { get; set; }

        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public Socket Socket { get; set; }

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime ConnectTime { get; set; }

        /// <summary>  
        /// 数据缓存区  
        /// </summary>  
        public List<byte> Buffer { get; set; }


        public AsyncUserToken()
        {
            this.Buffer = new List<byte>();
        }
    }
}