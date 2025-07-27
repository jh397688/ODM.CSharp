using RTSPStream.Lib;
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
    internal delegate void RTSPClientReceivedEventHandler(RTSPTrackTypeEnum rtspTrackTypeEnum, int chanel, byte[] payload);

    internal class RTSPClient : IDisposable
    {
        internal RTSPClientReceivedEventHandler RTPReceivedEvent;
        internal RTSPClientReceivedEventHandler RTCPReceivedEvent;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _recvCts;
        private Task _recvTask;
        private CommonWaiter _commonWaiter;
        private List<RTSPTrackInfoBase> _rtpsTrackInfoList;
        private int _cseq = 1;
        private string _sessionId;

        IRTSPAuthenticator _rtspAuthenticator;
        RTSPAuthChallenge _rtspAuthChallenge;
        public Uri RTSPUri { get; private set; }
        public RTSPoverEnum RTSPOver { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }

        public RTSPClient(Uri rtspUri, RTSPoverEnum rtspOver)
        {
            RTSPUri = rtspUri;
            RTSPOver = rtspOver;
            _commonWaiter = new CommonWaiter();
            _rtpsTrackInfoList = new List<RTSPTrackInfoBase>();
        }

        internal void SetAuth(string username, string password)
        {
            Username = username;
            Password = password;
        }

        internal List<RTSPTrackTypeEnum> Connect()
        {
            InitTcpClient();
            StartReceiveLoopAsync();

            var authTrials = new List<IRTSPAuthenticator>
            {
                new AnonymousAuthenticator(), 
                new BasicAuthenticator(), 
                new DigestMD5Authenticator(), 
                new DigestSHA256Authenticator()
            };
            
            int cseq = 0;
            string response = string.Empty;

            foreach (var authFactory in authTrials)
            {
                _rtspAuthenticator = authFactory;

                if (_rtspAuthenticator.RTSPAuthType != RTSPAuthTypeEnum.None)
                    _rtspAuthenticator.SetCredential(Username, Password);

                if ((_rtspAuthenticator.RTSPAuthType == RTSPAuthTypeEnum.Basic || _rtspAuthenticator.RTSPAuthType == RTSPAuthTypeEnum.Digest) &&
                    IsUnauthorized(response, _rtspAuthenticator.RTSPAuthDigestAlgorithm, out _rtspAuthChallenge) == false)
                    continue;

                response = SendDescribeMethod();

                if (IsResponseOK(response, out cseq) && _cseq - 1 == cseq)
                {
                    return ParseSdpAndAddTracks(response);
                }
            }

            throw new Exception("RTSP Connect: 모든 인증 방식 실패 또는 서버 접속 불가");
        }

        private void InitTcpClient()
        {
            if (_tcpClient == null)
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(RTSPUri.Host, RTSPUri.Port > 0 ? RTSPUri.Port : 554);
                _stream = _tcpClient.GetStream();
            }
        }

        private void DeInitTcpClient()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        private void StartReceiveLoopAsync()
        {
            if (_recvCts == null)
            {
                _recvCts = new CancellationTokenSource();
                _recvTask = Task.Run(() => ReceiveLoopAsync(_recvCts.Token));
            }
        }

        private void StopReceiveLoopAsync()
        {
            if (_recvCts != null)
            {
                try
                {
                    _recvTask.Wait();
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {

                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    _recvCts.Dispose();
                    _recvCts = null;
                    _recvTask = null;
                }
            }
        }

        internal void DisConnect()
        {
            Dispose();
        }

        internal async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new List<byte>();
            var tmp = new byte[4096];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(tmp, 0, tmp.Length, token);
                if (bytesRead == 0) 
                    break;

                buffer.AddRange(tmp.Take(bytesRead));

                while (buffer.Count > 0)
                {
                    if (buffer[0] != 0x24)
                    {
                        var hdrEnd = FindSequence(buffer, Encoding.ASCII.GetBytes("\r\n\r\n"));
                        if (hdrEnd < 0) 
                            break;

                        var headerBytes = buffer.Take(hdrEnd + 4).ToArray();
                        var headerText = Encoding.UTF8.GetString(headerBytes);

                        int contentLength = 0;
                        var m = Regex.Match(headerText, @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
                        if (m.Success) contentLength = int.Parse(m.Groups[1].Value);

                        if (buffer.Count < hdrEnd + 4 + contentLength) 
                            break;

                        var bodyBytes = buffer.Skip(hdrEnd + 4).Take(contentLength).ToArray();
                        buffer.RemoveRange(0, hdrEnd + 4 + contentLength);

                        var fullResponse = Encoding.UTF8.GetString(headerBytes) +
                                           Encoding.UTF8.GetString(bodyBytes);

                        Console.WriteLine($"RTSPAsnc : {fullResponse}");

                        _commonWaiter.SetData(fullResponse);
                    }
                    else
                    {
                        if (buffer.Count < 4) 
                            break;

                        int channel = buffer[1];
                        int length = (buffer[2] << 8) | buffer[3];

                        if (buffer.Count < 4 + length) 
                            break;

                        var payload = buffer.Skip(4).Take(length).ToArray();
                        buffer.RemoveRange(0, 4 + length);

                        var track = _rtpsTrackInfoList.FirstOrDefault(info => channel == info.RTPInterleaved || channel == info.RTCPInterleaved);
                        if (track != null)
                        {
                            if (channel == track.RTPInterleaved)
                                RTPReceivedEvent?.Invoke(track.TrackType, channel, payload);
                            else
                                RTCPReceivedEvent?.Invoke(track.TrackType, channel, payload);
                        }
                    }
                }
            }
        }

        private static int FindSequence(List<byte> buffer, byte[] seq)
        {
            for (int i = 0; i <= buffer.Count - seq.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < seq.Length; j++)
                {
                    if (buffer[i + j] != seq[j]) { ok = false; break; }
                }
                if (ok) return i;
            }
            return -1;
        }


        internal bool Play(RTSPTrackTypeEnum rtspTrackType, out int rtpChannel, out int rtcpChannel)
        {
            rtpChannel = 0;
            rtcpChannel = 0;
            int cseq = 0;

            var info = _rtpsTrackInfoList.Find(info => info.TrackType == rtspTrackType);

            string transportHeader = RTSPOver == RTSPoverEnum.RTSPoverTCP ? "RTP/AVP/TCP;unicast;interleaved=0-1" : "RTP/AVP;unicast;client_port=5000-5001";

            string setupResponse = SendSetupMethod(info.ControlUrl, transportHeader);
            if (!IsResponseOK(setupResponse, out cseq) || _cseq - 1 != cseq)
                return false;

            var pair = ParseInterleavedFromResponse(setupResponse);
            if (pair != null)
                info.SetInterleaved(pair.Value.rtp, pair.Value.rtcp);
            else
                return false;

            string playResponse = SendPlayMethod();
            if (IsResponseOK(playResponse, out cseq) || _cseq - 1 != cseq)
            {
                rtpChannel = pair.Value.rtp;
                rtcpChannel = pair.Value.rtcp;

                return true;
            }
            else
                return false;
        }

        internal bool Stop(RTSPTrackTypeEnum rtspTrackType)
        {
            var info = _rtpsTrackInfoList.Find(info => info.TrackType == rtspTrackType);

            string setupResponse = SendTeardownMethod(info.ControlUrl);
            if (IsResponseOK(setupResponse, out int cseq) && _cseq - 1 == cseq)
            {

                return true;
            }
            else
                return false;
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

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
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

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
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
                $"User-Agent: RTSPStream/1.0\r\n" +
                $"Transport: {transportHeader}\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request, true);
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

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
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

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendTeardownMethod(string trackUri)
        {
            var reqInfo = new RTSPRequestInfo
            {
                RTSPMethod = RTSPMethodEnum.TEARDOWN,
                RTSPUri = RTSPUri
            };
            string request = $"TEARDOWN {trackUri} RTSP/1.0\r\n" +
                             $"CSeq: {_cseq++}\r\n" + 
                             $"User-Agent: RTSPStream/1.0\r\n";

            if (!string.IsNullOrEmpty(_sessionId))
                request += $"Session: {_sessionId}\r\n";

            string? authHeader = _rtspAuthenticator.GetAuthorizationHeader(reqInfo, _rtspAuthChallenge);
            if (!string.IsNullOrEmpty(authHeader))
                request += authHeader + "\r\n";

            request += "\r\n";
            return SendAndReceive(request);
        }

        private string SendAndReceive(string request, bool flag = false)
        {
            Console.WriteLine($"request : {request}\n");

            byte[] buffer = Encoding.ASCII.GetBytes(request);
            _stream.Write(buffer, 0, buffer.Length);

            string response = "";

            if (_commonWaiter.WaitForData(new TimeSpan(0, 0, 5), out response))
            {
                // Session ID 추출 (SETUP 후에만)
                if (request.StartsWith("SETUP", StringComparison.OrdinalIgnoreCase))
                {
                    var sessionHeader = Regex.Match(response, @"Session:\s*([^\r\n;]+)", RegexOptions.IgnoreCase);
                    if (sessionHeader.Success)
                        _sessionId = sessionHeader.Groups[1].Value.Trim();
                }
            }

            return response;
        }
        #endregion

        #region ReceiveParser
        private bool IsResponseOK(string response, out int cseq)
        {
            cseq = ParseCSeqFromResponse(response);
            return response.StartsWith("RTSP/1.0 200", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUnauthorized(string response, RTSPAuthDigestAlgorithmEnum rtspAuthDigestAlgorithm, out RTSPAuthChallenge challenge)
        {
            challenge = null;

            if (response.StartsWith("RTSP/1.0 401", StringComparison.OrdinalIgnoreCase))
            {
                // WWW-Authenticate 헤더 파싱
                var matches = Regex.Matches(response, @"WWW-Authenticate:\s*([^\r\n]+)", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    challenge = RtspAuthChallengeParser.Parse(match.Groups[1].Value);

                    if (challenge != null &&
                        challenge.Algorithm == rtspAuthDigestAlgorithm)
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

        private int ParseCSeqFromResponse(string response)
        {
            // Transport 헤더만 추출
            var lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string transportLine = lines.FirstOrDefault(l => l.StartsWith("CSeq:", StringComparison.OrdinalIgnoreCase));
            if (transportLine == null) return 0;

            // 정규식 파싱
            var match = Regex.Match(transportLine, @"CSeq: (\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }

            return 0;
        }
        #endregion

        public void Dispose()
        {
            StopReceiveLoopAsync();
            DeInitTcpClient();
        }
    }
}
