using RTSPStream.RTSP.Enum;
using RTSPStream.RTSP.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Authenticator.Impl
{
    internal class BasicAuthenticator : IRTSPAuthenticator
    {
        public RTSPAuthTypeEnum RTSPAuthType => RTSPAuthTypeEnum.Basic;
        public RTSPAuthDigestAlgorithmEnum RTSPAuthDigestAlgorithm => RTSPAuthDigestAlgorithmEnum.None;
        private string? _username, _password; 

        public string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                return null;

            string credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            return $"Authorization: Basic {credentials}";
        }

        public void SetCredential(string? username, string? password)
        {
            _username = username;
            _password = password;
        }
    }
}
