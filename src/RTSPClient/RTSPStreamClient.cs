using RTSPStream.MediaStream;
using RTSPStream.RTSP;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RTSPStream
{
    public class RTSPStreamClient : IDisposable
    {
        RTSPClient _rtspClient;
        List<MediaStreamManager> _mediaStreamManagerList;
        public Uri RTSPUri { get; private set; }
        public RTSPoverEnum RTSPOver { get; private set; }
        public List<RTSPTrackTypeEnum> RTSPTrackTypeList { get; private set; }

        public RTSPStreamClient(string rtspUri, RTSPoverEnum rtspOver)
        {
            RTSPUri = new Uri(rtspUri);
            RTSPOver = rtspOver;

            _rtspClient = new RTSPClient(RTSPUri, RTSPOver);
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

        public void DisConnect()
        {
            foreach (var manager in _mediaStreamManagerList)
            {
                _rtspClient.Stop(manager.RTSPTrackType);
            }
        }

        public bool Play(RTSPTrackTypeEnum rtspTrackType)
        {
            bool result = false;

            if (RTSPTrackTypeList.Find(type => type == rtspTrackType) != RTSPTrackTypeEnum.None &&
                _mediaStreamManagerList.Find(manager => manager.RTSPTrackType == rtspTrackType) == null)
            {
                result = _rtspClient.Play(rtspTrackType, out int rtpChannel, out int rtcpChannel);

                if (result)
                {
                    _mediaStreamManagerList.Add(new MediaStreamManager(RTSPOver, rtspTrackType, rtpChannel, rtcpChannel));
                }
            }

            return result;
        }

        public bool Stop(RTSPTrackTypeEnum rtspTrackType)
        {
            bool result = false;

            var manager = _mediaStreamManagerList.Find(manager => manager.RTSPTrackType == rtspTrackType);

            if (manager != null)
            {
                _rtspClient.Stop(rtspTrackType);
                result = manager.Close();
            }

            return result;
        }

        private void RTPReceived(RTSPTrackTypeEnum rtspTrackTypeEnum, int channel, byte[] payload)
        {
            Console.WriteLine($"RTP Received : {rtspTrackTypeEnum.ToString()}, channel : {channel} payload Length : {payload.Length}");
        }

        private void RTCPReceived(RTSPTrackTypeEnum rtspTrackTypeEnum, int channel, byte[] payload)
        {
            Console.WriteLine($"RTCP Received : {rtspTrackTypeEnum.ToString()}, channel : {channel} payload Length : {payload.Length}");
        }

        public void Dispose()
        {
            _rtspClient?.Dispose();
        }
    }
}
