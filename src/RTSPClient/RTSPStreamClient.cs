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

            _mediaStreamManagerList.Clear();
            _rtspClient.DisConnect();
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
                    var mediaStreamManager = new MediaStreamManager(RTSPOver, rtspTrackType, rtpChannel, rtcpChannel);
                    _rtspClient.RTPReceivedEvent += mediaStreamManager.RTPReceived;
                    _rtspClient.RTCPReceivedEvent += mediaStreamManager.RTCPReceived;


                    _mediaStreamManagerList.Add(mediaStreamManager);
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
                result = _rtspClient.Stop(rtspTrackType);

                if (result)
                {
                    result = manager.Close();
                    _mediaStreamManagerList.Remove(manager);
                }
            }

            return result;
        }        

        public void Dispose()
        {
            _rtspClient?.Dispose();
        }
    }
}
