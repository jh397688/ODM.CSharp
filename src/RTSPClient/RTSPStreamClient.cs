using RTSPStream.RTSP;
using RTSPStream.RTSP.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream
{
    public class RTSPStreamClient : IDisposable
    {
        RTSPClient _rtspClient;

        public RTSPStreamClient(string rtspUri, RTSPoverEnum rtspOver)
        {
            _rtspClient = new RTSPClient(new Uri(rtspUri), rtspOver);
        }

        public void SetAuth(string username, string password)
        {
            _rtspClient.SetAuth(username, password);
        }

        public void Play()
        {
            _rtspClient.Connect();
            _rtspClient.Play();
            _rtspClient.StartReceiveAsync();
        }

        public void Dispose()
        {
            _rtspClient?.Dispose();
        }
    }
}
