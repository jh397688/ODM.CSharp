using RTSPStream.RTSP.Enum;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Authenticator.Impl
{
    internal class AnonymousAuthenticator : IRTSPAuthenticator
    {
        public RTSPAuthTypeEnum RTSPAuthType => RTSPAuthTypeEnum.Anonymous;
        public RTSPAuthDigestAlgorithmEnum RTSPAuthDigestAlgorithm => RTSPAuthDigestAlgorithmEnum.None;

        public string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge) => null;
        public void SetCredential(string? username, string? password) { /* 무시 */ }
    }
}
