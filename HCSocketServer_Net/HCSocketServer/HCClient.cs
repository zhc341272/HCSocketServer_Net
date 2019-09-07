using HCSocketServer.Common.Enmu;
using HCSocketServer.Interface;
using HCSocketServer.Message;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HCSocketServer
{
    public class HCClient : IClient
    {
        /// <summary>
        /// 客户端的标识信息
        /// </summary>
        public string ClientID { get; set; }
        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public Socket Socket { get; set; }
        /// <summary>  
        /// 连接时间  
        /// </summary>  
        internal DateTime LastHeart { get; set; }
        /// <summary>
        /// 解析半包数据的缓冲
        /// </summary>
        private List<byte> datacache = new List<byte>();
        /// <summary>
        /// 发送区数据集合
        /// </summary>
        private BlockingCollection<byte[]> data_collection = new BlockingCollection<byte[]>();
        /// <summary>
        /// 发送数据的线程
        /// </summary>
        private Thread m_sendThread;
        /// <summary>
        /// 客户端数据状态事件
        /// </summary>
        public event HCClientDataStateDelegate ClientDataState;

        public HCClient()
        {
            m_sendThread = new Thread(SendData);
            m_sendThread.IsBackground = true;
            m_sendThread.Start();
        }

        /// <summary>
        /// 解析接收到的数据
        /// </summary>
        /// <param name="data"></param>
        public void AnalysisData(byte[] data)
        {
            try
            {           
                if (data.Length < 4)
                {
                    if (datacache.Count + data.Length < 4)
                    {//缓冲区与新添加的数据不够4个字节
                        datacache.AddRange(data);
                        return;
                    }
                }

                if (datacache.Count == 0)
                {//缓冲区没有数，需要处理的数据是带包头的
                    byte[] datalength = new byte[4];//获取包头
                    Array.Copy(data, 0, datalength, 0, datalength.Length);
                    Array.Reverse(datalength);//倒转数据
                    uint packagelength = BitConverter.ToUInt32(datalength, 0);//读取到包的长度

                    if (packagelength == (data.Length - 4))
                    {//恰好一个数据包
                        byte[] msgdata = new byte[packagelength];
                        Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                        ClientDataState?.Invoke(HCDataStateEnmu.Received, this, new HCMessage(ClientID, msgdata));
                    }
                    else if (packagelength < (data.Length - 4))
                    {//粘包情况
                        byte[] msgdata = new byte[packagelength];
                        Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                        ClientDataState?.Invoke(HCDataStateEnmu.Received, this, new HCMessage(ClientID, msgdata));

                        byte[] datacontinue = new byte[data.Length - packagelength - 4];
                        Array.Copy(data, packagelength + 4, datacontinue, 0, datacontinue.Length);
                        AnalysisData(datacontinue);
                    }
                    else
                    {//半包情况
                        datacache.AddRange(data);
                        if (packagelength <= datacache.Count - 4)
                        {//缓冲区中数据可形成整包
                            byte[] datacontinue = datacache.ToArray();
                            datacache.Clear();
                            AnalysisData(datacontinue);
                        }
                    }
                }
                else
                {//缓冲区有数据，接收到的数据是没有包头的
                    datacache.AddRange(data);
                    byte[] datacontinue = datacache.ToArray();
                    datacache.Clear();
                    AnalysisData(datacontinue);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 清除缓冲区数据
        /// </summary>
        public void ClearCache()
        {
            try
            {
                ClientID = "";
                datacache.Clear();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 添加发送数据(byte)
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            try
            {
                byte[] start = BitConverter.GetBytes(data.Length);
                Array.Reverse(start);//倒转数据

                byte[] result = new byte[4 + data.Length];
                start.CopyTo(result, 0);
                data.CopyTo(result, 4);
                data_collection.Add(result);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 添加发送数据(string)
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            try
            {
                Send(Encoding.UTF8.GetBytes(data));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        private void SendData()
        {
            while (true)
            {
                try
                {
                    byte[] data = data_collection.Take();
                    Socket.Send(data);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}