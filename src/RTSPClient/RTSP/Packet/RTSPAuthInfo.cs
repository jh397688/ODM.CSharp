using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Packet
{
    internal class RtspAuthInfo
    {// <summary>
        /// 인증 타입: "Basic", "Digest" 등
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Realm(보안영역, 인증문자열)
        /// </summary>
        public string? Realm { get; set; }

        /// <summary>
        /// Digest 인증용 Nonce (서버에서 임의로 발급)
        /// </summary>
        public string? Nonce { get; set; }

        /// <summary>
        /// 해시 알고리즘명 (MD5, SHA-256 등)
        /// </summary>
        public string? Algorithm { get; set; }

        /// <summary>
        /// (Optional) qop, opaque 등 추가 파라미터도 필요시 확장 가능
        /// </summary>
        public string? Qop { get; set; }
        public string? Opaque { get; set; }
    }
}
