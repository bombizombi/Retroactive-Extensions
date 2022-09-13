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

    public sealed class ExecLogState
    {

    }


    
    public sealed class ExecLog //v2
    {
                
        public string Name;
        //public List<ExecLogEntry> LogEntries;
        public List< Func<ExecLogState,ExecLogState> > LogEntries;  //

        
        WriteableBitmap _bmpStart;
        WriteableBitmap _bmpMid;
        WriteableBitmap _bmpEnd;

        public ExecLog(string name)
        {
            Name = name;
            LogEntries = new List<Func<ExecLogState,ExecLogState>>();

            _bmpStart = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpMid = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);
            _bmpEnd = new WriteableBitmap(256, 192, 96, 96, PixelFormats.Bgr32, null);

        }


        public ExecLogEntry this[int index]
        {
            //currently both indexer and public list are available
            get {
                //will not work
                Debugger.Break();
                //return LogEntries[i];

                //something like this should
                var state = LogEntries[0](null);
                for (int i = 1; i <= index; i++)
                {
                    LogEntries[i](state);
                }
                return state;

            }
        }

        //was red this way
        //            ExecLogEntryV1 entry = _activeLog.LogEntries[requested];



        public WriteableBitmap BmpStart { get { return _bmpStart; } }
        public WriteableBitmap BmpMid { get { return _bmpMid; } }
        public WriteableBitmap BmpEnd { get { return _bmpEnd; } }
        public void CreateMugShots()
        {
            var count = LogEntries.Count - 1;
            var mid = count / 2;

            Debugger.Break();  //mugshots a nice test

            Screen.PaintZXScreenVersion5(LogEntries[0].ramC, _bmpStart);
            Screen.PaintZXScreenVersion5(LogEntries[mid].ramC, _bmpMid);
            Screen.PaintZXScreenVersion5(LogEntries[count].ramC, _bmpEnd);
        }


    }



}
