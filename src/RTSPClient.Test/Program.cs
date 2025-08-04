using RTSPStream;
using RTSPStream.RTSP.Enum;

namespace RTSPClient.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RTSPStreamClient client = new RTSPStreamClient("rtsp://210.99.70.120:1935/live/cctv001.stream", RTSPoverEnum.RTSPoverTCP);
            client.Connect();
            client.Play(RTSPTrackTypeEnum.Video);

            while (true) { }
        }
    }
}
