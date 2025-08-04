using RTSPStream.MediaStream.RTP.Packet;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib.EventBusData
{
    internal class RTPCombinePacketReceivedEventData
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; internal set; }
        public RTPCombinePacket RTPCombinePacket { get; private set; }

        public RTPCombinePacketReceivedEventData(RTSPTrackTypeEnum rtspTackType, RTPCombinePacket rtpCombinePacket)
        {
            RTSPTrackType = rtspTackType;
            RTPCombinePacket = rtpCombinePacket;
        }
    }
}
