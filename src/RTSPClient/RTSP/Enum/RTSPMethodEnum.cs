using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.RTSP.Enum
{
    internal enum RTSPMethodEnum
    {
        OPTIONS,
        DESCRIBE,
        SETUP,
        PLAY,
        PAUSE,
        RECORD,
        ANNOUNCE,
        TEARDOWN,
        GET_PARAMETER,
        SET_PARAMETER,
        REDIRECT,
        Embedded,
    }
}
