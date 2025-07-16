using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Info
{
    internal class RTSPAuthChallenge
    {
        public RTSPAuthTypeEnum Type { get; set; }
        public string? Realm { get; set; }
        public string? Nonce { get; set; }
        public RTSPAuthDigestAlgorithmEnum? Algorithm { get; set; }
        public string? Qop { get; set; }
        public string? Opaque { get; set; }
    }
}
