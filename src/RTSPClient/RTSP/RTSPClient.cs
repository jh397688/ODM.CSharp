using RTSPStream.RTSP.Authenticator;
using RTSPStream.RTSP.Authenticator.Impl;
using RTSPStream.RTSP.Enum;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RTSPStream.RTSP
{
    internal delegate void RTSPClientReceivedEventHandler(RTSPTrackTypeEnum rtspTrackTypeEnum, byte[] payload);

    internal class RTSPClient : IDisposable
    {
        internal RTSPClientReceivedEventHandler RTPReceivedEvent;
        internal RTSPClientReceivedEventHandler RTCPReceivedEvent;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _recvCts;
        private Task _recvTask;
        private List<RTSPTrackInfoBase> _rtpsTrackInfoList;
        private int _cseq = 1;
        private string _sessionId;

        IRTSPAuthenticator _rtspAuthenticator;
        public Uri RTSPUri { get; private set; }
        public RTSPoverEnum RTSPOver { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }

        public RTSPClient(Uri rtspUri, RTSPoverEnum rtspOver)
        {
            RTSPUri = rtspUri;
            RTSPOver = rtspOver;
            _rtpsTrackInfoList = new List<RTSPTrackInfoBase>();
        }

        internal void SetAuth(string username, string password)
        {
            Username = username;
            Password = password;
        }

        internal List<RTSPTrackTypeEnum> Connect()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(RTSPUri.Host, RTSPUri.Port > 0 ? RTSPUri.Port : 554);
            _stream = _tcpClient.GetStream();

            var authTrials = new List<Func<IRTSPAuthenticator>>
            {
                () =>
                    new AnonymousAuthenticator(),
                () =>
                {
                    var auth = new BasicAuthenticator();
                    auth.SetCredential(Username, Password);
                    return auth;
                },
                () =>
                {
                    var auth = new DigestMD5Authenticator();
                    auth.SetCredential(Username, Password);
                    return auth;
                },
                () =>
                {
                    var auth = new DigestSHA256Authenticator();
                    auth.SetCredential(Username, Password);
                    return auth;
                }
            };

            foreach (var authFactory in authTrials)
            {
                _rtspAuthenticator = authFactory();

                string response = SendDescribeMethod();

                if (IsResponseOK(response))
                {
                    return ParseSdpAndAddTracks(response);
                }
                else if (IsUnauthorized(response, out var challenge))
                {
                    var suggestedAuth = SuggestAuthenticatorFromChallenge(challenge, Username, Password);
                    if (suggestedAuth == null)
                        continue;

                    response = SendDescribeMethod();
                    if (IsResponseOK(response))
                    {
                        return ParseSdpAndAddTracks(response);
                    }
                }
            }

            throw new Exception("RTSP Connect: 모든 인증 방식 실패 또는 서버 접속 불가");
        }

        internal void Play(RTSPTrackTypeEnum rtspTrackType)
        {
            var info = _rtpsTrackInfoList.Find(info =>  info.TrackType == rtspTrackType);

            string transportHeader = RTSPOver == RTSPoverEnum.RTSPoverTCP ? "RTP/AVP/TCP;unicast;interleaved=0-1" : "RTP/AVP;unicast;client_port=5000-5001";

            string setupResponse = SendSetupMethod(info.ControlUrl, transportHeader);
            if (!IsResponseOK(setupResponse))
                return;

            var pair = ParseInterleavedFromResponse(setupResponse);
            if (pair != null)
                info.SetInterleaved(pair.Value.rtp, pair.Value.rtcp);
            else
                return;

            string playResponse = SendPlayMethod();
            if (IsResponseOK(playResponse))
            {
                var types = _rtspAuthenticator.GetType().ToString().Split('.');

                Console.WriteLine($"RTSP {types[types.Length - 1]} {rtspTrackType.ToString()} PLAY 성공");
                return;
            }
        }

        internal void StartReceiveAsync()
        {
            _recvCts = new CancellationTokenSource();
            _recvTask = Task.Run(() => ReceiveLoopAsync(_recvCts.Token));
        }

        internal async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            var ms = new MemoryStream();

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                if (bytesRead == 0)
                    break; // 연결 종료

                ms.Write(buffer, 0, bytesRead);

                while (ms.Length >= 4)
                {
                    ms.Position = 0;
                    byte[] header = new byte[4];
                    ms.Read(header, 0, 4);

                    if (header[0] == 0x24) // '$'
                    {
                        int channel = header[1];
                        int length = (header[2] << 8) | header[3];

                        if (ms.Length - 4 < length)
                        {
                            ms.Position = ms.Length; // 데이터 부족, 다음 수신까지 대기
                            break;
                        }

                        byte[] payload = new byte[length];
                        ms.Read(payload, 0, length);

                        var trackInfo = _rtpsTrackInfoList.FirstOrDefault(info => info.RTPInterleaved == channel || info.RTCPInterleaved == channel);
                        if (trackInfo != null)
                        {
                            if (channel == trackInfo.RTPInterleaved)
                                RTPReceivedEvent?.Invoke(trackInfo.TrackType, payload);
                            else if (channel == trackInfo.RTCPInterleaved)
                                RTCPReceivedEvent?.Invoke(trackInfo.TrackType, payload);
                        }


                        // 스트림에서 읽은 만큼 버리기
                        byte[] remain = ms.ToArray().Skip((int)ms.Position).ToArray();
                        ms.SetLength(0);
                        ms.Position = 0;
                        ms.Write(remain, 0, remain.Length);
                        ms.Position = 0;
                    }
                    else
                    {
                        string rtspAsnc = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"RTSPAsnc : {rtspAsnc}");
                        // RTSP 텍스트 패킷(명령 응답 등)일 수도 있음.
                        // (여기서는 RTP/RTCP만 처리, 명령 응답은 따로 처리 필요)
                        break;
                    }
                }
            }
        }

        internal void StopReceiveAsync()
        {
            _recvCts?.Cancel();
            _recvTask?.Wait();
        }

        #region SendMethod
        private string SendOptionsMethod()
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.OPTIONS,
                RTSPUri = RTSPUri
            };
            string request =
                $"OPTIONS {RTSPUri} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendDescribeMethod()
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.DESCRIBE,
                RTSPUri = RTSPUri
            };
            string request =
                $"DESCRIBE {RTSPUri} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n" +
                "Accept: application/sdp\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendSetupMethod(string trackUri, string transportHeader)
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.SETUP,
                RTSPUri = RTSPUri
            };
            string request =
                $"SETUP {trackUri} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n" +
                $"Transport: {transportHeader}\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendPlayMethod()
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.PLAY,
                RTSPUri = RTSPUri
            };
            string request =
                $"PLAY {RTSPUri.ToString()} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendPauseMethod()
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.PAUSE,
                RTSPUri = RTSPUri
            };
            string request =
                $"PAUSE {RTSPUri} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendTeardownMethod()
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.TEARDOWN,
                RTSPUri = RTSPUri
            };
            string request =
                $"TEARDOWN {RTSPUri} RTSP/1.0\r\n" +
                $"CSeq: {_cseq++}\r\n" +
                "User-Agent: RTSPStream/1.0\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, null);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendAndReceive(string request)
        {
            Console.WriteLine($"request : {request}\n");

            byte[] buffer = Encoding.ASCII.GetBytes(request);
            _stream.Write(buffer, 0, buffer.Length);

            var responseBuffer = new byte[4096];
            int bytesRead = _stream.Read(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

            // Session ID 추출 (SETUP 후에만)
            if (request.StartsWith("SETUP", StringComparison.OrdinalIgnoreCase))
            {
                var sessionHeader = Regex.Match(response, @"Session:\s*([^\r\n;]+)", RegexOptions.IgnoreCase);
                if (sessionHeader.Success)
                    _sessionId = sessionHeader.Groups[1].Value.Trim();
            }

            Console.WriteLine($"response : {response}\n");

            return response;
        }
        #endregion

        #region ReceiveParser
        private bool IsResponseOK(string response)
        {
            return response.StartsWith("RTSP/1.0 200", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUnauthorized(string response, out RTSPAuthChallenge challenge)
        {
            challenge = null;

            if (response.StartsWith("RTSP/1.0 401", StringComparison.OrdinalIgnoreCase))
            {
                // WWW-Authenticate 헤더 파싱
                var match = Regex.Match(response, @"WWW-Authenticate:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    challenge = RtspAuthChallengeParser.Parse(match.Groups[1].Value);
                    return true;
                }
            }
            return false;
        }

        private List<RTSPTrackTypeEnum> ParseSdpAndAddTracks(string sdp)
        {
            var result = new List<RTSPTrackTypeEnum>(); ;

            var lines = sdp.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            int trackIdSeed = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("m=", StringComparison.OrdinalIgnoreCase))
                {
                    // 트랙 정보 임시 변수
                    string mediaType = null;
                    string codec = null;
                    int? sampleRate = null, channels = null;
                    int? width = null, height = null;
                    double? frameRate = null;
                    string fmtp = null, control = null, appType = null;
                    int payloadType = -1;

                    // m=video 0 RTP/AVP 96
                    var mParts = lines[i].Substring(2).Split(' ');
                    mediaType = mParts[0].Trim();

                    // payloadType은 보통 마지막 값
                    if (mParts.Length > 3 && int.TryParse(mParts[3], out int pt))
                        payloadType = pt;

                    // 다음 "m=" 전까지의 SDP 파싱
                    int j = i + 1;
                    for (; j < lines.Length && !lines[j].StartsWith("m=", StringComparison.OrdinalIgnoreCase); j++)
                    {
                        var line = lines[j];
                        if (line.StartsWith("a=rtpmap:", StringComparison.OrdinalIgnoreCase))
                        {
                            // a=rtpmap:96 H264/90000 or a=rtpmap:97 PCMU/8000/1
                            var map = line.Substring("a=rtpmap:".Length).Split(' ');
                            if (map.Length == 2)
                            {
                                var rtpmapTokens = map[1].Split('/');
                                codec = rtpmapTokens[0];
                                if (rtpmapTokens.Length > 1 && int.TryParse(rtpmapTokens[1], out int sr))
                                    sampleRate = sr;
                                if (rtpmapTokens.Length > 2 && int.TryParse(rtpmapTokens[2], out int ch))
                                    channels = ch;
                            }
                        }
                        else if (line.StartsWith("a=fmtp:", StringComparison.OrdinalIgnoreCase))
                        {
                            fmtp = line.Substring("a=fmtp:".Length).Trim();
                        }
                        else if (line.StartsWith("a=framesize:", StringComparison.OrdinalIgnoreCase))
                        {
                            // a=framesize:96 1920-1080
                            var tokens = line.Split(' ');
                            if (tokens.Length == 2)
                            {
                                var size = tokens[1].Split('-');
                                if (size.Length == 2 && int.TryParse(size[0], out int w) && int.TryParse(size[1], out int h))
                                {
                                    width = w;
                                    height = h;
                                }
                            }
                        }
                        else if (line.StartsWith("a=framerate:", StringComparison.OrdinalIgnoreCase))
                        {
                            // a=framerate:30
                            var val = line.Substring("a=framerate:".Length).Trim();
                            if (double.TryParse(val, out double fr))
                                frameRate = fr;
                        }
                        else if (line.StartsWith("a=control:", StringComparison.OrdinalIgnoreCase))
                        {
                            // a=control:trackID=1
                            control = line.Substring("a=control:".Length).Trim();
                            // trackID 추출
                            var tid = ExtractTrackIdFromControl(control);
                            if (tid != null)
                                trackIdSeed = tid.Value;
                        }
                        else if (line.StartsWith("a=rtpmap:", StringComparison.OrdinalIgnoreCase) && mediaType == "application")
                        {
                            // application은 rtpmap 값이 타입인 경우가 있음 (예: vnd.onvif.metadata)
                            var map = line.Substring("a=rtpmap:".Length).Split(' ');
                            if (map.Length == 2)
                                appType = map[1].Split('/')[0];
                        }
                    }

                    // 트랙 타입별 객체 생성
                    switch (mediaType.ToLower())
                    {
                        case "video":
                            result.Add(RTSPTrackTypeEnum.Video);

                            _rtpsTrackInfoList.Add(new RTSPVideoTrackInfo(
                                codec ?? "unknown",
                                trackIdSeed,
                                control ?? $"trackID={trackIdSeed}",
                                width ?? 0,
                                height ?? 0,
                                frameRate ?? 0,
                                fmtp
                            ));
                            break;
                        case "audio":
                            result.Add(RTSPTrackTypeEnum.Audio);

                            _rtpsTrackInfoList.Add(new RTSPAudioTrackInfo(
                                codec ?? "unknown",
                                trackIdSeed,
                                control ?? $"trackID={trackIdSeed}",
                                sampleRate ?? 0,
                                channels ?? 1
                            ));
                            break;
                        case "application":
                            result.Add(RTSPTrackTypeEnum.Application);

                            _rtpsTrackInfoList.Add(new RTSPApplicationTrackInfo(
                                codec ?? "unknown",
                                trackIdSeed,
                                control ?? $"trackID={trackIdSeed}",
                                appType ?? "unknown"
                            ));
                            break;
                            // TODO: text/image/data 등 추가 가능
                    }

                    // 다음 m= 라인에서 이어서 탐색
                    i = j - 1;
                    trackIdSeed++;
                }
            }

            return result;
        }

        private int? ExtractTrackIdFromControl(string control)
        {
            if (string.IsNullOrEmpty(control)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(control, @"trackID\s*=\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int tid))
                return tid;
            return null;
        }

        private IRTSPAuthenticator SuggestAuthenticatorFromChallenge(RTSPAuthChallenge challenge, string username, string password)
        {
            if (challenge.Type == RTSPAuthTypeEnum.Digest)
            {
                if (challenge.Algorithm == RTSPAuthDigestAlgorithmEnum.MD5)
                {
                    var a = new DigestMD5Authenticator(); a.SetCredential(username, password); return a;
                }
                else if (challenge.Algorithm == RTSPAuthDigestAlgorithmEnum.SHA256)
                {
                    var a = new DigestSHA256Authenticator(); a.SetCredential(username, password); return a;
                }
            }
            else if (challenge.Type == RTSPAuthTypeEnum.Basic)
            {
                var a = new BasicAuthenticator(); a.SetCredential(username, password); return a;
            }

            return null;
        }

        private (int rtp, int rtcp)? ParseInterleavedFromResponse(string response)
        {
            // Transport 헤더만 추출
            var lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string transportLine = lines.FirstOrDefault(l => l.StartsWith("Transport:", StringComparison.OrdinalIgnoreCase));
            if (transportLine == null) return null;

            // 정규식 파싱
            var match = Regex.Match(transportLine, @"interleaved\s*=\s*(\d+)-(\d+)");
            if (match.Success)
            {
                int rtp = int.Parse(match.Groups[1].Value);
                int rtcp = int.Parse(match.Groups[2].Value);
                return (rtp, rtcp);
            }
            return null;
        }
        #endregion

        public void Dispose()
        {
            
        }
    }
}
