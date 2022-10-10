using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Security.Policy;

namespace GreatEscape
{

    public sealed class Recording
    {
        public Dictionary<long, byte> keyboard;
        public Dictionary<long, byte> RRegister;
        public ExecLogState initialState;
    }

    /* version 1: record full memory after every step (64k per step)
     *         1b, compare memory state and only copy if there was a difference
     * version 2: each instruction had its own state modifying closure, still two objects
     *            were created after each step, 100 bytes per step
     * version 3: record only non-deterministic events, keyboard reading and R register reading
     *            0 bytes per instruction?
     *            ExecLogEntry is not needed anymore, as nothing is added after each Step.
     *            replaced with Recording Log and starting and ending counter values.
     *            Only the full state at the start is needed.
     *            
     *            
     *            
     *            -Dictionaries must be somehow sent to keyboard class
     *            -ExecLog to RecordingLog conversion
     *            -instCounter start and end values must be send to RecordingLog
     *            
     *            add start and end counters , start in the state int, last in a separate call
     *            
     */
    public sealed class RecordingLog  //exec log v3
    {
        public string Name;
        //keyboard and R logs
        public Recording rec;

        public long instructionStart;
        public long instructionEnd;


        //1111111111111111111111111111111111111111111111111111111111111111111


        //public sealed class ExecLog //v2

        //private List<ExecLogEntry> _LogEntries;  //_logEntries as a concept does not exist any more
        //could it be replaced with instCounterStart and instCounterEnd values?
        private RecordingLog m_log;

        //slider rendering
        Dictionary<long, ExecLogState> _renderedFrames = new(); //move

        //mugshots
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

            rec = new Recording();
            instructionStart = -1;
            instructionEnd = -1;
        }

        //? list does not exist with keyboardR recording
        /*public ExecLog(string name, List<ExecLogEntry> list) : this(name)
        {
            //Name = name;
            _LogEntries = list;
        }*/



        //the first entry should be the initial state

        //public void InitializeState(Spectrum zx)  or just 
        public void InitializeState(byte[] inram, Spectrum zx, long instCounterStart)
        //params:  inram -> current ram
        //         m_zx for access to the current registers
        //         instCounterStart -> current instruction counter
        //
        // (this parameter list makes no sense)
         
        {
            instructionStart = instCounterStart;

            rec.initialState = CopyStateFromSpectrum(inram, zx);
                
            /* moved to copyStateFromSpectrum
            byte[] capram = new byte[0x10000];
            Array.Copy(inram, capram, inram.Length);
            Registers capregs = new Registers(zx);
            rec.initialState = new ExecLogState(capram, capregs);*/

            //instructionStart is not part of the state???
        }

        public static ExecLogState CopyStateFromSpectrum(byte[] inram, Spectrum zx)
        {
            //this brainded parameter list is still here 
            byte[] capram = new byte[0x10000];
            Array.Copy(inram, capram, inram.Length);
            Registers capregs = new Registers(zx);

            Debugger.Break(); //decisions must be made on how many steps log actually has
                //and if points at the next instruction to exec or the current that was already done
            var rez = new ExecLogState(capram, capregs, zx.pc);
            return rez;
        }

        public void SetLastInstructionCounter(long c)
        {
            instructionEnd = c;
        }


        public bool IsEmpty()
        {
            return (instructionEnd - instructionStart) <= 0;
        }


        //indexer
        public Spectrum this[long index]
        {
            //currently both indexer and public list are available
            get
            {
                //right now indexer and IEnumerable of states share a lot of code, but indexer should
                //be optimized, and enumerable will probably stay the same

                //however, both are untested
                
                //var state = rec.initialState.Copy();
                var state = rec.initialState.Copy();//copy not needed, spectrum allready copies everything

                //create new log replaying keyboard
                KeyboardWithPlayback kb = new KeyboardWithPlayback(rec.keyboard, rec.RRegister);

                //create new zx with this state
                var zx = new Spectrum(state.ramC, state.registersC, kb);

                //do all steps
                /*
                for (long i = instructionStart; i < instructionEnd; i++)
                {
                    //see if end will be the next index, or the last existing
                    //zx.Step();
                    yield return zx;
                }*/


                bool stop_error_ignored;
                //do all steps
                for (long i = 0; i < index; i++)
                {
                    //see if end will be the next index, or the last existing
                    zx.Step(out stop_error_ignored);
                }
                return zx;

                /*
                for (int i = 1; i <= index; i++)
                {
                    //Debug.Write($"in{i} e6 = {state.ramC[0x5FE6]} out");
                    _LogEntries[i].replayInstruction(state);
                    //Debug.WriteLine($"log {i} 5FE6 = {state.ramC[0x5FE6]}");

                }*/
                
                //fix the ending pc  should be in the regs
                //state.registersC.pc = _LogEntries[index].endingAddress;
                //return state;
            }
        }

        
        //public IEnumerable<ExecLogState> LogEntries
        public IEnumerable<Spectrum> LogEntries
        {
            get
            {
                //log with two instructions recorded actually has 3 states:
                //initial state, after 1st inst, after 2nd inst.
                //which ones do i return?
                //LogEntries.Count() will return different result than (insEnd-insStart)


                var state = rec.initialState.Copy();//copy not needed, spectrum allready copies everything

                //create new log replaying keyboard
                KeyboardWithPlayback kb = new KeyboardWithPlayback(rec.keyboard, rec.RRegister);
                
                //create new zx with this state
                var zx = new Spectrum(state.ramC, state.registersC, kb);
                //        public Spectrum(byte[] inmem, Registers inregs, Keyboard kb )

                yield return zx;

                bool stop_error_ignored;
                //do all steps
                for (long i = instructionStart; i < instructionEnd; i++)
                {
                    //see if end will be the next index, or the last existing
                    zx.Step(out stop_error_ignored);
                    yield return zx;
                }


                /* fix when indexer works
                var state = _LogEntries[0].replayInstruction(ExecLogState.Empty);
                yield return state;

                for (int i = 1; i < _LogEntries.Count; i++)
                {
                    yield return _LogEntries[i].replayInstruction(state);
                }*/
                //Debugger.Break();  //could be enough
            }
        }

        //was red this way
        //            ExecLogEntryV1 entry = _activeLog.LogEntries[requested];





        internal RecordingLog CreateSubLog(long sublistStart, long length)
        {
            //this wasn't even finnished in the v2
            
            //create new log from this, with a new length

            //we might do a naive version, just copy everything in the range
            //or shallow version, just pointing to the existing collection

            //linq, create sublist from LogEntries
            //var newList = this.LogEntries.GetRange((int)sublistStart, (int)length);
            
            Debugger.Break(); //todo just a copy

            //run state up to sublistStart, just create with state and length (copy entire keyboard and R log?)



            var newName = Name + " sub";
            //var newLog = new ExecLog(newName, newList);
            //var newLog = new RecordingLog(newName, this._LogEntries);
            //return dummy
            var newLog = this;

            newLog.CreateMugShots();
            return newLog;
        }

        public long CalcFrameFromSlider(double sliderValue)
        //slider in  range 0,100 both ends possible
        {
            //long totalFrames = _LogEntries.Count;
            long totalFrames = instructionEnd;

            if (instructionStart > 0)
            {
                Debugger.Break(); //this only works for logs from 0
            }
            
            double requestedFrame = sliderValue / 100 * totalFrames;
            if (requestedFrame >= totalFrames) requestedFrame = totalFrames - 1;
            return (long)requestedFrame;
        }


        public WriteableBitmap BmpStart { get { return _bmpStart; } }
        public WriteableBitmap BmpMid { get { return _bmpMid; } }
        public WriteableBitmap BmpEnd { get { return _bmpEnd; } }


        public void CreateMugShots()
        {


            
            //var count = _LogEntries.Count - 1;
            long count = instructionEnd - 1;
            var mid = count / 2;

            //Debugger.Break();  //mugshots a nice test



            Screen.PaintZXScreenVersion5(this[0].ram, _bmpStart);
            Screen.PaintZXScreenVersion5(this[mid].ram, _bmpMid);
            Screen.PaintZXScreenVersion5(this[count].ram, _bmpEnd);

        }

        /*
        //0 init state
        //   step
        //   step
        //   1 - 
        //end
        log with start 0 and end 1 logically contains 2 states
        0 - state before first step
        1 - start after the first step
        so (end-start) = 1, but there are 2 states?

        rendered frames, 0 is the initial state
                         dict.Count -1 is the state after the first step
        */

        public void RenderAvailableFrames()
        {
            long SLIDER_STEPS = 100;
            //create state snapshots for slider access
            //100 steps?

            //create list? dict? of available frames, each indexed by long
            //when accessing rendered frames, pick one closest to the available value
            //(GetApproximateFrame?)

            //indexer was returning the same Spectrum instance, just with different states
            //Cache needs to copy values


            long stepSize = (instructionEnd - instructionStart) / SLIDER_STEPS;

            var state = rec.initialState.Copy();//copy not needed, spectrum already copies everything
            //create new log replaying keyboard
            KeyboardWithPlayback kb = new KeyboardWithPlayback(rec.keyboard, rec.RRegister);
            //create new zx with this state

            //recheck state copies, Spectrum copies ram and regs
            var zx = new Spectrum(state.ramC, state.registersC, kb);

            _renderedFrames[0] = state.Copy(); //
            
            bool stop_error_ignored;
            //do all steps
            for (long i = 0; i < instructionEnd; i++)
            {
                //see if end will be the next index, or the last existing
                zx.Step(out stop_error_ignored);

                if ((i % stepSize) == 0)
                {
                    var stateCopy = CopyStateFromSpectrum(zx.ram, zx);
                    long key = i;
                    //copy the Spectrum state?
                    Debug.Assert(_renderedFrames.ContainsKey(key), "overwriting dict state");

                    _renderedFrames[key] = stateCopy;
                }
            }

            //save last state? ignore?
            if (!_renderedFrames.ContainsKey(instructionEnd))
            {
                _renderedFrames[instructionEnd] = CopyStateFromSpectrum(zx.ram, zx);
            }

        }

        public ExecLogState GetRenderedFrame(long frame)
        {
            return _renderedFrames[frame];
        }


        public long GetClosestRenderedFrameIndex(long requestedFrame)
        {
            //find the closest rendered frame in the _renderedFrames
            long closestFrame = -1;
            long smallestDiff = long.MaxValue;

            foreach (var frame in _renderedFrames.Keys)
            {
                long diff = Math.Abs(frame - requestedFrame);
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    closestFrame = frame;
                }
                if (smallestDiff == 0) break;
            }
            return closestFrame;
        }



        internal void SetLogs(Dictionary<long, byte> mk, Dictionary<long, byte> mr)
        {
            rec.keyboard = mk;
            rec.RRegister = mr;
        }

        internal ExecLogState GetInitialState()
        {
            return rec.initialState.Copy();//copy not needed, spectrum allready copies everything
        }

        public KeyboardWithPlayback CreatePlaybackKeyboard()
        {
            KeyboardWithPlayback kb = new KeyboardWithPlayback(rec.keyboard, rec.RRegister);
            return kb;
        }


    }



}
