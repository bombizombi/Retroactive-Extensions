using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Reflection;
using System.Xaml;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.IO;

namespace GreatEscape
{

    public sealed class ExecLogState
    {
        public ushort executedAddress;

        public byte[] ramC;
        public Registers registersC;



        public static readonly ExecLogState Empty = new ExecLogState() { ramC = new byte[1] };



        public bool CompareToSpectrumState(Spectrum zx)
        {
            if (!registersC.RegistersSame(zx)) return false;
            if (!MemorySame(zx.ram)) return false;
            return true;
        }


        private bool MemorySame(byte[] ram)
        {
            //return ram.SequenceEqual(ramC);

            //compare two byte arrays
            for (int i = 0; i < ram.Length; i++)
            {
                if (ram[i] != ramC[i]) return false;
            }
            return true;

        }



        public static Action<byte, byte[], Registers>[] st_reg_writers;

        static ExecLogState()
        //public static void Initialize_Reg_ReadersWriters()
        {
            
            //xx - reg
            // b 000
            // c 001
            // d 010
            // e 011
            // h 100
            // l 101
            //   110?  (hl)
            // a 111


            st_reg_writers = new Action<byte, byte[], Registers>[8];
            st_reg_writers[0] = (x, ram, regs) => regs.b = (byte)x;
            st_reg_writers[1] = (x, ram, regs) => regs.c = (byte)x;
            st_reg_writers[2] = (x, ram, regs) => regs.d = (byte)x;
            st_reg_writers[3] = (x, ram, regs) => regs.e = (byte)x;
            st_reg_writers[4] = (x, ram, regs) => regs.h = (byte)x;
            st_reg_writers[5] = (x, ram, regs) => regs.l = (byte)x;
            st_reg_writers[7] = (x, ram, regs) => regs.a = (byte)x;
            st_reg_writers[6] = (x, ram, regs) =>
            {
                ram[regs.h * 256 + regs.l] = (byte)x;
                //DebugMemoryWrite(h * 256 + l, 1, pc - 1);
            };

            /*
            reg_readers = new Func<int>[8];
            reg_readers[0] = () => b;
            reg_readers[1] = () => c;
            reg_readers[2] = () => d;
            reg_readers[3] = () => e;
            reg_readers[4] = () => h;
            reg_readers[5] = () => l;
            reg_readers[7] = () => a;
            reg_readers[6] = () => ram[h * 256 + l];

            longreg_readers = new Func<int>[4];
            longreg_readers[0] = () => b * 256 + c;
            longreg_readers[1] = () => d * 256 + e;
            longreg_readers[2] = () => h * 256 + l;
            longreg_readers[3] = () => sp;

            longreg_writers = new Action<int>[4];
            longreg_writers[0] = x => { b = (byte)(x / 256); c = (byte)(x % 256); };
            longreg_writers[1] = x => { d = (byte)(x / 256); e = (byte)(x % 256); };
            longreg_writers[2] = x => { h = (byte)(x / 256); l = (byte)(x % 256); };
            longreg_writers[3] = x => { sp = (ushort)x; };
            */
        }


        


    } // end class ExecLogState


    public sealed class ExecLogEntry //v2
    {

        public ushort executedAddress;
        public ushort endingAddress;
        public Func<ExecLogState, ExecLogState> replayInstruction;

        public ExecLogEntry(ushort ea, ushort enda, Func<ExecLogState, ExecLogState> ri)
        {
            this.executedAddress = ea;
            this.endingAddress = enda;
            this.replayInstruction = ri;
        }



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





    public sealed class ExecLog //v2
    {
                
        public string Name;
        //public List<ExecLogEntry> LogEntries;

        //public List< Func<ExecLogState,ExecLogState> > LogEntries;  //
        //private  List< Func<ExecLogState, ExecLogState> > _LogEntries;  //
        private List<ExecLogEntry> _LogEntries;  //


        WriteableBitmap _bmpStart;
        WriteableBitmap _bmpMid;
        WriteableBitmap _bmpEnd;

        public ExecLog(string name)
        {
            Name = name;
            _LogEntries = new List< ExecLogEntry >(300_070_000);

            _bmpStart = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpMid = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpEnd = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);

        }
        public ExecLog(string name, List<ExecLogEntry> list) : this(name)
        {
            //Name = name;
            _LogEntries = list;
        }



        //the first entry should be the initial state

        //public void InitializeState(Spectrum zx)  or just 
        public void InitializeState(byte[] inram, Spectrum zx)
            //params:  inram -> current ram
            //         m_zx for access to the current registers
        {
            //usage log.InitializeState(m_zx.ram);

            //create a function with one parameter that returns a function that will initialize our state

            Func < Func<ExecLogState, ExecLogState> >  cooker = () =>
            {
                byte[] capram = new byte[0x10000];
                Array.Copy(inram, capram, inram.Length);
                Registers capregs = new Registers(zx);

                return state =>
                {
                    //this should execute on start of log replay, each time
                    //param is ignored for the full state update logstep
                    byte[] newram = new byte[0x10000];
                    Array.Copy( capram, newram, capram.Length);
                    Registers newregs = new Registers(capregs);

                    var newState = new ExecLogState()
                    {
                        executedAddress = 0,
                        ramC = newram,
                        registersC = newregs
                    };
                    return newState;
                };
            };

            Debug.Assert(_LogEntries.Count() == 0, "only at start now, perhaps different later");
            //_LogEntries.Add(cooker());
            var entry = new ExecLogEntry(zx.pc, zx.pc, cooker()); //no real starting address, this is just state initialization
            _LogEntries.Add(entry);


            //1111111
            //(Func<byte, ExecLogState, Action<ExecLogState>>)((opc, state) => {
            //...
            //return state => state.a = capRezValue;

        }


        public bool AddLogInstruction(ushort pc_start, ushort pc_end,
                                      Func<ExecLogState, ExecLogState> funcToReplayState )
        {

            //Debugger.Break(); //old log is below

            /* todo new log needs memory check
            if (ExecLogEntryV1.MemoryFull)
            {
                return true;
            }*/

            //            var e = new ExecLogEntryV1(pc_start, ram, zx);
            //Add(e);
            var entry = new ExecLogEntry(pc_start, pc_end, funcToReplayState);
            _LogEntries.Add( entry);

            //if execlog will do incremental updates, check if it gets the same state back
            //get last log state
            /*
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
            */
            //22


            return false; //no error
        }


        //usage
        //var x = m_loggingOpcodes[instruction];
        //m_execLog.AddLogInstruction(x);

            //public bool AddLogInstruction




            /* old log instruction

            internal bool LogInstruction(ushort pc_start, byte[] ram, Spectrum zx)
            //pc_start: pc at the start of instruction
            //ram:  
            //zx:  just needed for the registers
            //return bool if emulation stop requested
            {

                if (ExecLogEntryV1.MemoryFull)
                {
                    return true;
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
            */


        public bool IsEmpty()
        {
            return _LogEntries.Count() == 0;
        }


            //indexer
        public ExecLogState this[int index]
        {
            //currently both indexer and public list are available
            get {
                var dumState = new ExecLogState() { ramC = new byte[0x10000]}; //just dummy needed here
                //var dumState = ExecLogState.Empty;
                var state = _LogEntries[0].replayInstruction(dumState);

                //var x = 0x5fe6;
                //Debug.WriteLine($"log 0 5FE6 = {state.ramC[0x5FE6]}");

                for (int i = 1; i <= index; i++)
                {
                    //Debug.Write($"in{i} e6 = {state.ramC[0x5FE6]} out");
                    _LogEntries[i].replayInstruction(state);
                    //Debug.WriteLine($"log {i} 5FE6 = {state.ramC[0x5FE6]}");

                }
                //fix the ending pc
                state.registersC.pc = _LogEntries[index].endingAddress;
                return state;
            }
        }

        public IEnumerable<ExecLogState> LogEntries
        {
            get
            {
                var state = _LogEntries[0].replayInstruction(ExecLogState.Empty);
                yield return state;

                for (int i = 1; i < _LogEntries.Count; i++)
                {
                    yield return _LogEntries[i].replayInstruction(state);
                }
            }
        }

        //was red this way
        //            ExecLogEntryV1 entry = _activeLog.LogEntries[requested];






        internal ExecLog CreateSubLog(long sublistStart, long length)
        {
            //create new log from this, with a new length

            //we might do a naive version, just copy everything in the range
            //or shallow version, just pointing to the existing collection

            //linq, create sublist from LogEntries
            //var newList = this.LogEntries.GetRange((int)sublistStart, (int)length);
            Debugger.Break(); //todo just a copy


            var newName = Name + " sub";
            //var newLog = new ExecLog(newName, newList);
            var newLog = new ExecLog(newName, this._LogEntries);
            newLog.CreateMugShots();
            return newLog;
        }

        public long CalcFrameFromSlider(double sliderValue)
        //slider in  range 0,100 both ends possible
        {
            long totalFrames = _LogEntries.Count;
            double requestedFrame = sliderValue / 100 * totalFrames;
            if (requestedFrame >= totalFrames) requestedFrame = totalFrames - 1;
            return (long)requestedFrame;
        }


        public WriteableBitmap BmpStart { get { return _bmpStart; } }
        public WriteableBitmap BmpMid { get { return _bmpMid; } }
        public WriteableBitmap BmpEnd { get { return _bmpEnd; } }
        public void CreateMugShots()
        {
            var count = _LogEntries.Count - 1;
            var mid = count / 2;

            //Debugger.Break();  //mugshots a nice test

            Screen.PaintZXScreenVersion5(this[0].ramC, _bmpStart);
            Screen.PaintZXScreenVersion5(this[mid].ramC, _bmpMid);
            Screen.PaintZXScreenVersion5(this[count].ramC, _bmpEnd);
            
        }


    }



}
