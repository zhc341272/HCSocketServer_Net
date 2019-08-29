using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace HCSocketServer
{
    /// <summary>
    /// 可重用的SocketAsyncEventArgs对象的集合
    /// </summary>
    class SocketAsyncEventArgsPool
    {
        /// <summary>
        /// 主要的对象池
        /// </summary>
        Stack<SocketAsyncEventArgs> m_pool;

        /// <summary>
        /// 按照指定大小初始化对象池
        /// </summary>
        /// <param name="capacity">对象池大小</param>
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// 向对象池中添加SocketAsyncEventArg对象
        /// </summary>
        /// <param name="item"></param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {//禁止空对象添加
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }

            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        /// <summary>
        /// 从对象池中删除并返回SocketAsyncEventArgs实例
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        /// <summary>
        /// 对象池中对象数目
        /// </summary>
        public int Count
        {
            get { return m_pool.Count; }
        }

    }
}
