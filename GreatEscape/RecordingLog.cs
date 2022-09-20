using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace GreatEscape
{

    public sealed class Recording
    {
        public Dictionary<long, byte> keyboard;
        public Dictionary<long, byte> RRegister;
    }

    public sealed class RecordingLog  //exec log v3
    {
        public string Name;
        //keyboard and R logs
        Recording recording;

    //1111111111111111111111111111111111111111111111111111111111111111111


    //public sealed class ExecLog //v2

        //private List<ExecLogEntry> _LogEntries;  //


        WriteableBitmap _bmpStart;
        WriteableBitmap _bmpMid;
        WriteableBitmap _bmpEnd;

        public RecordingLog(string name)
        {
            Name = name;
            //_LogEntries = new List<ExecLogEntry>(300_070_000);

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

            Func<Func<ExecLogState, ExecLogState>> cooker = () =>
            {
                byte[] capram = new byte[0x10000];
                Array.Copy(inram, capram, inram.Length);
                Registers capregs = new Registers(zx);

                return state =>
                {
                    //this should execute on start of log replay, each time
                    //param is ignored for the full state update logstep
                    byte[] newram = new byte[0x10000];
                    Array.Copy(capram, newram, capram.Length);
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



        //public bool AddLogInstruction(ushort pc_start, ushort pc_end,  Func<ExecLogState, ExecLogState> funcToReplayState)
        //{ ... } not needed








        public bool IsEmpty()
        {
            return _LogEntries.Count() == 0;
        }


        //indexer
        public ExecLogState this[int index]
        {
            //currently both indexer and public list are available
            get
            {
                var dumState = new ExecLogState() { ramC = new byte[0x10000] }; //just dummy needed here
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
