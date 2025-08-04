using RTSPStream.RTSP.Enum;
using RTSPStream.RTSP.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Authenticator.Impl
{
    internal class DigestMD5Authenticator : IRTSPAuthenticator
    {
        public RTSPAuthTypeEnum RTSPAuthType => RTSPAuthTypeEnum.Digest;
        public RTSPAuthDigestAlgorithmEnum RTSPAuthDigestAlgorithm => RTSPAuthDigestAlgorithmEnum.MD5;
        private string? _username;
        private string? _password;

        public string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge)
        {
            if (challenge == null || challenge.Type != RTSPAuthType || challenge.Algorithm != RTSPAuthDigestAlgorithm)
                return null;
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                return null;

            string ha1 = HashMD5($"{_username}:{challenge.Realm}:{_password}");
            string ha2 = HashMD5($"{request.RTSPMethod.ToString()}:{request.RTSPUri.ToString()}");
            string response = HashMD5($"{ha1}:{challenge.Nonce}:{ha2}");

            return $"Authorization: " +
                   $"Digest username=\"{_username}\", " +
                   $"realm=\"{challenge.Realm}\", nonce=\"{challenge.Nonce}\", uri=\"{request.RTSPUri.ToString()}\", " +
                   $"response=\"{response}\", algorithm=MD5";
        }

        public void SetCredential(string? username, string? password)
        {
            _username = username;
            _password = password;
        }

        private static string HashMD5(string input)
        {
            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
        }
    }
}
