using System.Collections.Generic;
using System.Net.Sockets;

namespace HCSocketServer
{
    /// <summary>
    /// 此类创建一个大缓冲区，可以将其分割并分配给SocketAsyncEventArgs对象，以便与每个套接字I/O操作一起使用
    /// 可以轻松地重用缓冲区并防止内存碎片化
    /// </summary>
    class BufferManager
    {
        /// <summary>
        /// 缓冲池可控制的最大字节数量
        /// </summary>
        readonly int m_numBytes;
        /// <summary>
        /// 缓冲区管理器维护的基础字节数组
        /// </summary>
        byte[] m_buffer;
        /// <summary>
        /// 空闲的缓冲池位置
        /// </summary>
        Stack<int> m_freeIndexPool;
        /// <summary>
        /// 当前总缓冲区的偏移位置
        /// </summary>
        int m_currentIndex;
        /// <summary>
        /// 单socket使用的缓冲区大小
        /// </summary>
        readonly int m_bufferSize;

        /// <summary>
        /// 缓冲区管理器，所有socket统一管理，防止内存碎片化
        /// </summary>
        /// <param name="totalBytes">管理的缓冲区总大小</param>
        /// <param name="bufferSize">单独的socket使用的缓冲区大小</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 分配缓冲池使用的缓冲区空间
        /// </summary>
        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }

        /// <summary>
        /// 将缓冲池中的缓冲区分配给指定的SocketAsyncEventArgs对象
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {//没有空间分配
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        /// <summary>
        /// 从SocketAsyncEventArg对象中删除缓冲区，这会将缓冲区释放回缓冲池
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}
