using RTSPStream.MediaStream.RTP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTP.Packet
{
    internal class RTPCombinePacket
    {
        public byte[] FullFrame;
        public byte[] SpsPpsSegment;
        public FrameTypeEnum FrameType;

        public RTPCombinePacket(byte[] fullFrame, byte[] spsPpsSegment, FrameTypeEnum frameType)
        {
            this.FullFrame = fullFrame;
            this.SpsPpsSegment = spsPpsSegment;
            this.FrameType = frameType;
        }
    }
}
