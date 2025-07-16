using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSPStream.Lib
{
    internal class EnumParser
    {
        public static TEnum StringToEnum<TEnum>(string value)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }
    }
}
