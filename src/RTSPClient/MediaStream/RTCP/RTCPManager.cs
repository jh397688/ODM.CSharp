using RTSPStream.Lib;
using RTSPStream.Lib.EventBusData;
using RTSPStream.MediaStream.RTP.Packet;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTCP
{
    internal class RTCPManager
    {
        private RTSPTrackTypeEnum _rtspTrackType;
        private EventBus _eventBus;
        private int _channel;

        public RTCPManager(EventBus eventBus, RTSPTrackTypeEnum rtspTrackType, int channel)
        {
            _eventBus = eventBus;
            _channel = channel;
            _rtspTrackType = rtspTrackType;


            SetEventBus();
        }

        private void SetEventBus()
        {
            _eventBus.Subscribe<RTPPacketReceivedEventData>(RTPPacketReceivedEvent);
        }

        internal void RTCPReceived(byte[] payload)
        {
            
        }

        private void RTPPacketReceivedEvent(RTPPacketReceivedEventData data)
        {
            throw new NotImplementedException();
        }
    }
}
