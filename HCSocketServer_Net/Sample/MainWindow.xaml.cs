using HCSocketServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sample
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        HCServer m_server;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 点击了启动服务器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickeStartServerBtn(object sender, RoutedEventArgs e)
        {
            m_server = new HCServer(10, 200, 200);
            m_server.Init();
            m_server.Start(new System.Net.IPEndPoint(IPAddress.Any, 3237));
            m_server.ClientDataState += Server_ClientDataState;
        }

        private void Server_ClientDataState(HCSocketServer.Common.Enmu.HCDataStateEnmu state, HCClient client, HCSocketServer.Message.HCMessage message)
        {
            Trace.WriteLine("---------------------------");
        }
    }
}
