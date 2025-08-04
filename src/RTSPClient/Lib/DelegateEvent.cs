using RTSPStream.MediaStream.RTP.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib
{
    internal delegate void RTPPacketStreamEventHandler(RTPPacket rtpPacket);
    internal delegate void RTPCombinePacketStreamEventHandler(RTPCombinePacket rtpCombinePacket);
}
