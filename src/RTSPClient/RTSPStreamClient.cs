using RTSPStream.MediaStream;
using RTSPStream.RTSP;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream
{
    public class RTSPStreamClient : IDisposable
    {
        RTSPClient _rtspClient;
        List<MediaStreamManager> _mediaStreamManagerList;
        public List<RTSPTrackTypeEnum> RTSPTrackTypeList { get; private set; }

        public RTSPStreamClient(string rtspUri, RTSPoverEnum rtspOver)
        {
            _rtspClient = new RTSPClient(new Uri(rtspUri), rtspOver);
            _rtspClient.RTPReceivedEvent += RTPReceived;
            _rtspClient.RTCPReceivedEvent += RTCPReceived;
            _mediaStreamManagerList = new List<MediaStreamManager>();

            RTSPTrackTypeList = new List<RTSPTrackTypeEnum>();
        }

        public void SetAuth(string username, string password)
        {
            _rtspClient.SetAuth(username, password);
        }

        public void Connect()
        {
            RTSPTrackTypeList = _rtspClient.Connect();
        }

        public void Play(RTSPTrackTypeEnum rtspTrackType)
        {
            if (RTSPTrackTypeList.Find(type => type == rtspTrackType) != RTSPTrackTypeEnum.None)
            {
                _rtspClient.Play(rtspTrackType);
                _rtspClient.StartReceiveAsync();
            }
        }

        private void RTPReceived(RTSPTrackTypeEnum rtspTrackTypeEnum, byte[] payload)
        {
            Console.WriteLine($"RTP Received {rtspTrackTypeEnum.ToString()} payload Length {payload.Length}");
        }

        private void RTCPReceived(RTSPTrackTypeEnum rtspTrackTypeEnum, byte[] payload)
        {
            Console.WriteLine($"RTCP Received {rtspTrackTypeEnum.ToString()} payload Length {payload.Length}");
        }

        public void Dispose()
        {
            _rtspClient?.Dispose();
        }
    }
}
