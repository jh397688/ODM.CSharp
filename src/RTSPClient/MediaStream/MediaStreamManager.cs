using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream
{
    internal class MediaStreamManager
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; internal set; }
        private RTSPoverEnum _rtspOver;
        private int _rtpChannel;
        private int _rtcpChannel;

        public MediaStreamManager(RTSPoverEnum rTSPOver, RTSPTrackTypeEnum rtspTrackType, int rtpChannel, int rtcpChannel)
        {
            this._rtspOver = rTSPOver;
            this.RTSPTrackType = rtspTrackType;
            this._rtpChannel = rtpChannel;
            this._rtcpChannel = rtcpChannel;
        }

        internal bool Close()
        {
            return true;
        }
    }
}
