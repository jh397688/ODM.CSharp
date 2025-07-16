using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Info
{
    internal class RTSPRequestInfo
    {
        public RTSPMethodEnum RTSPMethod { get; set; }
        public Uri RTSPUri { get; set; }
    }
}
