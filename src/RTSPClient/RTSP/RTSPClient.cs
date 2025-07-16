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
    internal delegate void RTSPClientReceivedEventHandler(object packet);

    internal class RTSPClient : IDisposable
    {
        internal RTSPClientReceivedEventHandler RTPReceivedEvent;
        internal RTSPClientReceivedEventHandler RTCPReceivedEvent;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _recvCts;
        private Task _recvTask;
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
        }

        internal void SetAuth(string username, string password)
        {
            Username = username;
            Password = password;
        }

        internal void Connect()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(RTSPUri.Host, RTSPUri.Port > 0 ? RTSPUri.Port : 554);
            _stream = _tcpClient.GetStream();
        }

        internal void Play()
        {
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
                    string transportHeader = RTSPOver == RTSPoverEnum.RTSPoverTCP ?
                        "RTP/AVP/TCP;unicast;interleaved=0-1" : "RTP/AVP;unicast;client_port=5000-5001";

                    string setupResponse = SendSetupMethod(GetTrackUriFromSdp(response), transportHeader);
                    if (!IsResponseOK(setupResponse))
                        continue;

                    string playResponse = SendPlayMethod();
                    if (IsResponseOK(playResponse))
                    {
                        var types = _rtspAuthenticator.GetType().ToString().Split('.');

                        Console.WriteLine($"RTSP {types[types.Length - 1]} PLAY 성공");
                        return;
                    }
                }
                else if (IsUnauthorized(response, out var challenge))
                {
                    var suggestedAuth = SuggestAuthenticatorFromChallenge(challenge, Username, Password);
                    if (suggestedAuth == null)
                        continue;

                    response = SendDescribeMethod();
                    if (IsResponseOK(response))
                    {
                        string transportHeader = RTSPOver == RTSPoverEnum.RTSPoverTCP ? 
                            "RTP/AVP/TCP;unicast;interleaved=0-1" : "RTP/AVP;unicast;client_port=5000-5001";

                        string setupResponse = SendSetupMethod(GetTrackUriFromSdp(response), transportHeader);
                        if (!IsResponseOK(setupResponse))
                            continue;

                        string playResponse = SendPlayMethod();
                        if (IsResponseOK(playResponse))
                        {
                            var types = _rtspAuthenticator.GetType().ToString().Split('.');

                            Console.WriteLine($"RTSP {types[types.Length - 1]} PLAY 재시도 성공");
                            return;
                        }
                    }
                }
                // 이 인증자에서도 실패면 다음 인증자로 트라이
            }

            throw new Exception("RTSP PLAY: 모든 인증 방식 실패 또는 서버 접속 불가");
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

                        // 패킷 분배
                        if (channel % 2 == 0) // RTP(채널 0, 2, ...)
                        {
                            RTPReceivedEvent?.Invoke(payload);
                        }
                        else // RTCP(채널 1, 3, ...)
                        {
                            RTCPReceivedEvent?.Invoke(payload);
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

        // DESCRIBE 응답에서 SDP 분석해 트랙 URI 추출(실전은 좀 더 복잡)
        private string GetTrackUriFromSdp(string sdpResponse)
        {
            // 1. 트랙별 control:trackID=xxx 우선 사용
            var trackMatch = Regex.Match(sdpResponse, @"a=control:(trackID=\d+)", RegexOptions.IgnoreCase);
            if (trackMatch.Success)
                return $"{RTSPUri}/{trackMatch.Groups[1].Value}";

            // 2. 전체 control:rtsp로 시작 (절대 URI)도 지원
            var absMatch = Regex.Match(sdpResponse, @"a=control:(rtsp://[^\r\n]+)", RegexOptions.IgnoreCase);
            if (absMatch.Success)
                return absMatch.Groups[1].Value;

            // 3. a=control:* (글로벌) 이면 원본 URI 사용
            var globalMatch = Regex.Match(sdpResponse, @"a=control:\*", RegexOptions.IgnoreCase);
            if (globalMatch.Success)
                return RTSPUri.ToString();

            // 4. 그 외에는 Content-Base 등도 조합 가능
            return RTSPUri.ToString();
        }

        // 인증 방식 제안에 따라 인증자 선택
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
        #endregion

        public void Dispose()
        {
            
        }
    }
}
