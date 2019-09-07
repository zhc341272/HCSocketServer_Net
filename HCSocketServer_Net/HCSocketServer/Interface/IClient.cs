using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSocketServer.Interface
{
    internal interface IClient : IClientEvents
    {
        void AnalysisData(byte[] data);
        void Send(byte[] data);
        void Send(string data);
    }
}
