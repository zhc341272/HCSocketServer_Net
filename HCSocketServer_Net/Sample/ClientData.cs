using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class ClientData : INotifyPropertyChanged //通知接口
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private int _id;
        private string _ip;
        private string _clientid;
        private string _message;

        public string ip
        {
            get { return _ip; }
            set
            {
                _ip = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ip"));
            }
        }
        public string clientid
        {
            get { return _clientid; }
            set
            {
                _clientid = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("clientid"));
            }
        }
        public string message
        {
            get { return _message; }
            set
            {
                _message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("message"));
            }
        }
        public int id
        {
            get { return _id; }
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("id"));
            }
        }
        public ClientData(int id, string ip, string clientid)
        {
            _id = id;
            _ip = ip;
            _clientid = clientid;
        }
    }
}
