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

namespace GreatEscape
{

    public sealed class ExecLogState
    {
        public ushort executedAddress;

        public byte[] ramC;
        //public Registers registersC



        public static readonly ExecLogState Empty = new ExecLogState() { ramC = new byte[1] };

    }

    public delegate Func<ExecLogState, ExecLogState> LogStateMaker(byte op, ExecLogState s);


    public sealed class ExecLog //v2
    {
                
        public string Name;
        //public List<ExecLogEntry> LogEntries;

        //public List< Func<ExecLogState,ExecLogState> > LogEntries;  //
        private  List< Func<ExecLogState, ExecLogState> > _LogEntries;  //


        WriteableBitmap _bmpStart;
        WriteableBitmap _bmpMid;
        WriteableBitmap _bmpEnd;

        public ExecLog(string name)
        {
            Name = name;
            _LogEntries = new List<Func<ExecLogState, ExecLogState>>();

            _bmpStart = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpMid = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpEnd = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);

        }
        public ExecLog(string name, List<Func<ExecLogState, ExecLogState>> list) : this(name)
        {
            //Name = name;
            _LogEntries = list;
        }



        //the first entry should be the initiali state

        //public void InitializeState(Spectrum zx)  or just 
        public void InitializeState(byte[] inram)
        {
            //log.InitializeState(m_zx.ram);

            //create a function with one parameter that returns a function that will initialize our state

            Func < Func<ExecLogState, ExecLogState> >  cooker = () =>
            {
                byte[] capram = new byte[0x10000];
                Array.Copy(inram, capram, inram.Length);

                return state =>
                {
                    Array.Copy(capram, state.ramC, capram.Length);
                    return state;
                };
            };

            Debug.Assert(_LogEntries.Count() == 0, "only at start now, perhaps different later");
            _LogEntries.Add(cooker());

            
            //1111111
            //(Func<byte, ExecLogState, Action<ExecLogState>>)((opc, state) => {
                //...
                //return state => state.a = capRezValue;

            }


        public bool AddLogInstruction(ushort pc_start, 
                                      Func<ExecLogState, ExecLogState> funcToReplayState )
        {

            Debugger.Break(); //old log is below


            if (ExecLogEntryV1.MemoryFull)
            {
                return true;
            }

//            var e = new ExecLogEntryV1(pc_start, ram, zx);
            //Add(e);

            _LogEntries.Add( funcToReplayState);

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


            //indexer
        public ExecLogState this[int index]
        {
            //currently both indexer and public list are available
            get {
                //will not work
                Debugger.Break();
                //return LogEntries[i];

                //something like this should
                //var state = _LogEntries[0](null);

                var dumState = new ExecLogState() { ramC = new byte[0x10000]};
                var state = _LogEntries[0](dumState);
                for (int i = 1; i <= index; i++)
                {
                    _LogEntries[i](state);
                }
                return state;

            }
        }

        public IEnumerable<ExecLogState> LogEntries
        {
            get
            {
                var state = _LogEntries[0](null);
                yield return state;

                for (int i = 1; i < _LogEntries.Count; i++)
                {
                    yield return _LogEntries[i](state);
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

            Debugger.Break();  //mugshots a nice test

            //todo turn mugshots back on
            /*
            Screen.PaintZXScreenVersion5(LogEntries[0].ramC, _bmpStart);
            Screen.PaintZXScreenVersion5(LogEntries[mid].ramC, _bmpMid);
            Screen.PaintZXScreenVersion5(LogEntries[count].ramC, _bmpEnd);
            */
        }


    }



}
