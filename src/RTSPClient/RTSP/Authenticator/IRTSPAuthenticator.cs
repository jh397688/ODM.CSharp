using RTSPStream.RTSP.Enum;
using RTSPStream.RTSP.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Authenticator
{
    internal interface IRTSPAuthenticator
    {
        RTSPAuthTypeEnum RTSPAuthType { get; }
        RTSPAuthDigestAlgorithmEnum RTSPAuthDigestAlgorithm { get; }
        string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge);
        void SetCredential(string? username, string? password);
    }
}
