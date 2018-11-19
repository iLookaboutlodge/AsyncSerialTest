using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncSerialTest2
{
    public class NMEA
    {

        public static bool IsValid(string nmea)
        {
            int L = nmea.Length;
            if (L < 5) return false;
            if (nmea[0] != '$') return false;
            if (nmea[L - 3] != '*') return false;
            if (!byte.TryParse(nmea.Substring(L - 2), NumberStyles.HexNumber, null, out byte cs)) return false;
            byte chk = 0;
            for (int i = 1; i < (L - 3); i++)
            {
                byte b = (byte)nmea[i];
                chk ^= b;
            }
            return chk == cs;
        }
    }


}
