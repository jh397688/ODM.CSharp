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
    internal class DigestSHA256Authenticator : IRTSPAuthenticator
    {
        public RTSPAuthTypeEnum RTSPAuthType => RTSPAuthTypeEnum.Digest;
        public RTSPAuthDigestAlgorithmEnum RTSPAuthDigestAlgorithm => RTSPAuthDigestAlgorithmEnum.SHA256;
        private string? _username;
        private string? _password;

        public string? GetAuthorizationHeader(RTSPRequestInfo request, RTSPAuthChallenge? challenge)
        {
            if (challenge == null || challenge.Type != RTSPAuthType || challenge.Algorithm != RTSPAuthDigestAlgorithm)
                return null;
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                return null;

            // HA1 = SHA256(username:realm:password)
            string ha1 = HashSHA256($"{_username}:{challenge.Realm}:{_password}");
            // HA2 = SHA256(method:uri)
            string ha2 = HashSHA256($"{request.RTSPMethod.ToString()}:{request.RTSPUri.ToString()}");
            // response = SHA256(HA1:nonce:HA2)
            string response = HashSHA256($"{ha1}:{challenge.Nonce}:{ha2}");

            return $"Authorization: Digest username=\"{_username}\", realm=\"{challenge.Realm}\", nonce=\"{challenge.Nonce}\", uri=\"{request.RTSPUri.ToString()}\", response=\"{response}\", algorithm=SHA-256";
        }

        public void SetCredential(string? username, string? password)
        {
            _username = username;
            _password = password;
        }

        private static string HashSHA256(string input)
        {
            using (var sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
        }
    }
}
