using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace HCSocketServer
{
    public class HCServer
    {
        /// <summary>
        /// 最大处理连接数
        /// </summary>
        private readonly int m_numConnections;
        /// <summary>
        /// 每个socket I/O操作使用的缓冲区大小
        /// </summary>
        private int m_receiveBufferSize;
        /// <summary>
        /// 所有socket操作的可复用的大型缓冲区，管理流大小
        /// </summary>
        BufferManager m_bufferManager;
        /// <summary>
        /// 读、写，需要2份，不为接收分配缓冲区
        /// </summary>
        const int m_opsToPreAlloc = 2;
        /// <summary>
        /// 用于侦听传入连接请求的socket
        /// </summary>
        Socket m_listenSocket;
        /// <summary>
        /// 可重用的SocketAsyncEventArgs对象池，用于写入，读取和接受套接字操作
        /// </summary>
        SocketAsyncEventArgsPool m_readWritePool;
        /// <summary>
        /// 计数器接收的服务器总字节数
        /// </summary>
        int m_totalBytesRead;
        /// <summary>
        /// 连接到服务器的客户端总数
        /// </summary>
        int m_numConnectedSockets;
        /// <summary>
        /// 限制线程池中接收客户端的最大数目
        /// </summary>
        Semaphore m_maxNumberAcceptedClients;

        /// <summary>
        /// 创建未初始化的服务器实例。 要启动服务器侦听连接请求，先调用Init方法，然后调用Start方法
        /// </summary>
        /// <param name="numConnections">同时处理的最大连接数</param>
        /// <param name="receiveBufferSize">用于每个socket I/O操作的缓冲区大小</param>
        public HCServer(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            m_bufferManager = new BufferManager(m_receiveBufferSize * numConnections * m_opsToPreAlloc, m_receiveBufferSize);
            m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        /// <summary>
        /// 通过预分配可重用缓冲区和上下文对象来初始化服务器
        /// </summary>
        public void Init()
        {
            //分配一个大字节缓冲区，所有I/O操作都使用这一个，这可以防止内存碎片化
            m_bufferManager.InitBuffer();

            //预分配SocketAsyncEventArgs对象池
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                //预分配一组可重用的SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                //将缓冲池中的字节缓冲区分配给SocketAsyncEventArg对象
                m_bufferManager.SetBuffer(readWriteEventArg);

                //将SocketAsyncEventArgs添加到池中
                m_readWritePool.Push(readWriteEventArg);
            }

        }

        /// <summary>
        /// 启动服务器，使其正在侦听传入的连接请求
        /// </summary>
        /// <param name="localEndPoint">侦听的网络端点</param>
        public void Start(IPEndPoint localEndPoint)
        {
            //创建侦听传入连接的socket
            m_listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_listenSocket.Bind(localEndPoint);
            //限定100个监听压力
            m_listenSocket.Listen(100);

            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            //Console.WriteLine("Press any key to terminate the server process....");
            //Console.ReadKey();
        }

        /// <summary>
        /// 开始接受来自客户端的连接请求的操作
        /// </summary>
        /// <param name="acceptEventArg"></param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
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

        /// <summary>
        /// 与Socket.AcceptAsync操作关联的回调方法，并在接受操作完成时调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("接收到客户端连接，服务器当前客户端连接数：{0}",
                m_numConnectedSockets);

            //从对象池中获取一个新对象
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

            //客户端连接之后，立即向连接抛出接收信号
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }

            //接收下一个连接请求
            StartAccept(e);
        }

        /// <summary>
        /// 只要在套接字上完成接收或发送操作，就会调用此方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("socket上完成的最后一个操作不是接收或发送");
            }

        }

        /// <summary>
        /// 异步接收操作，如果远程主机关闭了连接，则关闭socket
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            //检查远程主机是否关闭了连接
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //增加服务器接收的总字节数
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("服务器已经读取的总字节数： {0} bytes", m_totalBytesRead);

                //将收到的数据回显给客户端
                e.SetBuffer(e.Offset, e.BytesTransferred);
                bool willRaiseEvent = token.Socket.SendAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessSend(e);
                }

            }
            else
            {//远程主机关闭了连接
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// 异步发送操作
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
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

        /// <summary>
        /// 关闭socket客户端
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            //关闭与客户端连接的socket
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {//客户端已经关闭

            }
            token.Socket.Close();

            //连接数递减
            Interlocked.Decrement(ref m_numConnectedSockets);

            //释放SocketAsyncEventArgs，以便其他客户端可以重用它们
            m_readWritePool.Push(e);

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("客户端从服务器断开连接，当前服务器客户端连接数：{0}",
                m_numConnectedSockets);
        }

    }
}
