using RTSPStream.Lib;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Info
{
    internal static class RtspAuthChallengeParser
    {
        /// <summary>
        /// RTSP WWW-Authenticate 헤더를 파싱해서 RtspAuthChallenge 객체로 반환
        /// </summary>
        /// <param name="wwwAuthenticateValue">예: Digest realm="IP Camera", nonce="ABC", algorithm=MD5, qop="auth"</param>
        public static RTSPAuthChallenge Parse(string wwwAuthenticateValue)
        {
            // "Digest realm="...", nonce="...", algorithm=MD5, qop="auth", opaque="..."
            var challenge = new RTSPAuthChallenge();

            // Type 추출 (Digest, Basic 등)
            var typeMatch = Regex.Match(wwwAuthenticateValue, @"^(Digest|Basic)", RegexOptions.IgnoreCase);
            challenge.Type = typeMatch.Success ? EnumParser.StringToEnum<RTSPAuthTypeEnum>(typeMatch.Groups[1].Value) : RTSPAuthTypeEnum.None;

            // Key-Value 파싱 (따옴표 감싸진 값 포함)
            foreach (Match m in Regex.Matches(wwwAuthenticateValue, @"(\w+)\s*=\s*(""[^""]*""|[^\s,]+)", RegexOptions.IgnoreCase))
            {
                string key = m.Groups[1].Value.Trim().ToLower();
                string value = m.Groups[2].Value.Trim().Trim('"');

                switch (key)
                {
                    case "realm": challenge.Realm = value; break;
                    case "nonce": challenge.Nonce = value; break;
                    case "algorithm": challenge.Algorithm = EnumParser.StringToEnum<RTSPAuthDigestAlgorithmEnum>(value.Replace("-", "")); break;
                    case "qop": challenge.Qop = value; break;
                    case "opaque": challenge.Opaque = value; break;
                        // 필요시 추가 필드 처리
                }
            }

            return challenge;
        }
    }
}
