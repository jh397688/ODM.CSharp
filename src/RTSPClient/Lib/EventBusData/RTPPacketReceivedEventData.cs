using RTSPStream.MediaStream.RTP.Packet;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib.EventBusData
{
    internal class RTPPacketReceivedEventData
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; internal set; }
        public RTPPacket RTPPacket { get; private set; }

        public RTPPacketReceivedEventData(RTSPTrackTypeEnum rtspTackType, RTPPacket rtpPacket)
        {
            RTSPTrackType = rtspTackType;
            RTPPacket = rtpPacket;
        }
    }
}
