using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace BARCPack
{
    public static class util
    {
        public static string readBARCString(BeBinaryReader barcread)
        {
            var ofs = barcread.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[32];

            int count = 0;
            while ((nextbyte = barcread.ReadByte()) != 0xFF & nextbyte != 0x00 & nextbyte != 0x2E)
            {
                // http://xayr.ga/share/08-2018/devenv_2018-08-28_19-25-1449828102-6af8-4f41-9714-3332a90194d3.png
                name[count] = nextbyte;
                count++;
            }
            barcread.BaseStream.Seek(ofs + 16, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }

        public static string readBARCStringWE(BeBinaryReader barcread)
        {
            var ofs = barcread.BaseStream.Position;
            byte nextbyte;
            byte[] name = new byte[32];

            int count = 0;
            while ((nextbyte = barcread.ReadByte()) != 0xFF & nextbyte != 0x00)
            {
                name[count] = nextbyte;
                count++;
            }
            barcread.BaseStream.Seek(ofs + 16, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(name, 0, count);
        }

        public static void writeBARCString(BeBinaryWriter barcwrite,string str)
        {

            byte[] buff = new byte[16];
            for (int i=0; i < str.Length && i < 16; i++)
            {
                buff[i] = (byte)str[i];
            }

            buff[14] = 0xFF; // aaa? AAAa.
            buff[15] = 0xFF;
            barcwrite.BaseStream.Write(buff, 0, 16); 
        }
    }
}
