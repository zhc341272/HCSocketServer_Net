using HCSocketServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// <summary>
        /// 服务器实例
        /// </summary>
        HCServer m_server;

        /// <summary>
        /// listview使用的绑定数据
        /// </summary>
        private ObservableCollection<ClientData> m_clientlistsource = new ObservableCollection<ClientData>();

        /// <summary>
        /// 客户端序号计数器
        /// </summary>
        private int clientid = 0;

        public MainWindow()
        {
            InitializeComponent();

            m_clientlist.ItemsSource = m_clientlistsource;
        }

        /// <summary>
        /// 点击了启动服务器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickeStartServerBtn(object sender, RoutedEventArgs e)
        {
            m_server = new HCServer(70, 40960, 40960);
            m_server.Init();
            m_server.Start(new System.Net.IPEndPoint(IPAddress.Any, 3237));
            m_server.ClientDataState += Server_ClientDataState;
            m_server.ClientState += M_server_ClientState;
            m_server.ServerState += M_server_ServerState;
        }

        private void M_server_ServerState(HCSocketServer.Common.Enmu.HCServerStateEnmu state, string msg)
        {
            switch (state)
            {
                case HCSocketServer.Common.Enmu.HCServerStateEnmu.Success:
                    Trace.WriteLine("1111Success");
                    break;
                case HCSocketServer.Common.Enmu.HCServerStateEnmu.Failed:
                    Trace.WriteLine("1111Failed");
                    break;
                case HCSocketServer.Common.Enmu.HCServerStateEnmu.RunningException:
                    Trace.WriteLine("1111" + msg);
                    break;
                default:
                    break;
            }
            
        }

        private void M_server_ClientState(HCSocketServer.Common.Enmu.HCClientStateEnmu state, HCClient client)
        {
            switch (state)
            {
                case HCSocketServer.Common.Enmu.HCClientStateEnmu.Connected:
                    m_clientlist.Dispatcher.Invoke(new Action(() =>
                    {
                        m_clientlistsource.Add(new ClientData(clientid, client.Socket.RemoteEndPoint.ToString(), client.ClientID));
                        clientid++;
                    }));              
                    break;
                case HCSocketServer.Common.Enmu.HCClientStateEnmu.Failed:
                    break;
                case HCSocketServer.Common.Enmu.HCClientStateEnmu.Disconnected:
                    m_clientlist.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (ClientData item in m_clientlistsource)
                        {
                            if (item.clientid.Equals(client.ClientID))
                            {
                                m_clientlistsource.Remove(item);
                                clientid = 0;
                                break;
                            }
                        }

                        foreach (ClientData item in m_clientlistsource)
                        {
                            item.id = clientid;
                            clientid++;
                        }
                    }));

                    break;
                default:
                    break;
            }
        }

        private void Server_ClientDataState(HCSocketServer.Common.Enmu.HCDataStateEnmu state, HCSocketServer.Message.HCMessage message)
        {
            try
            {
                //Trace.WriteLine(message.GetDataString()) ;

                switch (state)
                {
                    case HCSocketServer.Common.Enmu.HCDataStateEnmu.SendSuccessed:
                        Trace.WriteLine("2222SendSuccessed");
                        break;
                    case HCSocketServer.Common.Enmu.HCDataStateEnmu.SendFailed:
                        Trace.WriteLine("2222SendSuccessed");
                        break;
                    case HCSocketServer.Common.Enmu.HCDataStateEnmu.Received:

                        //JObject json = JObject.Parse(message.GetDataString());
                        //if (json["code"].ToString().Equals("0"))
                        //{//认真听讲
                        //    JObject sendjson = new JObject { { "code", "0" }, { "data", "指令：认真听讲" }, { "sender", message.ClientID.ToString() } };
                        //    m_server.SendMsgToAllClient(sendjson.ToString());
                        //}
                        //else if (json["code"].ToString().Equals("1"))
                        //{//转发消息
                        //    json["sender"] = message.ClientID.ToString();
                        //    //JObject sendjson = new JObject { { "code", "1" }, { "data", "指令：转发消息" }, { "sender", client.ClientID.ToString() } };
                        //    m_server.SendMsgToAllClient(json.ToString());
                        //}

                        m_clientlist.Dispatcher.Invoke(new Action(() =>
                        {
                            foreach (ClientData item in m_clientlistsource)
                            {
                                if (item.clientid.Equals(message.ClientID))
                                {
                                    item.message = "接收到指令：" + DateTime.Now.ToString();
                                    break;
                                }
                            }
                        }));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("异常：" + e.Message.ToString()) ;
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (ClientData item in m_clientlistsource)
                {
                    m_server.SendMsgByClientID(item.clientid, "记号记号大家看法和进口撒护国客户尽快给就" +
                        "发哈尽快的恢复金卡恢复健康的的恢复科举会根据考生的韩国和快速" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "打击阿富汗金卡沙发哈手机卡号即可四大护法金卡会尽快发哈健康黑人与" +
                        "i阿强健康的繁华韩国进口蛤科");
                }
            }
            catch (Exception exp)
            {
                Trace.WriteLine("1111" + exp.Message);
            }

        }
    }
}
