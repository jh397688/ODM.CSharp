using RTSPStream.MediaStream.RTCP;
using RTSPStream.MediaStream.RTP;
using RTSPStream.RTSP;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        private RTPManager _rtpManager;
        private RTCPManager _rtcpManager;

        public MediaStreamManager(RTSPoverEnum rTSPOver, RTSPTrackTypeEnum rtspTrackType, int rtpChannel, int rtcpChannel)
        {
            this._rtspOver = rTSPOver;
            this.RTSPTrackType = rtspTrackType;
            this._rtpChannel = rtpChannel;
            this._rtcpChannel = rtcpChannel;

            if (this._rtspOver == RTSPoverEnum.RTSPoverUDP)
                SetUdpClient();
        }

        private void SetUdpClient()
        {
            throw new NotImplementedException();
        }

        public void RTPReceived(RTSPTrackTypeEnum rtspTrackType, int channel, byte[] payload)
        {
            if (this.RTSPTrackType != rtspTrackType)
                return;

            Console.WriteLine($"RTP Received : {rtspTrackType.ToString()}, channel : {channel}, payload Length : {payload.Length}");
        }

        public void RTCPReceived(RTSPTrackTypeEnum rtspTrackType, int channel, byte[] payload)
        {
            if (this.RTSPTrackType != rtspTrackType)
                return;

            Console.WriteLine($"RTCP Received : {rtspTrackType.ToString()}, channel : {channel}, payload Length : {payload.Length}");
        }

        internal bool Close()
        {
            return true;
        }
    }
}
