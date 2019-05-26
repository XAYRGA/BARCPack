using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Be.IO;

namespace BARCPack
{




    class Program
    {
        static bool[] addr_history;
        static int[] seq_map;
        static int[] TrackSizes;
        static void Main(string[] args)
        {
           
            if (args.Length > 0)
            {
                if (args[0] == "unpack")
                {
                    if (args.Length > 2 )
                    {
                        unpack(args[1], args[2]);
                    } else
                    {
                        Console.WriteLine("barcpack unpack <BARC file> <out dir>");
                    }
                }

                if (args[0] == "pack")
                {
                    if (args.Length > 2)
                    {
                        packBARC(args[1], args[2]);
                    }
                    else
                    {
                        Console.WriteLine("barcpack pack <BARC file> <out dir>");
                    }
                }
            }
        }


        public static void packBARC(string dir,string barcname)
        {
            var barcstm = File.OpenWrite(barcname);
            var barcwrite = new BeBinaryWriter(barcstm);
            barcwrite.Write(0x424152432D2D2D2DL); // BARC----
            barcwrite.Write((uint)0xDEADBEEF); // skip 

            var schema = File.OpenRead(dir + "\\schema.bin");
            var schemard = new BeBinaryReader(schema);

            var total_off = 0;
            
            var end_size = schemard.ReadInt32();
            var count = schemard.ReadInt32();
            var seqArcFile = schemard.ReadString();

           
            barcwrite.Write(count);
            var sqarc_out = File.OpenWrite(dir + "\\" + seqArcFile);

            util.writeBARCString(barcwrite, seqArcFile);
            seq_map = new int[0xFFFFF];
            TrackSizes = new int[0xFFFFF];
            for (int i =  0; i < count; i++)
            {
               
                var inname = string.Format("{0:D6}.bms", i);
                var e = File.ReadAllBytes(dir + "\\" + inname);
                var wx = File.OpenRead(dir + "\\" + inname);
                var wxr = new BeBinaryReader(wx);
                var alias = wxr.ReadUInt32();
                bool doAli = false;
                int sIdx = 0;
                if (alias==0xAFBFCFDF)
                {
                    sIdx = wxr.ReadInt32();
                    doAli = true;
                    Console.WriteLine("Aliased {0}",sIdx);
                }
                wx.Close();

         
                util.writeBARCString(barcwrite, inname);
                barcwrite.Write((int)2);
                barcwrite.Write((int)3);
                if (doAli)
                {
                    Console.WriteLine("Alias map to {0:X}, {1}",seq_map[sIdx],sIdx);
                    barcwrite.Write(seq_map[sIdx]);
                    barcwrite.Write(TrackSizes[sIdx]);

                    continue;
                }
                else
                {
                    barcwrite.Write(total_off);
                }
              
               
                Console.WriteLine("Sequence {0} at {1:X}", i, total_off);


                seq_map[i] = total_off;
                TrackSizes[i] = e.Length;
                barcwrite.Write(e.Length);
                if (doAli) { continue; }
                sqarc_out.Position = total_off;
                sqarc_out.Write(e, 0, e.Length);


               
                var Pad = new byte[0x20];
                sqarc_out.Write(Pad, 0, Pad.Length);


                total_off += e.Length + Pad.Length ;

            }
            barcwrite.Flush();
            sqarc_out.Flush();
           
        }

        public static void unpack(string barc, string outdir)
        {
            if (!Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }
        
            var schemastm = File.OpenWrite(outdir + "\\schema.bin");
            var schemawt = new BeBinaryWriter(schemastm);
            var barcstm = File.OpenRead(barc);
            var barcread = new BeBinaryReader(barcstm);
            schemawt.Write((int)barcstm.Length);

            var bhead = barcread.ReadUInt64(); // Should be BARC.

            if (bhead != 0x2D2D2D2D42415243)
            {
                Console.WriteLine("!!!! BARC header didn't match! 0x{0:X}!=0x2D2D2D2D42415243", bhead);
                return;

            }
            barcread.ReadInt32(); // skip 
            var count = barcread.ReadInt32();
            schemawt.Write(count);
            var ARCFile = util.readBARCStringWE(barcread);
            schemawt.Write(ARCFile);
            var vread = File.OpenRead(ARCFile);
            addr_history = new bool[vread.Length * 3];
            seq_map = new int[vread.Length * 3]; 
            var names = new string[count];
            for (int i=0; i < count; i++)
            {
                var seqname = util.readBARCStringWE(barcread) ;
                var outname = string.Format("{0:D6}.bms", i);
                names[i] = outname + " = " + seqname;
                barcread.ReadUInt32();
                barcread.ReadUInt32(); // Idk, but im sure as hell not using these for this app. 
                var offset = barcread.ReadUInt32();
                if (addr_history[offset])
                {
                    var bx = File.OpenWrite(outdir + "\\" + outname);
                    var bxw = new BeBinaryWriter(bx);
                    bxw.Write(0xAFBFCFDF);
                    bxw.Write(seq_map[offset]);
                    barcread.ReadUInt32(); // just to keep aligned.
                    Console.WriteLine("Alias.");
                    continue;
                    
                }
                addr_history[offset] = true;
                seq_map[offset] = i;
                var size = barcread.ReadUInt32();
                var filedata = new byte[size];
                vread.Position = offset;
                vread.Read(filedata, 0, (int)size);
                File.WriteAllBytes(outdir + "\\" + outname,filedata);   
            }

            File.WriteAllLines(outdir + "\\names.txt", names);
            schemawt.Close();
            schemastm.Close();
           
        }
    }
}
