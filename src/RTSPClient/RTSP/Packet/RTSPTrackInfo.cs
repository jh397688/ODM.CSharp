using RTSPStream.RTSP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Packet
{
    internal abstract class RTSPTrackInfoBase
    {
        public RTSPTrackTypeEnum RTSPTrackType { get; protected set; }
        public string Codec { get; protected set; }
        public int TrackId { get; protected set; }
        public string ControlUrl { get; protected set; }
        public int RTPInterleaved { get; protected set; }
        public int RTCPInterleaved { get; protected set; }

        protected RTSPTrackInfoBase(RTSPTrackTypeEnum rtspTrackType, string codec, int trackId, string controlUrl)
        {
            RTSPTrackType = rtspTrackType;
            Codec = codec;
            TrackId = trackId;
            ControlUrl = controlUrl;
        }

        internal void SetInterleaved(int rtpInterleaved, int rtcpInterleaved)
        {
            RTPInterleaved = rtpInterleaved;
            RTCPInterleaved = rtcpInterleaved;
        }
    }
    
    internal class RTSPTrackInfoList : List<RTSPTrackInfoBase>
    {

    }

    internal class RTSPVideoTrackInfo : RTSPTrackInfoBase
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public string FormatParams { get; set; } // fmtp 등 SDP 부가정보

        public RTSPVideoTrackInfo(string codec, int trackId, string controlUrl, int width, int height, double frameRate, string fmtp = null)
            : base(RTSPTrackTypeEnum.Video, codec, trackId, controlUrl)
        {
            Width = width;
            Height = height;
            FrameRate = frameRate;
            FormatParams = fmtp;
        }
    }

    internal class RTSPAudioTrackInfo : RTSPTrackInfoBase
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }

        public RTSPAudioTrackInfo(string codec, int trackId, string controlUrl, int sampleRate, int channels)
            : base(RTSPTrackTypeEnum.Audio, codec, trackId, controlUrl)
        {
            SampleRate = sampleRate;
            Channels = channels;
        }
    }

    internal class RTSPApplicationTrackInfo : RTSPTrackInfoBase
    {
        public string ApplicationType { get; set; }

        public RTSPApplicationTrackInfo(string codec, int trackId, string controlUrl, string applicationType)
            : base(RTSPTrackTypeEnum.Application, codec, trackId, controlUrl)
        {
            ApplicationType = applicationType;
        }
    }
} 