using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HCSocketServer.Message
{
    public class HCMessage
    {
        /// <summary>
        /// 客户端标识
        /// </summary>
        public string ClientID { get; set; }
        /// <summary>
        /// 客户端数据（byte）
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// 客户端
        /// </summary>
        public HCClient HCClient { get; set; }

        public HCMessage(HCClient client, byte[] data)
        {
            HCClient = client;
            ClientID = client.ClientID;
            Data = data;
        }

        /// <summary>
        /// 获取字符串信息
        /// </summary>
        /// <returns></returns>
        public String GetDataString()
        {
            string result = null;
            try
            {
                result = Encoding.UTF8.GetString(Data);
            }
            catch (Exception)
            {

            }                    
            return result;
        }
    }
}
