using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEscape
{
    internal class ControlFile
    {


        public static void CreateTemporaryFile_ForCrashDissasembly(int ipc)
        {
            //create temporary ctl file, that will have our unknown instruction
            string ctlText = 
                $"c ${ipc - 14:X4} UNK \r\n  ${ipc:X4},1 comment for unknown ins  \r\nD ${ipc + 2:X4} commD \r\n@ $800A label=compare_none\r\nC $800E,1 A button was pressed, continue. \r\n"
                //+ $"t $85cd texts\r\n"
                //+ $"c $8824 Wait key routine\r\n"
                //+ $"@ $8824 label=hp_loop\r\n"
                + $"i ${ipc+30:X4}\r\n"
                ;
            File.WriteAllText("temp.ctl", ctlText);

            /*
                + $"i ${ipc + 2100:X4}\r\n"
             * 
             **/
        }


        // temporary file for Ghidra with every instruction commented

        public static void CreateTemporaryFile_ForGhidra(IEnumerable addresses, string comment, string fileName)
        {
            //delete old temporary file
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            int index = 0;
            //open file for text writing
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                //write every instruction to the file
                foreach (var address in addresses)
                {
                    sw.WriteLine($"C ${address:X4} {comment} {index++}");
                }
            }
        }

        public static void CreateTemporaryFile_ForGhidra_WithCustomStringGenerator<T>
            (IEnumerable<T> elems, Func<T, string>writer, string fileName)
        {
            //delete old temporary file
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            //open file for text writing
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                //write every instruction to the file
                foreach (var address in elems)
                {
                    sw.WriteLine ( writer(address) );
                    //sw.WriteLine($"C ${address:X4} {comment} {index++}");
                }
            }
        }



    }
}
