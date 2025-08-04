using RTSPStream.Lib;
using RTSPStream.Lib.EventBusData;
using RTSPStream.MediaStream.RTCP;
using RTSPStream.MediaStream.RTP;
using RTSPStream.MediaStream.RTP.Packet;
using RTSPStream.RTSP;
using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream
{
    internal class MediaStreamManager
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; internal set; }

        private EventBus _eventBus;
        private RTSPoverEnum _rtspOver;
        private int _rtpChannel;
        private int _rtcpChannel;
        private RTPManager _rtpManager;
        private RTCPManager _rtcpManager;

        public MediaStreamManager(EventBus _eventBus, RTSPoverEnum rTSPOver, RTSPTrackTypeEnum rtspTrackType, int rtpChannel, int rtcpChannel)
        {
            this._eventBus = _eventBus;
            this._rtspOver = rTSPOver;
            this.RTSPTrackType = rtspTrackType;
            this._rtpChannel = rtpChannel;
            this._rtcpChannel = rtcpChannel;

            this._rtpManager = new RTPManager(_eventBus, RTSPTrackType, _rtpChannel);
            this._rtcpManager = new RTCPManager(_eventBus, RTSPTrackType, _rtcpChannel);

            if (this._rtspOver == RTSPoverEnum.RTSPoverUDP)
                SetUdpClient();

            SetEventBus();
        }

        private void SetEventBus()
        {
            this._eventBus.Subscribe<RTSPClietnReceivedEventData>(InterleavedPacketEvent);
        }

        private void InterleavedPacketEvent(RTSPClietnReceivedEventData data)
        {
            if (data.RTSPTrackType != this.RTSPTrackType)
                return;

            if (data.Channel == _rtpChannel) _rtpManager.RTPReceived(data.Buffer);

            if (data.Channel == _rtcpChannel) _rtcpManager.RTCPReceived(data.Buffer);
        }

        internal bool Close()
        {
            return true;
        }

        private void SetUdpClient()
        {
            throw new NotImplementedException();
        }

        private void RTPCombineBufferStream(RTPCombinePacket rtpCombinePacket)
        {
            
        }
    }
}