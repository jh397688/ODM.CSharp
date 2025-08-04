using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib
{
    public static class ByteReader
    {
        public static int ReadBitByByte(byte bytedata, ref int offset, int count)
        {
            // 유효성 검증
            if (offset < 0 || offset > 7)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset은 0~7 사이여야 합니다.");
            if (count < 1 || count > 8 - offset)
                throw new ArgumentOutOfRangeException(nameof(count), "count는 1 이상이며, offset + count <= 8 이어야 합니다.");

            int shiftAmount = 8 - (offset + count);
            int mask = (1 << count) - 1;

            offset += count;
            return (bytedata >> shiftAmount) & mask;
        }

        public static int ReadIntByByteArr(byte[] bytearr, ref int offset, int count)
        {
            if (bytearr == null)
                throw new ArgumentNullException(nameof(bytearr));
            if (count < 1 || count > 4)
                throw new ArgumentOutOfRangeException(nameof(count), "count는 1~4 사이여야 합니다.(int 범위)");
            if (offset + count > bytearr.Length)
                throw new ArgumentException("offset + count가 배열 길이를 초과합니다.");

            int result = 0;
            // 빅 엔디안 방식으로 count바이트를 읽어서 int로 변환
            for (int i = 0; i < count; i++)
            {
                result = (result << 8) | bytearr[offset + i];
            }
            offset += count;

            return result;
        }
    }

    public static class ByteWriteArray
    {
        public static void WriteUInt8BigEndian(uint value, ref byte[] byteArray, ref int offset)
        {
            byteArray[offset++] = (byte)(value & 0xFF);
        }

        public static void WriteUInt16BigEndian(uint value, ref byte[] byteArray, ref int offset)
        {
            byteArray[offset++] = (byte)((value >> 8) & 0xFF);
            byteArray[offset++] = (byte)(value & 0xFF);
        }

        public static void WriteUInt24BigEndian(int value, ref byte[] byteArray, ref int offset)
        {
            byteArray[offset++] = (byte)((value >> 16) & 0xFF);
            byteArray[offset++] = (byte)((value >> 8) & 0xFF);
            byteArray[offset++] = (byte)(value & 0xFF);
        }

        public static void WriteUInt32BigEndian(uint value, ref byte[] byteArray, ref int offset)
        {
            byteArray[offset++] = (byte)((value >> 24) & 0xFF);
            byteArray[offset++] = (byte)((value >> 16) & 0xFF);
            byteArray[offset++] = (byte)((value >> 8) & 0xFF);
            byteArray[offset++] = (byte)(value & 0xFF);
        }

        public static void WriteUInt64BigEndian(ulong value, ref byte[] byteArray, ref int offset)
        {
            byteArray[offset++] = (byte)((value >> 56) & 0xFF);
            byteArray[offset++] = (byte)((value >> 48) & 0xFF);
            byteArray[offset++] = (byte)((value >> 40) & 0xFF);
            byteArray[offset++] = (byte)((value >> 32) & 0xFF);
            byteArray[offset++] = (byte)((value >> 24) & 0xFF);
            byteArray[offset++] = (byte)((value >> 16) & 0xFF);
            byteArray[offset++] = (byte)((value >> 8) & 0xFF);
            byteArray[offset++] = (byte)(value & 0xFF);
        }
    }
}
