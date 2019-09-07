using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using HCSocketServer.Interface;
using System.Collections.Concurrent;
using System.Collections;
using HCSocketServer.Message;
using HCSocketServer.Common.Enmu;

namespace HCSocketServer
{
    public class HCServer : IServer
    {
        /// <summary>
        /// 最大处理连接数
        /// </summary>
        private readonly int m_numConnections;
        /// <summary>
        /// 每个socket I/O读取操作使用的缓冲区大小
        /// </summary>
        private int m_receiveBufferSize;
        /// <summary>
        /// 每个socket I/O发送操作使用的缓冲区大小
        /// </summary>
        private int m_sendBufferSize;
        /// <summary>
        /// 所有socket读取操作的可复用的大型缓冲区，管理流大小
        /// </summary>
        private BufferManager m_readBufferManager;
        /// <summary>
        /// 用于侦听传入连接请求的socket
        /// </summary>
        private Socket m_listenSocket;
        /// <summary>
        /// 可重用的SocketAsyncEventArgs对象池，用于读取和接受套接字操作
        /// </summary>
        private SocketAsyncEventArgsPool m_readPool;
        /// <summary>
        /// 连接到服务器的客户端总数
        /// </summary>
        private int m_numConnectedSockets;
        /// <summary>
        /// 限制线程池中接收客户端的最大数目
        /// </summary>
        private Semaphore m_maxNumberAcceptedClients;
        /// <summary>
        /// 已连接的客户端池
        /// </summary>
        private ArrayList m_clientPool = ArrayList.Synchronized(new ArrayList());
        /// <summary>
        /// 心跳线程
        /// </summary>
        private Thread m_heartthread;

        public event HCServerStateDelegate ServerState;
        public event HCClientStateDelegate ClientState;
        public event HCClientDataStateDelegate ClientDataState;

        /// <summary>
        /// 创建未初始化的服务器实例。 要启动服务器侦听连接请求，先调用Init方法，然后调用Start方法
        /// </summary>
        /// <param name="numConnections">同时处理的最大连接数</param>
        /// <param name="receiveBufferSize">用于每个socket I/O操作接收的缓冲区大小</param>
        /// <param name="sendBufferSize">用于每个socket I/O操作发送的缓冲区大小</param>
        public HCServer(int numConnections, int receiveBufferSize, int wirteBufferSize)
        {
            //m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;

            m_receiveBufferSize = receiveBufferSize;
            m_sendBufferSize = wirteBufferSize;
        }

        /// <summary>
        /// 通过预分配可重用缓冲区和上下文对象来初始化服务器
        /// </summary>
        public void Init()
        {
            try
            {
                m_readBufferManager = new BufferManager(m_receiveBufferSize * m_numConnections, m_receiveBufferSize);

                m_readPool = new SocketAsyncEventArgsPool(m_numConnections);

                m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);

                //分配一个大字节缓冲区，所有I/O操作都使用这一个，这可以防止内存碎片化
                m_readBufferManager.InitBuffer();

                //预分配SocketAsyncEventArgs对象池
                SocketAsyncEventArgs readEventArg;

                for (int i = 0; i < m_numConnections; i++)
                {//读取
                 //预分配一组可重用的SocketAsyncEventArgs
                    readEventArg = new SocketAsyncEventArgs();
                    readEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                    readEventArg.UserToken = new HCClient();
                    ((HCClient)readEventArg.UserToken).ClientDataState += HCServer_ClientDataState;

                    //将缓冲池中的字节缓冲区分配给SocketAsyncEventArg对象
                    m_readBufferManager.SetBuffer(readEventArg);

                    //将SocketAsyncEventArgs添加到池中
                    m_readPool.Push(readEventArg);
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.Failed, e.Message);
            }
        }

        /// <summary>
        /// 启动服务器，使其正在侦听传入的连接请求
        /// </summary>
        /// <param name="localEndPoint">侦听的网络端点</param>
        public void Start(IPEndPoint localEndPoint)
        {
            try
            {
                //创建侦听传入连接的socket
                m_listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                m_listenSocket.Bind(localEndPoint);
                //限定100个监听压力
                m_listenSocket.Listen(100);

                StartAccept(null);

                //m_heartthread = new Thread(ServerHeart);
                //m_heartthread.Start();
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.Failed, e.Message);
            }
        }

        /// <summary>
        /// 服务器心跳
        /// </summary>
        private void ServerHeart()
        {
            SendMsgToAllClient("ping");
            Thread.Sleep(1000);
        }

        /// <summary>
        /// 断开与客户端的连接
        /// </summary>
        /// <param name="client"></param>
        public void CloseClient(HCClient client)
        {
            try
            {
                foreach (SocketAsyncEventArgs item in m_clientPool)
                {
                    if (((HCClient)item.UserToken).Equals(client))
                    {
                        CloseClientSocket(item);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 开始接受来自客户端的连接请求的操作
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                if (acceptEventArg == null)
                {
                    acceptEventArg = new SocketAsyncEventArgs();
                    acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
                }
                else
                {
                    //因为正在重用，所以必须清除socket
                    acceptEventArg.AcceptSocket = null;
                }

                m_maxNumberAcceptedClients.WaitOne();
                bool willRaiseEvent = m_listenSocket.AcceptAsync(acceptEventArg);
                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 与Socket.AcceptAsync操作关联的回调方法，并在接受操作完成时调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 接收socket任务
        /// </summary>
        /// <param name="e"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                Interlocked.Increment(ref m_numConnectedSockets);
                Console.WriteLine("接收到客户端连接，服务器当前客户端连接数：{0}",
                    m_numConnectedSockets);

                //从对象池中获取一个新对象
                SocketAsyncEventArgs readEventArgs = m_readPool.Pop();
                ((HCClient)readEventArgs.UserToken).Socket = e.AcceptSocket;
                ((HCClient)readEventArgs.UserToken).Socket.NoDelay = true;
                ((HCClient)readEventArgs.UserToken).ClientID = "";//重置ClientID

                //将该连接对象填入连接池，此时该客户端还没有通用标识信息
                m_clientPool.Add(readEventArgs);

                //客户端连接之后，立即向连接抛出接收信号
                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }

                //接收下一个连接请求
                StartAccept(e);
            }
            catch (Exception exp)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, exp.Message);
            }
        }

        /// <summary>
        /// 客户端接收到消息
        /// </summary>
        /// <param name="state"></param>
        /// <param name="msg"></param>
        private void HCServer_ClientDataState(HCDataStateEnmu state, HCClient client, HCMessage msg)
        {
            try
            {
                if (msg.ClientID == "")
                {//没有客户端ID，解析ID
                    string[] info = msg.GetDataString().Split(new string[] { "/" }, StringSplitOptions.None);
                    string ver = info[0];//协议版本号
                    client.ClientID = info[1];//客户端ID
                    ClientState?.Invoke(HCClientConnectStateEnmu.Connected, client);//客户端连接成功（业务逻辑上成功）
                }
                else
                {
                    ClientDataState?.Invoke(state, client, msg);
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 只要在套接字上完成接收或发送操作，就会调用此方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive://接收成功
                        ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send://发送成功
                        ProcessSend(e);
                        break;
                    default:
                        ServerState?.Invoke(HCServerStateEnmu.RunningException, "socket上完成的最后一个操作不是接收或发送");
                        break;
                        //throw new ArgumentException("socket上完成的最后一个操作不是接收或发送");
                }
            }
            catch (Exception exp)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, exp.Message);
            }

        }

        /// <summary>
        /// 异步接收操作，如果远程主机关闭了连接，则关闭socket
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                //检查远程主机是否关闭了连接
                HCClient token = (HCClient)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    byte[] recdata = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, recdata, 0, e.BytesTransferred);
                    token.AnalysisData(recdata);

                    ContinueReceive(e);
                }
                else
                {//远程主机关闭了连接
                    CloseClientSocket(e);
                }
            }
            catch (Exception exp)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, exp.Message);
            }
        }

        /// <summary>
        /// 处理发送结束
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {

        }

        /// <summary>
        /// 继续接收数据
        /// </summary>
        /// <param name="e"></param>
        private void ContinueReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    HCClient token = (HCClient)e.UserToken;
                    //读取从客户端发送的下一个数据块
                    bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception exp)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, exp.Message);
            }
        }

        /// <summary>
        /// 关闭socket客户端
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            try
            {
                HCClient token = e.UserToken as HCClient;


                if (m_clientPool.Contains(e))
                {
                    //关闭与客户端连接的socket
                    try
                    {
                        token.Socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception)
                    {//客户端已经关闭

                    }
                    ClientState?.Invoke(HCClientConnectStateEnmu.Disconnected, token);//客户端断开连接
                    token.Socket.Close();
                    token.ClearCache();

                    //连接数递减
                    Interlocked.Decrement(ref m_numConnectedSockets);

                    //释放SocketAsyncEventArgs，以便其他客户端可以重用它们
                    m_readPool.Push(e);
                    m_clientPool.Remove(e);

                    m_maxNumberAcceptedClients.Release();
                    Console.WriteLine("客户端从服务器断开连接，当前服务器客户端连接数：{0}",
                        m_numConnectedSockets);
                }
                else
                {
                    Console.WriteLine("客户端从服务器断开连接");
                }

                
            }
            catch (Exception exp)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, exp.Message);
            }
        }

        /// <summary>
        /// 向指定clientID的客户端发送数据
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="data"></param>
        public void SendMsgByClientID(string clientid, byte[] data)
        {
            try
            {
                foreach (SocketAsyncEventArgs item in m_clientPool)
                {
                    if (((HCClient)(item.UserToken)).ClientID.Equals(clientid))
                    {
                        ((HCClient)(item.UserToken)).Send(data);
                    }
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 向指定clientID的客户端发送数据
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="data"></param>
        public void SendMsgByClientID(string clientid, string data)
        {
            try
            {
                foreach (SocketAsyncEventArgs item in m_clientPool)
                {
                    if (((HCClient)(item.UserToken)).ClientID.Equals(clientid))
                    {
                        ((HCClient)(item.UserToken)).Send(data);
                    }
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 向所有客户端发送数据
        /// </summary>
        /// <param name="data"></param>
        public void SendMsgToAllClient(byte[] data)
        {
            try
            {
                foreach (SocketAsyncEventArgs item in m_clientPool)
                {
                    ((HCClient)(item.UserToken)).Send(data);
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }

        /// <summary>
        /// 向所有客户端发送数据
        /// </summary>
        /// <param name="clientid"></param>
        /// <param name="data"></param>
        public void SendMsgToAllClient(string data)
        {
            try
            {
                foreach (SocketAsyncEventArgs item in m_clientPool)
                {
                    ((HCClient)(item.UserToken)).Send(data);
                }
            }
            catch (Exception e)
            {
                ServerState?.Invoke(HCServerStateEnmu.RunningException, e.Message);
            }
        }
    }
}
