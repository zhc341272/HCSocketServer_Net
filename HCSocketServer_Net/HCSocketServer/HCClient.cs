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
    public class HCClient : IClientEvents
    {
        /// <summary>
        /// 客户端的标识信息
        /// </summary>
        public String ClientID { get; set; }
        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public Socket Socket { get; set; }
        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime ConnectTime { get; set; }
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
        /// 添加接收数据
        /// </summary>
        /// <param name="data"></param>
        internal void AddReceiveData(byte[] data)
        {
            try
            {
                byte[] datalength = new byte[4];
                Array.Copy(data, 0, datalength, 0, datalength.Length);
                Array.Reverse(datalength);//倒转数据
                uint temp = BitConverter.ToUInt32(datalength, 0);//读取到包的长度

                if (temp == (data.Length - 4))
                {//恰好一个数据包
                    byte[] msgdata = new byte[temp];
                    Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                    ClientDataState?.Invoke(HCDataStateEnmu.Received, this, new HCMessage(ClientID, msgdata));
                }
                else if (temp < (data.Length - 4))
                {//粘包情况
                    byte[] msgdata = new byte[temp];
                    Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                    ClientDataState?.Invoke(HCDataStateEnmu.Received, this, new HCMessage(ClientID, msgdata));

                    byte[] datacontinue = new byte[data.Length - temp - 4];
                    Array.Copy(data, temp + 4, datacontinue, 0, datacontinue.Length);
                    AddReceiveData(datacontinue);
                }
                else
                {//半包情况
                    datacache.AddRange(data);
                    if (temp <= datacache.Count)
                    {//缓冲区中数据可形成整包
                        byte[] datacontinue = datacache.ToArray();
                        datacache.Clear();
                        AddReceiveData(datacontinue);
                    }
                }
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