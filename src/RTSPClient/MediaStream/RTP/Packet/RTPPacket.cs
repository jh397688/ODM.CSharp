using RTSPStream.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.MediaStream.RTP.Packet
{
    internal class RTPPacket
    {
        public byte[] OriginData;
        
        public int Version;
        public bool Padding;
        public bool Extension;
        public uint ExtensionId;
        public uint ExtensionSize;
        public int CsrcCount;
        public bool Marker;
        public int PayloadType;
        public uint SequenceNumber;
        public uint TimeStamp;
        public uint Ssrc;
        public int PayLoadStart;
        public byte[] PayloadData;

        public bool IsRTPPacket;

        public RTPPacket(byte[] rtpPacket)
        {
            try
            {
                int byteOffset = 0;
                int bitOffset = 0;

                OriginData = rtpPacket;

                Version = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 2);
                Padding = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 1) != 0;
                Extension = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 1) != 0;
                CsrcCount = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 3);
                
                byteOffset = 1;
                bitOffset = 0;
                Marker = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 1) != 0;
                PayloadType = ByteReader.ReadBitByByte(rtpPacket[byteOffset], ref bitOffset, 7);

                byteOffset = 2;
                bitOffset = 0;
                SequenceNumber = (uint)ByteReader.ReadIntByByteArr(rtpPacket, ref byteOffset, 2);
                TimeStamp = (uint)ByteReader.ReadIntByByteArr(rtpPacket, ref byteOffset, 4);
                Ssrc = (uint)ByteReader.ReadIntByByteArr(rtpPacket, ref byteOffset, 4);
                
                PayLoadStart = byteOffset + (4 * CsrcCount); // zero or more csrcs

                if (Marker)
                {
                    if (Extension == true)
                    {
                        if (PayLoadStart + 4 > rtpPacket.Length)
                        {
                            ExtensionId = (uint)ByteReader.ReadIntByByteArr(rtpPacket, ref PayLoadStart, 2);
                            ExtensionSize = (uint)ByteReader.ReadIntByByteArr(rtpPacket, ref PayLoadStart, 2);
                            
                            PayLoadStart += (int)ExtensionSize;  // extension header and extension payload
                        }
                    }
                }

                if (rtpPacket.Length - PayLoadStart < 0)
                {
                    return;
                }

                PayloadData = new byte[rtpPacket.Length - PayLoadStart]; // payload with RTP header removed
                System.Array.Copy(rtpPacket, PayLoadStart, PayloadData, 0, PayloadData.Length); // copy payload

                IsRTPPacket = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void PrintInfo()
        {
            Console.WriteLine("RTP Header Information:");
            Console.WriteLine($"Version: {Version}");
            Console.WriteLine($"Padding: {Padding}");
            Console.WriteLine($"Extension: {Extension}");
            Console.WriteLine($"CSRC Count: {CsrcCount}");
            Console.WriteLine($"Marker: {Marker}");
            Console.WriteLine($"Payload Type: {PayloadType}");
            Console.WriteLine($"Sequence Number: {SequenceNumber}");
            Console.WriteLine($"Timestamp: {TimeStamp}");
            Console.WriteLine($"SSRC: {Ssrc}");
            Console.WriteLine();
        }
    }
}
