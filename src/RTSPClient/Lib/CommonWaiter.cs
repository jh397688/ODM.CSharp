using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib
{
    internal class CommonWaiter
    {
        private int _cseq;
        private string _commonData;

        private AutoResetEvent EventWaitHandler;
        public bool IsWait;

        public CommonWaiter()
        {
            EventWaitHandler = new AutoResetEvent(false);
        }

        public void SetExpectedHeader(int cseq)
        {
            _cseq = cseq;
        }

        public bool IsDesiredPacket(int cseq)
        {
            return _cseq == cseq;
        }

        public void SetData(string commonData)
        {
            _commonData = commonData;
            EventWaitHandler.Set();

            IsWait = false;
        }

        public bool WaitForData(TimeSpan timeout, out string commonData)
        {
            IsWait = true;

            if (EventWaitHandler.WaitOne(timeout))
            {
                commonData = _commonData;
                return true;
            }
            else
            {
                commonData = "";
                return false;
            }
        }
    }
}
