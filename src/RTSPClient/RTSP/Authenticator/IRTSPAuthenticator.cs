using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Authenticator
{
    internal interface IRTSPAuthenticator
    {
        string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge);
        void SetCredential(string? username, string? password);
    }
}
