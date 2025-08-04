using RTSPStream.Lib;
using RTSPStream.Lib.EventBusData;
using RTSPStream.MediaStream;
using RTSPStream.RTSP;
using RTSPStream.RTSP.Enum;
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
        public Uri RTSPUri { get; private set; }
        public RTSPoverEnum RTSPOver { get; private set; }
        public List<RTSPTrackTypeEnum> RTSPTrackTypeList { get; private set; }

        private EventBus _eventBus;
        private RTSPClient _rtspClient;
        private List<MediaStreamManager> _mediaStreamManagerList;

        public RTSPStreamClient(string rtspUri, RTSPoverEnum rtspOver)
        {
            RTSPUri = new Uri(rtspUri);
            RTSPOver = rtspOver;

            _eventBus = new EventBus();
            _rtspClient = new RTSPClient(_eventBus, RTSPUri, RTSPOver);
            _mediaStreamManagerList = new List<MediaStreamManager>();

            RTSPTrackTypeList = new List<RTSPTrackTypeEnum>();

            SetEventBus();
        }

        private void SetEventBus()
        {
            _eventBus.Subscribe<RTPPacketReceivedEventData>(RTPPacketStreamEvent);
            _eventBus.Subscribe<RTPCombinePacketReceivedEventData>(RTPCombinePacketStreamEvent);
        }

        private void RTPPacketStreamEvent(RTPPacketReceivedEventData data)
        {
            Console.WriteLine($"RTPPacketStreamEvent RTSPTrackType : {data.RTSPTrackType} Length : {data.RTPPacket.OriginData.Length}");
        }

        private void RTPCombinePacketStreamEvent(RTPCombinePacketReceivedEventData data)
        {
            Console.WriteLine($"RTPCombinePacketStreamEvent RTSPTrackType : {data.RTSPTrackType} FrameType : {data.RTPCombinePacket.FrameType.ToString()}");
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
                    _mediaStreamManagerList.Add(new MediaStreamManager(_eventBus, RTSPOver, rtspTrackType, rtpChannel, rtcpChannel));
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
