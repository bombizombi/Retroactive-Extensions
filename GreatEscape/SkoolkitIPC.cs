using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEscape
{
    public sealed class SkoolkitIPC
    {
        private string m_z80File;  //snapshot file

        public SkoolkitIPC(string z80File)
        {
            m_z80File = z80File;
        }

        public List<DisLine> Disassemble(int pc) //return ?
        {
            //return a List of { adress, line disassembled} objects


            //ipc = original pc at the start of the instruction
            //call external program capturing the output

            ControlFile.CreateTemporaryFile_ForCrashDissasembly(pc);

            //string ctlText = $"c ${ipc-14:X4} UNK \r\n  ${ipc:X4},2 comment for unknown ins  \r\nD ${ipc+2:X4} commD \r\n@ $800A label=compare_none\r\nC $800E,1 A button was pressed, continue. \r\ni ${ipc+19:X4}";
            //File.WriteAllText("temp.ctl", ctlText);

            FileInfo fileInfo = new FileInfo(@"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\yayfuse.z80");
            var dir = fileInfo.DirectoryName;

            //dissasemble it via snatoskool, capturing output
            var proc = new Process();
            //proc.StartInfo.FileName = "sna2skool.py";
            //proc.StartInfo.WorkingDirectory = @"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\";
            proc.StartInfo.WorkingDirectory = dir; // @"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\";
            proc.StartInfo.FileName = @"c:\b\anaconda3\python.exe";
            proc.StartInfo.Arguments =
            @"""G:\cs\Spectrum_emulator\skoolkit\skoolkit-8.6\sna2skool.py""  -H -c temp.ctl "
                //+ "yayfuse.z80";
                + m_z80File;

            //yayfuse was at g:\

            //proc.StartInfo.Arguments = $" - H -c temp.ctl yayfuse.80";
            proc.StartInfo.UseShellExecute = false; //changed
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();


            //string output = proc.StandardOutput.ReadToEnd();  save lines instead
            var lines = new List<string>();
            string? rl;
            do
            {
                rl = proc.StandardOutput.ReadLine();
                if (rl is null) break;
                lines.Add(rl);
            } while (true);

            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            int exitCode = proc.ExitCode;
            //proc.Close();
            //return output;



            //var rez = Parse(output);
            var rez = new List<DisLine>();
            foreach (var line in lines)
            {
                if (line.Length < 6) continue;
                //take characters 2 to 6 from line
                string sadr = line.Substring(2, 4);
                bool suc = int.TryParse(sadr, System.Globalization.NumberStyles.HexNumber, null, out int adr);
                //int adr = int.Parse(sadr, System.Globalization.NumberStyles.HexNumber);
                if (!suc) continue;
                string disassembled = line.Substring(6);
                rez.Add(new DisLine( adr, disassembled ));
            }

            return rez;
            //return output + "\r\n\r\n" + error;



        }

        //just to make it compile, the code moved from Spectrum
        public static string DisassembleTemp(int ipc, string z80File)
        {
            //ipc = original pc at the start of the instruction
            //call external program capturing the output


            ControlFile.CreateTemporaryFile_ForCrashDissasembly(ipc);

            //string ctlText = $"c ${ipc-14:X4} UNK \r\n  ${ipc:X4},2 comment for unknown ins  \r\nD ${ipc+2:X4} commD \r\n@ $800A label=compare_none\r\nC $800E,1 A button was pressed, continue. \r\ni ${ipc+19:X4}";
            //File.WriteAllText("temp.ctl", ctlText);

            FileInfo fileInfo = new FileInfo(@"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\yayfuse.z80");
            var dir = fileInfo.DirectoryName;

            //dissasemble it via snatoskool, capturing output
            var proc = new Process();
            //proc.StartInfo.FileName = "sna2skool.py";
            //proc.StartInfo.WorkingDirectory = @"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\";
            proc.StartInfo.WorkingDirectory = dir; // @"G:\cs\Spectrum_emulator\GreatEscape\GreatEscape\bin\Debug\net6.0-windows\";
            proc.StartInfo.FileName = @"c:\b\anaconda3\python.exe";
            proc.StartInfo.Arguments =
            @"""G:\cs\Spectrum_emulator\skoolkit\skoolkit-8.6\sna2skool.py""  -H -c temp.ctl "
                //+ "yayfuse.z80";
                //+ m_z80File;
                + z80File;

            //yayfuse was at g:\

            //proc.StartInfo.Arguments = $" - H -c temp.ctl yayfuse.80";
            proc.StartInfo.UseShellExecute = false; //changed
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            int exitCode = proc.ExitCode;
            //proc.Close();
            //return output;





            return output + "\r\n\r\n" + error;






        }
    }

    [DebuggerDisplay("{Address} {Disassembled}")]
    public sealed class DisLine
    {
        private int address;
        private string disassembled;

        public string Address { get => $"${address:X4}"; }
        public string Disassembled { get => disassembled; set => disassembled = value; }
        public string Comment { get; set; } 

        public DisLine(int adr, string dis)
        {
            address = adr;
            Disassembled = dis;
        }
    }
}
