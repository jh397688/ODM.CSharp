using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib.EventBusData
{
    internal class RTSPClietnReceivedEventData
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; internal set; }
        public byte[] Buffer { get; private set; }
        public int Channel { get; private set; }

        public RTSPClietnReceivedEventData(RTSPTrackTypeEnum rtspTackType, byte[] buffer, int channel)
        {
            RTSPTrackType = rtspTackType;
            Buffer = buffer;
            Channel = channel;
        }
    }
}
