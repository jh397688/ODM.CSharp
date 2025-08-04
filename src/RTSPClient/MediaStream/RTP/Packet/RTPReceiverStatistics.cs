using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTP.Packet
{
    internal static class RTPReceiverStatisticsManager
    {
        public static RTPReceiverStatistics Statistics { get; } = new RTPReceiverStatistics();
    }

    internal class RTPReceiverStatistics
    {
        public uint BaseSequence { get; set; }
        public uint ExtendedHighestSequenceNumber { get; set; }
        public uint ReceivedCount { get; set; }
        public uint ExpectedPrior { get; set; }
        public uint ReceivedPrior { get; set; }
        public uint Jitter { get; set; }
        public uint LastSR { get; set; }
        public DateTime LastSRArrivalTime { get; set; }

        private object LockObj = new object();
        private double _lastTransit = 0;
        private double _jitter = 0;

        public RTPReceiverStatistics()
        {

        }

        internal void StreamRTPPacket(RTPPacket rtpPacket)
        {
            lock (LockObj)
            {
                // RTP 패킷 도착 시 통계 업데이트 (예제 코드 참고)
                const double clockRate = 90000.0;
                double arrivalInRtpUnits = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds * clockRate;

                ReceivedCount++;

                if (ReceivedCount == 1)
                {
                    BaseSequence = rtpPacket.SequenceNumber;
                    ExtendedHighestSequenceNumber = rtpPacket.SequenceNumber;
                    _lastTransit = arrivalInRtpUnits - rtpPacket.TimeStamp;
                    _jitter = 0;
                    Jitter = 0;
                }
                else
                {
                    uint currentSeq = rtpPacket.SequenceNumber;
                    uint highestSeq16 = ExtendedHighestSequenceNumber & 0xFFFF;
                    if (currentSeq < highestSeq16 && (highestSeq16 - currentSeq) > 30000)
                    {
                        currentSeq += 65536;
                    }
                    if (currentSeq > ExtendedHighestSequenceNumber)
                    {
                        ExtendedHighestSequenceNumber = currentSeq;
                    }

                    double transit = arrivalInRtpUnits - rtpPacket.TimeStamp;
                    double d = Math.Abs(transit - _lastTransit);
                    _jitter += (d - _jitter) / 16.0;
                    _lastTransit = transit;
                    Jitter = (uint)_jitter;
                }
            }
        }
    }
}
