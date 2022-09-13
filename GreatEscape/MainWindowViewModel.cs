using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
//using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xaml;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace GreatEscape
{
    internal class MainWindowViewModel : ObservableObject
    {
        //Spectrum zx = new Spectrum(ge, viz, m_keyboard);


        private Spectrum m_zx;

        private Keyboard m_keyboard;
        private Screen m_screen;

        private DispatcherTimer m_timer;

        //expose public property of type Image
        public WriteableBitmap ScreenBmp { get => screen; set => SetProperty( ref screen, value); }
        private WriteableBitmap screen;
        
        public MainWindowViewModel()
        {
            m_keyboard = new Keyboard();
            m_screen = new Screen(); //lol

            //create an instance of Image
            //Screen = new Image(); ???

            //set the source of the Image to a new BitmapImage
            //Image.Source = new BitmapImage(new Uri(@"pack://application:,,,/GreatEscape;component/Images/GreatEscape.png"));
            //take random image from internet

            //attempt 2 baci
            //Screen.Source = new BitmapImage(new Uri(@"http://www.google.com/intl/en_ALL/images/logo.gif"));

            ScreenBmp = Paint5(new byte[] { 0, 1, 2, 3 } /*ignored*/);



            //string memFileName = "ge.z80";
            //string memFileName = "He.z80";
            //memFileName = "pen.z80"; //penetrator koji ne raid
            string memFileName = "yayfuse.z80";
            byte[] mem = File.ReadAllBytes(memFileName);
            byte[] rom = File.ReadAllBytes("48.rom");


            //todo upgrade forms
            //IMemoryAccessVisualizer viz = new MemoryAccessVisualizer();

            m_zx = new Spectrum(rom, mem, null, m_keyboard, memFileName);
            
            //m_screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);
            Screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);

            //viz.SetSpectrum(zx);

            //22                




            //poke place
            byte[] ram = m_zx.GetRam();

            /*
            //create byte span from ram
            Span<byte> ramSpan = new Span<byte>(ram, 0, 16380);
            //fill ram with random bytes
            Random r = new Random(48);
            r.NextBytes(ramSpan);
            */


            //ram[0xa57f] = 1; // 8 rockets?
            //ram[0xa59c] = 50; //0x32?  32 fragments   32 speed :)

            //remove pause from fireworks, 
            ram[0xa5dc] = 0;
            ram[0xa5dd] = 0;


            //rocket starting coords aa53  cf -> c0
            //ram[0xaa53] = 0xc3;  move starting postion
            //aac9 = tablica adresa 8 ruta, mozda fragments
            byte a = ram[0xaac9];
            byte b = ram[0xaaca];


            //change fragment routes.
            /*
            for (int i = 0; i < 12; i += 2)
            {
                ram[i + 0xaacb] = a;
                ram[i + 0xaacb + 1] = b;
            }*/

        }

        private bool m_instaExitRequested = false;

        public void Start()
        {
            DebugProp = "start clicked";

            //for now big logger starts right away
            //turn this on for convenience

            StartExecLog();



            //create new thread pool dispatcher or reuse
            if( m_timer == null)
            {
                var dispatcher = m_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Render, Timer_Tick, Dispatcher.CurrentDispatcher);
                m_timer.Tick += Timer_Tick;
            }
            CheckBoxTimerEnabled = true;
            m_instaExitRequested = false;
            m_timer.Start();

        }
        private void StopTimer()
        {
            if (m_timer is null) return;
            CheckBoxTimerEnabled = false;
            m_timer.Stop();
        }


        //private int m_timerLoopTimes = 50000; //might be wrong
        private int m_timerLoopTimes = 5000; 
        private void Timer_Tick(object? senderIn, EventArgs e)
        {
            if( senderIn is null) { return; }
            object sender = senderIn;

            //m_screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);
            Screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);

            if (!checkBoxTimerEnabled) return;

            bool abortRequest = 
                
            LoopX(m_timerLoopTimes);
            
            if(abortRequest)
            {
                StopTimer();
            }

        }

        public bool LoopX(int steps) //return true to stop running
        {
            bool rezRequestStop = false;
            bool stop_error;

            CreateProcessorX_IfNeeded();

            /*
            if (procX == null)
            {
                //var ctx = new Z80Lib.Context();
                var ctx = new Z80Lib.Context();

                //var mem = new XZ80Memory(m_zx); //copy
                m_Xmem = new XZ80Memory(m_zx); //copy
                var ports = new XInputOutput(m_keyboard); //?
                ctx.mem = m_Xmem;
                ctx.io = ports;

                //set up regs and pc as well?


                m_zx.CopyRegistersToXZ80(ref ctx);

                //var procX = new Z80Lib.Z80(ctx);
                procX = new Z80Lib.Z80(ctx);
            }*/

            for (int i = 0; i < steps; i++)
            {

                //changing the order, should be the same, if not for the R register hack

                //TODO see what is this
                XStepMeansStep();
                m_zx.Step( out stop_error);

                //hack  force R to be the same


                if (stop_error)
                {
                    ZDis = m_zx.stop_error_string;
                    m_timer.Stop();
                    rezRequestStop = true;
                    break;
                }
                if (m_instaExitRequested) //all this is big bs in single threaded app
                {                
                    m_instaExitRequested = false;
                    
                    rezRequestStop = true;
                    break;
                }



                bool theSame = m_zx.ComparetoXZ80(procX);
                string reason = "";
                if (!theSame) reason = m_zx.CompareAndGiveReason(procX);

                if (!theSame)
                {
                    Debugger.Break();
                }
                xtot++;
            }
            UpdateGUI();
            return rezRequestStop;

        }

        private void CreateProcessorX_IfNeeded()
        {
            if (procX == null)
            {
                //var ctx = new Z80Lib.Context();
                var ctx = new Z80Lib.Context();

                //var mem = new XZ80Memory(m_zx); //copy
                m_Xmem = new XZ80Memory(m_zx); //copy
                var ports = new XInputOutput(m_keyboard); //?
                ctx.mem = m_Xmem;
                ctx.io = ports;

                //set up regs and pc as well?
                m_zx.CopyRegistersToXZ80(ref ctx);


                //var procX = new Z80Lib.Z80(ctx);
                procX = new Z80Lib.Z80(ctx);

                //hack for R register
                m_zx.hack_procX = procX;
            }
        }



        public void Loop(int steps)
        {
            m_zx.LoopSteps(new PictureBoxReplacement(() => UpdateGUI()) , steps);
            UpdateGUI();

            ZDis = m_zx.stop_error_string;
        }


        private void UpdateGUI()
        {

            Screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);
            //m_screen.PaintZXScreenVersion5(m_zx.GetRam(), _writeableBitmap);


            if (m_Xmem != null)
            {

                //this to draw from X emulator
                //m_zx.DrawScreenFromRAM(e.Graphics, m_Xmem.ram);
                //m_screen.Paint4(e.Graphics, m_Xmem.ram);

            }


            //2222222222222

            DebugProp = m_zx.DisplayPC();
            
            string dbg = "";
            if (m_zx.dbg_rez != null) dbg = m_zx.dbg_rez.ToString();
            if (dbg.Length < 100)
            {
                //textBoxRez.Text = m_zx.DisplayRegisters() + dbg;
            }
            else
            {
                //Debugger.Break();
            }
        }







        //DebugProp
        public string DebugProp { get => debugProp; set => SetProperty(ref debugProp, value); }
        private string debugProp = "uninit";

        public bool CheckBoxTimerEnabled { get => checkBoxTimerEnabled; set => SetProperty(ref checkBoxTimerEnabled, value); }
        private bool checkBoxTimerEnabled = true;

        public string StepsPerFrame
        {
            get => $"{m_timerLoopTimes}";
            set => SetProperty(ref m_timerLoopTimes, int.Parse(value));
        }

        public string ZDis { get => zDis; set => SetProperty(ref zDis, value); }
        private string zDis;

        //in fact this is paint version 5
        private WriteableBitmap _writeableBitmap;
        
        public WriteableBitmap Paint5(byte[] ram)
        {
            int XS = 256, YS = 192;
            if (_writeableBitmap == null)
            {
                _writeableBitmap = new WriteableBitmap(XS, YS, 96, 96, PixelFormats.Bgr32, null);
            }

            //update some pixels 

            //11
            try
            {
                // Reserve the back buffer for updates.
                _writeableBitmap.Lock();

                
                unsafe
                {
                    // Get a pointer to the back buffer.
                    IntPtr pBackBuffer = _writeableBitmap.BackBuffer;

                    // Compute the pixel's color.
                    int color_data = 255 << 16; // R
                    color_data |= 128 << 8;   // G
                    color_data |= 255 << 0;   // B
                    
                    // Find the address of the pixel to draw.
                    //pBackBuffer += row * writeableBitmap.BackBufferStride;
                    //pBackBuffer += column * 4;

                    pBackBuffer += 10 * _writeableBitmap.BackBufferStride;
                    pBackBuffer += 10 * 4;

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = color_data;

                    pBackBuffer = _writeableBitmap.BackBuffer;
                    pBackBuffer += 182 * _writeableBitmap.BackBufferStride;
                    pBackBuffer += 245 * 4;
                    *((int*)pBackBuffer) = color_data;
                    
                    //writes two pixels in the opposite corners
                }

                // Specify the area of the bitmap that changed.
                //_writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, XS, YS));

            }
            finally
            {
                // Release the back buffer and make it available for display.
                _writeableBitmap.Unlock();
            }

            //return value is not needed
            return _writeableBitmap;
        }



        #region OLD
        private void PaintZXScreenVersion5BACIBACI(byte[] ram)
        {
            //version 5, use wpf writeable bitmap, draw there.
            //Bitmap pic = new Bitmap(256, 192, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Color cfore, cbackground;

            Debugger.Break();
            
            int screen_start = 16384 + 0;
            int attributes_start = 16384 + 256 * 192 / 8;

            try
            {
                // Reserve the back buffer for updates.
                _writeableBitmap.Lock();

                unsafe
                {
                    // Get a pointer to the back buffer.
                    //IntPtr pBackBuffer = _writeableBitmap.BackBuffer;

                    /*
                    // Find the address of the pixel to draw.
                    pBackBuffer += row * writeableBitmap.BackBufferStride;
                    pBackBuffer += column * 4;

                    // Compute the pixel's color.
                    int color_data = 255 << 16; // R
                    color_data |= 128 << 8;   // G
                    color_data |= 255 << 0;   // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = color_data;
                    */


                    for (int y = 0; y < 192; y++)
                    {

                        //get address y
                        int adr;
                        //adr = screen_start + y * 32;

                        int third = y / 64;
                        int char_row = y % 8;  //this is not row number counted in characters, but row inside a character
                        int block_row = (y % 64) / 8;
                        adr = screen_start + (third * 32 * 8 * 8) + (char_row * 256) + (block_row * 32);

                        //attribute 
                        int adr_attrib = attributes_start + (y / 8) * 32;


                        IntPtr pBackBuffer = _writeableBitmap.BackBuffer;
                        // Find the address of the pixel to draw.
                        pBackBuffer += y * _writeableBitmap.BackBufferStride; 
                        //pBackBuffer += column * 4;   we always start from x=0  (column is x)


                        for (int x = 0; x < 256; x += 8)
                        {
                            int atrbyte = ram[adr_attrib];
                            bool bright = (atrbyte & 0x40) > 0;

                            /* todo the colors
                            if (bright)
                            {
                                //take 3 last bits
                                cfore = m_colors_bright[atrbyte % 8];
                                //take next 3 bits
                                cbackground = m_colors_bright[atrbyte / 8 % 8];
                            }
                            else
                            {
                                cfore = m_colors[atrbyte % 8];
                                cbackground = m_colors[atrbyte / 8 % 8];
                            }
                            */

                            int colFore = 255; //blue?
                            int colBack = 0; //black?

                            byte bitmask = 128;
                            for (int i = 0; i < 8; i++)
                            {
                                int membyte = ram[adr];
                                if ((membyte & bitmask) > 0)
                                {
                                    //pic.SetPixel(x + i, y, cfore);
                                    * ((int*)pBackBuffer) = colFore;
                                }
                                else
                                {
                                    //pic.SetPixel(x + i, y, cbackground);
                                    *((int*)pBackBuffer) = colBack;
                                }
                                pBackBuffer += 4;
                                bitmask = (byte)(bitmask >> 1);
                            }

                            adr++;
                            adr_attrib++;

                        }
                    }

                    //?? g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    //?? g.DrawImage(pic, 0, 0, 256 * 3, 192 * 3);

                }

                // Specify the area of the bitmap that changed.
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, 256, 192));
            }
            finally
            {
                // Release the back buffer and make it available for display.
                _writeableBitmap.Unlock();
            }
        }//end method Paint
        #endregion
        

        internal void Save()
        {
            /*
            string filename = "slika.png";
            BitmapSource image5 = _writeableBitmap.Clone();
            using (FileStream stream5 = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                encoder5.Frames.Add(BitmapFrame.Create(image5));
                encoder5.Save(stream5);
            }*/
            //22
            var ram = this.m_zx.ram;
            //save ram bytes to file
            string path = @"ram64.bin";
            System.IO.File.WriteAllBytes(path, ram);

        }

        internal void PreviewKeyDown(KeyEventArgs e)
        {
            Action<Key, Action<bool>> ac = (k, setter) =>
            {
                if (e.Key == k)
                {
                    setter(true);
                    //e.Handled = true;
                }
            };
            //if ( e.Key == Key.D1) { m_keyboard.SetKey1(true); }

            ac(Key.D1, m_keyboard.SetKey1);
            ac(Key.D2, m_keyboard.SetKey2);
            ac(Key.D3, m_keyboard.SetKey3);
            ac(Key.D4, m_keyboard.SetKey4);
            ac(Key.D5, m_keyboard.SetKey5);

            ac(Key.D6, m_keyboard.SetKey6);
            ac(Key.D7, m_keyboard.SetKey7);
            ac(Key.D8, m_keyboard.SetKey8);
            ac(Key.D9, m_keyboard.SetKey9);
            ac(Key.D0, m_keyboard.SetKey0);

            ac(Key.Q, m_keyboard.SetKeyQ);
            ac(Key.W, m_keyboard.SetKeyW);
            ac(Key.E, m_keyboard.SetKeyE);
            ac(Key.R, m_keyboard.SetKeyR);
            ac(Key.T, m_keyboard.SetKeyT);

            ac(Key.Y, m_keyboard.SetKeyY);
            ac(Key.U, m_keyboard.SetKeyU);
            ac(Key.I, m_keyboard.SetKeyI);
            ac(Key.O, m_keyboard.SetKeyO);
            ac(Key.P, m_keyboard.SetKeyP);

            ac(Key.A, m_keyboard.SetKeyA);
            ac(Key.S, m_keyboard.SetKeyS);
            ac(Key.D, m_keyboard.SetKeyD);
            ac(Key.F, m_keyboard.SetKeyF);
            ac(Key.G, m_keyboard.SetKeyG);


            ac(Key.Enter, m_keyboard.SetKeyEnter);
            ac(Key.L, m_keyboard.SetKeyL);
            ac(Key.K, m_keyboard.SetKeyK);
            ac(Key.J, m_keyboard.SetKeyJ);
            ac(Key.H, m_keyboard.SetKeyH);

            ac(Key.LeftShift, m_keyboard.SetKeyLeftShift);
            ac(Key.Z, m_keyboard.SetKeyZ);
            ac(Key.X, m_keyboard.SetKeyX);
            ac(Key.C, m_keyboard.SetKeyC);
            ac(Key.V, m_keyboard.SetKeyV);

            ac(Key.Space, m_keyboard.SetKeySpace);
            ac(Key.OemComma, m_keyboard.SetKeyOemComma);
            ac(Key.M, m_keyboard.SetKeyM);
            ac(Key.N, m_keyboard.SetKeyN);
            ac(Key.B, m_keyboard.SetKeyB);



        }

        internal void PreviewKeyUp(KeyEventArgs e)
        {
            Action<Key, Action<bool>> ac = (k, setter) =>
            {
                if (e.Key == k)
                {
                    setter(false);
                    //e.Handled = true;
                }
            };
            ac(Key.D1, m_keyboard.SetKey1);
            ac(Key.D2, m_keyboard.SetKey2);
            ac(Key.D3, m_keyboard.SetKey3);
            ac(Key.D4, m_keyboard.SetKey4);
            ac(Key.D5, m_keyboard.SetKey5);

            ac(Key.D6, m_keyboard.SetKey6);
            ac(Key.D7, m_keyboard.SetKey7);
            ac(Key.D8, m_keyboard.SetKey8);
            ac(Key.D9, m_keyboard.SetKey9);
            ac(Key.D0, m_keyboard.SetKey0);

            ac(Key.Q, m_keyboard.SetKeyQ);
            ac(Key.W, m_keyboard.SetKeyW);
            ac(Key.E, m_keyboard.SetKeyE);
            ac(Key.R, m_keyboard.SetKeyR);
            ac(Key.T, m_keyboard.SetKeyT);

            ac(Key.Y, m_keyboard.SetKeyY);
            ac(Key.U, m_keyboard.SetKeyU);
            ac(Key.I, m_keyboard.SetKeyI);
            ac(Key.O, m_keyboard.SetKeyO);
            ac(Key.P, m_keyboard.SetKeyP);

            ac(Key.A, m_keyboard.SetKeyA);
            ac(Key.S, m_keyboard.SetKeyS);
            ac(Key.D, m_keyboard.SetKeyD);
            ac(Key.F, m_keyboard.SetKeyF);
            ac(Key.G, m_keyboard.SetKeyG);

            ac(Key.Enter, m_keyboard.SetKeyEnter);
            ac(Key.L, m_keyboard.SetKeyL);
            ac(Key.K, m_keyboard.SetKeyK);
            ac(Key.J, m_keyboard.SetKeyJ);
            ac(Key.H, m_keyboard.SetKeyH);

            ac(Key.LeftShift, m_keyboard.SetKeyLeftShift);
            ac(Key.Z, m_keyboard.SetKeyZ);
            ac(Key.X, m_keyboard.SetKeyX);
            ac(Key.C, m_keyboard.SetKeyC);
            ac(Key.V, m_keyboard.SetKeyV);

            ac(Key.Space, m_keyboard.SetKeySpace);
            ac(Key.OemComma, m_keyboard.SetKeyOemComma);
            ac(Key.M, m_keyboard.SetKeyM);
            ac(Key.N, m_keyboard.SetKeyN);
            ac(Key.B, m_keyboard.SetKeyB);

        }

        private Z80Lib.Context ctx;
        private Z80Lib.Z80 procX;
        private int xtot = 0;

        private XZ80Memory m_Xmem;


        private bool freshLDIR = true;
        private void XStepMeansStep()
        {
            bool resetToFreshLDIR = false;
            //also do a major do loop that handles LDIR
            bool over = true;
            do
            {
                over = true;
                do
                {
                    procX.Step();
                } while (procX.getContext().prefix != 0);
                //this completes our full instruction, or just one step of LDIR, in that case we will be back on the LDIR

                ushort pc = procX.getContext().pc.w;
                byte instr = procX.getContext().mem.Read(null, pc, false);
                byte instr2 = procX.getContext().mem.Read(null, (ushort)(pc + 1), false);
                if ((instr == 0xED) && ((instr2 == 0xB0) || (instr2 == 0xB8)))   //LDIR or LDDR
                {
                    if (freshLDIR)
                    {
                        freshLDIR = false;
                    }
                    else
                    {
                        //Debugger.Break();
                        over = false; //its not over
                        resetToFreshLDIR = true;
                    }
                }

            } while (!over);
            if (resetToFreshLDIR) freshLDIR = true;
        }


        internal void StartExecLog()
        {
            bool stop_error = false;
            /* this doesnot work before log is created
            m_zx.Step(out stop_error);
            XStepMeansStep();
            bool same = m_zx.ComparetoXZ80(procX);
            Debug.Assert(same);*/


            var log = new ExecLog("Fireworks.");
            var logTester = new ExecLogV1FullSnapshots("Fireworks.");
            m_zx.StartLog(log, logTester);


            if( procX != null)
            {
                //hack for R register copy
                m_zx.hack_procX = procX;

                //create one step you bitch
                m_zx.Step(out stop_error);
                XStepMeansStep();
                bool same2 = m_zx.ComparetoXZ80(procX);
                Debug.Assert(same2);
            }


        }


        //private ObservableCollection<ExecLogV1FullSnapshots> _execLog = new();
        private ObservableCollection<ExecLog> _execLogs = new();


        public ObservableCollection<ExecLog> ExecLogs
        {
            get { return _execLogs; }
            set { _execLogs = value; SetProperty(ref _execLogs, value);  }
        }

        internal void StopExecLog()
        {
            var log = m_zx.StopLog(); //should stop after current instruction

            if (log is null) return;  //emulator didn't even start
            if (log.LogEntries.Count() <= 0) return; //no log entries

            //create movie screenshots
            log.CreateMugShots();

            //save the log in a collection  
            _execLogs.Add(log);
            
            
            
            


            //break out of loop
            m_instaExitRequested = true; //all this is big bs in single threaded app

            StopTimer();
            //CheckBoxTimerEnabled = false;

            //control file handling, should be somewhere else

            //version 1, create just the index of addresses touched
            var executedAdr = log.LogEntries
                .Select(e => e.executedAddress)
                .Distinct();

            //generate control file
            //warning, overwrites existing file
            //ControlFile.CreateTemporaryFile_ForGhidra(executedAdr, log.Name , "ghidra.ctl");

            //version 2, create list of addresses touched and the visited counts
            //from log logentries, generate list of distint adresses, with count of each
            var adrCount = log.LogEntries
                .GroupBy(e => e.executedAddress)
                .Select(g => new { adr = g.Key, count = g.Count() });
                //.ToList();
            int index = 0;
            ControlFile.CreateTemporaryFile_ForGhidra_WithCustomStringGenerator(
                adrCount,
                (e) => $"C ${e.adr:X4} {log.Name} {index++} {e.count}",
                "ghidra.ctl"
            );
            /*xx;
            //sw.WriteLine($"C ${address:X4} {comment} {index++}");
            //11
            (IEnumerable<T> elems, Func<T, string> writer, string fileName)
            //22*/

        }


        internal void LogicalFunctionForGhidra()
        {
            //copy of stop-logging event
            //do log[active+1] and not log[active]

            var log1 = _execLogs[1]; //hack
            var log2 = _execLogs[2];

            //select members of log2 that are not in the log1
            var forbidenAdrs = log1.LogEntries
                .GroupBy(e => e.executedAddress)
                .Select(g => g.Key);

            var filtered = log2.LogEntries
                .GroupBy(e => e.executedAddress)
                .Where(g => !forbidenAdrs.Contains(g.Key))
                .Select(g => new { adr=g.Key, count=g.Count() });


            //for debug, full log2
            //?


            //control file handling, should be somewhere else

            int index = 0;
            ControlFile.CreateTemporaryFile_ForGhidra_WithCustomStringGenerator(
                filtered,
                (e) => $"C ${e.adr:X4} Explosion {index++} {e.count}",
                "ghidra.ctl"
            );
            //Explosion was {log.Name}

        }


        //222222222222222

        private ExecLogV1FullSnapshots _activeLog;
        public ExecLogV1FullSnapshots ActiveLog //bound to listview SelectedItem
        {
            get { return _activeLog; }
            set
            {
                if (_activeLog == value) return;
                SetProperty(ref _activeLog, value);

                //slider position will not be updated 

                //same as slider 
                int requested = 0; //jump to start
                SetZXStateFromLog(_activeLog, requested);
            }

        }

        private void SetZXStateFromLog(ExecLogV1FullSnapshots _activeLog, int requested)
        {
            //this will not work with more than 7FFF FFFF elemes
            ExecLogEntryV1 entry = _activeLog.LogEntries[requested];
            //copy entry to m_zx memory
            m_zx.SetStateScreenOnly(entry);
            UpdateGUI();
        }




        private double _sliderValue = 0;
        internal void SliderValueChanged(RoutedPropertyChangedEventArgs<double> e, object dc)
        {
            //var a = dc
            var log = dc as ExecLog;
            if (log is null)
            {
                //take the first log in the collection if it exists
                if (ExecLogs.Count() > 0)
                {
                    log = ExecLogs.First();
                }
                else
                {
                    return;
                }
            }

            _sliderValue = e.NewValue;

            //set the new emulator state from the value

            long requestedFrame = log.CalcFrameFromSlider(_sliderValue);


            //not working if log has 0 frames
            if (requestedFrame < 0) return; //??


            SetZXStateFromLog(log, (int)requestedFrame);

            //dissasembler state?
            UpdateVarWatchers(log, (int)requestedFrame);

        }

        private void UpdateVarWatchers(ExecLogV1FullSnapshots _activeLog, int requested)
        {

            ExecLogEntryV1 entry = _activeLog.LogEntries[requested];

            int ix = 0xa5df;
            int adr = ix + 6; //, ix is? a5df
            byte six = entry.ramC[adr];

            string rez = "all ix+6  ";
            for (int i = 0; i < 8; i++)
            {
                int sixerA = ix + (19 * i) + 18;
                int sixer = entry.ramC[sixerA];
                rez = rez + $" {sixer:X2}   ";
            }


            //DbgWatchVars = $"6: {six:X2}";
            DbgWatchVars = rez;;

            //< TextBlock Grid.Row = "3" Text = "{Binding DbgWatchVars}" />
        }

        private string _dbgWatchVars;
        public string DbgWatchVars
        {
            get { return _dbgWatchVars; }
            set
            {
                SetProperty<string>(ref _dbgWatchVars, value);
            }
        }


        private ExecLogV1FullSnapshots? ActiveOrFirst()
        {
            var log = ActiveLog;

            //var log = dc as ExecLog;  //slider dataContext is also bound to SelectedItem
            if (log is null)
            {
                //take the first log in the collection if it exists
                if (ExecLogs.Count() > 0)
                {
                    log = ExecLogs.First();
                }
            }
            return log;
        }




        private IObservable<int> m_debouncePipe;

        public void Disassemble()
        {
            string dis = m_zx.Disassemble();
            ZDis = dis;
        }

        internal void SliderDebounced(RoutedEventArgs e)
        {
            var ev = e as
                RoutedPropertyChangedEventArgs<double>;
            Disassemble();
        }


        private long _ssStart;
        private long _ssEnd;
        public string ZssStart
        {
            get { return $"{_ssStart}"; }
        }
        public string ZssEnd
        {
            get { return $"{_ssEnd}"; }
        }

        internal void CreateNewFromSelection()
        {
            var parent = ActiveOrFirst();
            if (parent is null) return;

            //reverse params if end is before start

            var log = parent.CreateSubLog(Math.Min(_ssStart, _ssEnd), Math.Abs( _ssEnd - _ssStart));
            if( log.LogEntries.Count() == 0)
            {
                Debug.Assert(false, "zero len log in the system");
            }
            
            _execLogs.Add(log);
        }

        internal void SetSelectionStart()
        {
            var log = ActiveOrFirst();
            if (log is null) return;
            _ssStart = log.CalcFrameFromSlider(_sliderValue);
        }

        internal void SetSelectionEnd()
        {
            var log = ActiveOrFirst();
            if (log is null) return;
            _ssEnd = log.CalcFrameFromSlider(_sliderValue);
        }
    } //end class MainWindowViewModel



}
