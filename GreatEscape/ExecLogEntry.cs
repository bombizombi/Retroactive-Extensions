using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GreatEscape
{


    public sealed class ExecLogEntry //v2
    {

        public ushort executedAddress;

        //full state on the first element, updates after?


        //public

        //for memory i need mem address
        //                  new value
        //                  old value

        //one byte memory read
        //one byte memory write

        //word memory read
        //word memory write

        //byte reg read
        //byte reg write

        //word reg read
        //word reg write
        //which reg, old, new

        //flags for if all those things happpened or not


    }








    //below is the full snapshot exec log version

    public sealed class ExecLogEntryV1
    {
        public const long EST_MEM_PER_ITEM = 66000;

        public ushort executedAddress;
        public byte[] ramC;
        public Registers registersC; //copy

        private static long entryCount = 0;

        private static byte[] immuLastRam = new byte[0x10000]; //what is this 
        public static Registers immuLastRegisters = new Registers(); 
        public ExecLogEntryV1(ushort adr, byte[] ram, Spectrum zx)
        {
            executedAddress = adr;

            //v1 copy whole ram every instruction
            //v2 if the memory is the same, just point to the last 
            //v3 sequence same comparison
            
            //compare array elements for each element
            bool ramSame = immuLastRam.SequenceEqual(ram);    

            if (ramSame)
            {
                ramC = immuLastRam;
            }
            else
            {
                //create a new copy of input array
                ramC = new byte[0x10000];
                Array.Copy(ram, ramC, 0x10000);
                immuLastRam = ramC;

                UpdateMemoryConsumption();
            }

            if (immuLastRegisters.RegistersSame( zx) )
            {
                registersC = immuLastRegisters;
                same++;
            }
            else
            {
                registersC = new Registers(zx);
                immuLastRegisters = registersC;
                different++;
            }

        }
        public static long same = 0;   //same ram
        public static long different = 0; //different ram





        private static void UpdateMemoryConsumption()
        {
            entryCount++;


            //stop if memory consumption goes above 20 gig  
            long estimatedTotal = entryCount * EST_MEM_PER_ITEM;
            if( estimatedTotal > oneGig * memLimit-1000000)
            {
                Debugger.Break();
            }
        }
        private const int memLimit = 4;

        private static long oneGig = (long)1024 * (long)1024 * (long)1024;
        public static bool MemoryFull { get => (entryCount+1) * EST_MEM_PER_ITEM > oneGig * memLimit; }

        public bool CompareToSpectrumState(Spectrum zx)
        {
            if (!immuLastRegisters.RegistersSame(zx)) return false;
            if (!MemorySame(zx.ram)) return false;            
            return true;
        }

        private bool MemorySame(byte[] ram)
        {
            //compare array elements for each element
            for (int i = 0; i < ram.Length; i++)
            {
                if (ram[i] != immuLastRam[i]) { return false; }
            }
            return true;
        }
        


    }

    public sealed class ExecLogV1FullSnapshots
    {
        public string Name;
        public List<ExecLogEntryV1> LogEntries;

        public ExecLogV1FullSnapshots(string name)
        {
            Name = name;
            LogEntries = new List<ExecLogEntryV1>();

            _bmpStart = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpMid = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpEnd = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);

            //at creation we don't have log elements yet, so we can't create bmps here

            ////_bmp1 = 
            //Screen.PaintZXScreenVersion5(LogEntries[0].ramC, _bmp1);
        }
        public ExecLogV1FullSnapshots(string name, List<ExecLogEntryV1> list) : this(name)
        {
            //Name = name;
            LogEntries = list;
        }

        public void CreateMugShots()
        {
            var count = LogEntries.Count-1;
            var mid = count / 2;

            Screen.PaintZXScreenVersion5(LogEntries[0].ramC, _bmpStart);
            Screen.PaintZXScreenVersion5(LogEntries[mid].ramC, _bmpMid);
            Screen.PaintZXScreenVersion5(LogEntries[count].ramC, _bmpEnd);
        }

        internal bool LogInstruction(ushort pc_start, byte[] ram, Spectrum zx)
        //pc_start: pc at the start of instruction
        //ram:  
        //zx:  just needed for the registers
        //return bool if emulation stop requested
        {

            if (ExecLogEntryV1.MemoryFull)
            {
                return true;
                /*
                m_instaExitRequested = true;
                stop_error = true;
                return;*/
            }

            var e = new ExecLogEntryV1(pc_start, ram, zx);
            Add(e);

            //if execlog will do incremental updates, check if it gets the same state back
            //get last log state
            ExecLogEntryV1? entry = GetLast();
            if (entry is not null)
            {
                if (!entry.CompareToSpectrumState(zx))
                {
                    //we need to get back the same state
                    Debug.Assert(false, "Last log no good.");
                }
            }
            return false;

        }

        internal void Add(ExecLogEntryV1 e)
        {
            LogEntries.Add(e);
        }

        //








        public override string ToString()
        {
            return $"Items: {this.LogEntries.Count()}";
        }

        internal ExecLogEntryV1? GetLast()
        {
            if (LogEntries.Count == 0) return null;
            return LogEntries.Last();
        }

        internal ExecLogV1FullSnapshots CreateSubLog(long sublistStart, long length)
        {
            //create new log from this, with a new length

            //we might do a naive version, just copy everything in the range
            //or shallow version, just pointing to the existing collection

            //linq, create sublist from LogEntries
            var newList = this.LogEntries.GetRange((int)sublistStart, (int)length);
            var newName = Name + " sub";
            var newLog = new ExecLogV1FullSnapshots(newName, newList);
            newLog.CreateMugShots();
            return newLog;
        }


        public long CalcFrameFromSlider(double sliderValue)
        //slider in  range 0,100 both ends possible
        {
            long totalFrames = LogEntries.Count;
            double requestedFrame = sliderValue / 100 * totalFrames;
            if (requestedFrame >= totalFrames) requestedFrame = totalFrames - 1;
            return (long)requestedFrame;
        }

        WriteableBitmap _bmpStart;
        WriteableBitmap _bmpMid;
        WriteableBitmap _bmpEnd;
        public WriteableBitmap BmpStart { get {  return _bmpStart;  }}
        public WriteableBitmap BmpMid { get { return _bmpMid; } }
        public WriteableBitmap BmpEnd { get { return _bmpEnd; } }



    }

    public class Registers
    {
        public Registers() { } //all zero
        public Registers(Spectrum zx)
        {
            a = zx.a;
            f = zx.f;
            b = zx.b;
            c = zx.c;
            h = zx.h;
            l = zx.l;
            d = zx.d;
            e = zx.e;

            sp = zx.sp;
            //pc = zx.pc;  //this is not good, pc is already increased 
            //pc will always be different

            a_ = zx.a_;
            f_ = zx.f_;
            b_ = zx.b_;
            c_ = zx.c_;
            h_ = zx.h_;
            l_ = zx.l_;
            d_ = zx.d_;
            e_ = zx.e_;

            ix = zx.ix;
            iy = zx.iy;

        }

        public byte a;
        public byte f;
        public byte b, c, h, l, d, e;
        public ushort sp;

        //public ushort pc;

        public byte b_, c_, h_, l_, d_, e_;
        public byte a_, f_;

        public ushort ix;
        public ushort iy;

        public bool RegistersSame(Spectrum zx)
        {
            if (a != zx.a) return false;
            if (f != zx.f) return false;
            if (b != zx.b) return false;
            if (c != zx.c) return false;
            if (h != zx.h) return false;
            if (l != zx.l) return false;
            if (d != zx.d) return false;
            if (e != zx.e) return false;

            if (sp != zx.sp) return false;
            //if (pc != zx.pc) return false;

            if (a_ != zx.a_) return false;
            if (f_ != zx.f_) return false;
            if (b_ != zx.b_) return false;
            if (c_ != zx.c_) return false;
            if (h_ != zx.h_) return false;
            if (l_ != zx.l_) return false;
            if (d_ != zx.d_) return false;
            if (e_ != zx.e_) return false;

            if (ix != zx.ix) return false;
            if (iy != zx.iy) return false;
            return true;
        }


    }


    

}
