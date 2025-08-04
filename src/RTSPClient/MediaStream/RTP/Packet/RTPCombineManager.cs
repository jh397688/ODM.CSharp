using RTSPStream.MediaStream.RTP.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTP.Packet
{
    internal class RTPCombineManager : IDisposable
    {
        readonly byte[] StartCode = { 0, 0, 0, 1 };

        byte[] SPSPPSSegment;

        byte[] SPSData = null;
        byte[] PPSData = null;
        List<byte[]> RTPBuffer;
        Queue<RTPCombinePacket> RTPCombinePacket;

        public RTPCombineManager()
        {
            SetProperty();
        }

        private void SetProperty()
        {
            RTPBuffer = new List<byte[]>();
            RTPCombinePacket = new Queue<RTPCombinePacket>();
        }

        public RTPPacket SetAddBuffer(byte[] buffer)
        {
            var rtppacket = new RTPPacket(buffer);

            if (rtppacket.IsRTPPacket)
            {
                byte nalUnitType = (byte)(rtppacket.PayloadData[0] & 0x1F);

                switch (nalUnitType)
                {
                    case 7: // SPS
                        ProcessSPSData(rtppacket);
                        break;
                    case 8: // PPS
                        ProcessPPSData(rtppacket);
                        break;
                    case 28: // FU-A
                        ProcessFUA(rtppacket);
                        break;
                    default: // 기타 NAL Unit
                        ProcessOtherNALUnits(rtppacket);
                        break;
                }

                if (rtppacket.Marker)
                {
                    byte[] completeFrame = CombineBuffer();
                    ProcessFrame(completeFrame);

                    RTPBuffer.Clear();
                }

                return rtppacket;
            }

            return null;
        }

        private void ProcessSPSData(RTPPacket rtppacket)
        {
            SPSData = rtppacket.PayloadData;
            
            TryBuildSpsPpsSegment();
        }

        private void ProcessPPSData(RTPPacket rtppacket)
        {
            PPSData = rtppacket.PayloadData;

            TryBuildSpsPpsSegment();
        }

        /*
        private void ProcessFUA(RTPPacket rtppacket)
        {
            byte fuHeader = rtppacket.PayloadData[1];
            bool startBit = (fuHeader & 0x80) != 0;
            bool endBit = (fuHeader & 0x40) != 0;

            if (startBit)
            {
                byte reconstructedHeader = (byte)((rtppacket.PayloadData[0] & 0xE0) | (rtppacket.PayloadData[1] & 0x1F));

                RTPBuffer.Add(new byte[] { reconstructedHeader });
                RTPBuffer.Add(rtppacket.PayloadData.Skip(2).ToArray()); // 첫 패킷의 헤더 다음 부분 추가
            }
            else
            {
                RTPBuffer.Add(rtppacket.PayloadData.Skip(2).ToArray()); // FU-A의 나머지 조각 추가
            }

            if (endBit)
            {
                // FU-A의 끝을 표시 (필요 시 추가 로직)
            }
        }
        */
        
        private void ProcessFUA(RTPPacket rtppacket)
        {
            byte fuHeader = rtppacket.PayloadData[1];
            bool startBit = (fuHeader & 0x80) != 0;
            bool endBit = (fuHeader & 0x40) != 0;

            byte[] skipPlayData = new byte[rtppacket.PayloadData.Length - 2];

            if (startBit)
            {
                byte reconstructedHeader = (byte)((rtppacket.PayloadData[0] & 0xE0) | (rtppacket.PayloadData[1] & 0x1F));

                RTPBuffer.Add(new byte[] { reconstructedHeader });
            }


            Array.Copy(rtppacket.PayloadData, 2, skipPlayData, 0, rtppacket.PayloadData.Length - 2);
            RTPBuffer.Add(skipPlayData);

            if (endBit)
            {
                // FU-A의 끝을 표시 (필요 시 추가 로직)
            }
        }

        private void TryBuildSpsPpsSegment()
        {
            if (SPSData == null || PPSData == null) return;

            int len = StartCode.Length + SPSData.Length + StartCode.Length + PPSData.Length;
            SPSPPSSegment = new byte[len];

            int off = 0;

            Array.Copy(StartCode, 0, SPSPPSSegment, off, StartCode.Length); off += StartCode.Length;
            Array.Copy(SPSData, 0, SPSPPSSegment, off, SPSData.Length); off += SPSData.Length;
            Array.Copy(StartCode, 0, SPSPPSSegment, off, StartCode.Length); off += StartCode.Length;
            Array.Copy(PPSData, 0, SPSPPSSegment, off, PPSData.Length);
        }

        private void ProcessOtherNALUnits(RTPPacket rtppacket)
        {
            RTPBuffer.Add(rtppacket.PayloadData);
        }

        private byte[] CombineBuffer()
        {
            int totalLength = RTPBuffer.Sum(p => p.Length);
            byte[] completeFrame = new byte[totalLength];
            int offset = 0;

            foreach (var payload in RTPBuffer)
            {
                Array.Copy(payload, 0, completeFrame, offset, payload.Length);
                offset += payload.Length;
            }

            return completeFrame;
        }

        private void ProcessFrame(byte[] completeFrame)
        {
            if (SPSPPSSegment == null) return;

            FrameTypeEnum frameType = GetFrameTypeFromNAL(completeFrame);

            byte[] fullFrame = null;
            int offset = 0;

            if (frameType == FrameTypeEnum.IFrame)
            {
                fullFrame = new byte[SPSPPSSegment.Length + StartCode.Length + completeFrame.Length];

                Array.Copy(SPSPPSSegment, 0, fullFrame, offset, SPSPPSSegment.Length); offset += SPSPPSSegment.Length;
            }
            else
            {
                fullFrame = new byte[StartCode.Length + completeFrame.Length];
            }

            Array.Copy(StartCode, 0, fullFrame, offset, StartCode.Length); offset += StartCode.Length;
            Array.Copy(completeFrame, 0, fullFrame, offset, completeFrame.Length); offset += completeFrame.Length;


            lock (RTPCombinePacket)
            {
                RTPCombinePacket.Enqueue(new RTPCombinePacket(fullFrame, SPSPPSSegment, frameType));
            }
        }

        private FrameTypeEnum GetFrameTypeFromNAL(byte[] completeFrame)
        {
            if (completeFrame.Length == 0) return FrameTypeEnum.Unknown;

            byte nalUnitType = (byte)(completeFrame[0] & 0x1F);

            switch (nalUnitType)
            {
                case 1:
                    return FrameTypeEnum.PFrame;  // P-프레임
                case 5:
                    return FrameTypeEnum.IFrame;  // I-프레임
                case 6:
                    return FrameTypeEnum.BFrame;  // B-프레임
                case 7:
                case 8:
                case 28:
                    // SPS, PPS, FU-A는 별도로 처리
                    return FrameTypeEnum.Unknown;
                default:
                    return FrameTypeEnum.Unknown; // 기타
            }
        }

        public RTPCombinePacket GetRTPCombineBuffer()
        {
            RTPCombinePacket result = null;

            lock (RTPCombinePacket)
            {
                if (RTPCombinePacket.Count != 0)
                {
                    result = RTPCombinePacket.Dequeue();
                }
            }

            return result;
        }

        public void Dispose()
        {
            RTPBuffer?.Clear();
            RTPBuffer = null;

            RTPCombinePacket?.Clear();
            RTPCombinePacket = null;
        }
    }
}
