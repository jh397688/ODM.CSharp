using RTSPStream.Lib;
using RTSPStream.Lib.EventBusData;
using RTSPStream.MediaStream.RTP.Packet;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTP
{
    internal class RTPManager
    {
        private RTSPTrackTypeEnum _rtspTrackType;
        private EventBus _eventBus;
        private int _channel;

        private RTPCombineManager _rtpCombineManager;

        public RTPManager(EventBus eventBus, RTSPTrackTypeEnum rtspTrackType, int channel)
        {
            this._rtspTrackType = rtspTrackType;
            this._eventBus = eventBus;
            this._channel = channel;

            _rtpCombineManager = new RTPCombineManager();
        }

        internal void RTPReceived(byte[] payload)
        {
            var rtpPacket = _rtpCombineManager.SetAddBuffer(payload);
            if (rtpPacket != null) _eventBus.Publish(new RTPPacketReceivedEventData(_rtspTrackType, rtpPacket));


            var rtpCombinePacket = _rtpCombineManager.GetRTPCombineBuffer();
            if (rtpCombinePacket != null) _eventBus.Publish(new RTPCombinePacketReceivedEventData(_rtspTrackType, rtpCombinePacket));
        }
    }
}
