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
        public string ClientID = "";
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
        /// 解析数据的线程
        /// </summary>
        private Thread m_recThread;

        public event HCClientDataStateDelegate ClientDataState;
        public event HCClientStateDelegate ClientState;

        /// <summary>
        /// 心跳计数器
        /// </summary>
        public int m_pingcount = 0;
        /// <summary>
        /// 超时时间
        /// </summary>
        public int m_timeout = 5;

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
                Console.WriteLine("准备解析数据：");
                m_pingcount = 0;

                if (data.Length < 4)
                {
                    Console.WriteLine("解析数据的长度小于4个，数据进入缓冲区");
                    if (datacache.Count + data.Length < 4)
                    {//缓冲区与新添加的数据不够4个字节
                        Console.WriteLine("缓冲区已有数据与数据长度总长小于4，数据追加进入缓冲区，并准备接收下一次数据");
                        datacache.AddRange(data);
                        return;
                    }
                }

                if (datacache.Count == 0)
                {//缓冲区没有数，需要处理的数据是带包头的
                    Console.WriteLine("缓冲区无数据，本次数据包含包头数据");
                    byte[] datalength = new byte[4];//获取包头
                    Array.Copy(data, 0, datalength, 0, datalength.Length);
                    Console.WriteLine("输出包头" + datalength[0] + " " + datalength[1] + " " + datalength[2] + " " + datalength[3]);
                    Array.Reverse(datalength);//倒转数据
                    uint packagelength = BitConverter.ToUInt32(datalength, 0);//读取到包的长度
                    Console.WriteLine("本次数据包长度为：" + packagelength + "；数据总长为：" + data.Length);

                    if (packagelength == (data.Length - 4))
                    {//恰好一个数据包
                        Console.WriteLine("恰好一个完整的数据包");
                        if (packagelength == 1)
                        {//ping包回馈
                            Console.WriteLine("ping包");
                        }
                        else
                        {
                            Console.WriteLine("非ping包，准备传递数据");
                            byte[] msgdata = new byte[packagelength];
                            Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                            ClientDataState?.Invoke(HCDataStateEnmu.Received, new HCMessage(this, msgdata));
                        }
                    }
                    else if (packagelength < (data.Length - 4))
                    {//粘包情况
                        Console.WriteLine("粘包情况");
                        if (packagelength == 1)
                        {//ping包回馈
                            Console.WriteLine("粘包中的ping包");
                        }
                        else
                        {
                            Console.WriteLine("粘包中的非ping包，准备传递数据");
                            byte[] msgdata = new byte[packagelength];
                            Array.Copy(data, 4, msgdata, 0, msgdata.Length);
                            ClientDataState?.Invoke(HCDataStateEnmu.Received, new HCMessage(this, msgdata));
                        }

                        Console.WriteLine("继续处理剩下的粘包数据，长度：" + (data.Length - packagelength - 4));
                        byte[] datacontinue = new byte[data.Length - packagelength - 4];
                        Array.Copy(data, packagelength + 4, datacontinue, 0, datacontinue.Length);

                        Console.WriteLine("继续处理的粘包数据头：" + data[packagelength] + " " + data[packagelength + 1] +
                            " " + data[packagelength + 2] + " " + data[packagelength + 3]);
                        AnalysisData(datacontinue);
                    }
                    else
                    {//半包情况
                        Console.WriteLine("半包情况");
                        datacache.AddRange(data);
                        if (packagelength <= datacache.Count - 4)
                        {//缓冲区中数据可形成整包
                            Console.WriteLine("缓冲区中数据可形成整包，继续解析数据");
                            byte[] datacontinue = datacache.ToArray();
                            datacache.Clear();
                            AnalysisData(datacontinue);
                        }
                    }
                }
                else
                {//缓冲区有数据，接收到的数据是没有包头的
                    Console.WriteLine("缓冲区有数据，接收到的数据是没有包头的");
                    datacache.AddRange(data);
                    byte[] datacontinue = datacache.ToArray();
                    datacache.Clear();
                    AnalysisData(datacontinue);
                }
            }
            catch (Exception exp)
            {
                ClientState?.Invoke(HCClientStateEnmu.RunningException, this);
            }
        }

        /// <summary>
        /// 清除缓冲区数据
        /// </summary>
        public void ClearCache()
        {
            try
            {
                m_pingcount = 0;
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

        /// <summary>
        /// 发送心跳数据
        /// </summary>
        public void SendPing()
        {
            if (m_pingcount >= m_timeout)
            {//超时
                Console.WriteLine("超时关闭客户端SOCKET");
                ClearCache();
                Socket.Close();
            }
            else
            {
                if (!ClientID.Equals(""))
                {//只有拥有客户端标识的角色才可以发送心跳包
                    Send(new byte[] { 112 });
                    m_pingcount++;
                }
            }
        }
    }
}