using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEscape
{

    public class XZ80Memory : Z80Lib.IMemory
    {
        public XZ80Memory(Spectrum zz)
        {
            //create copy of memory
            ram = zz.CopyOfMemory();
        }
        public byte[] ram = new byte[65536];
        public byte Read(Z80Lib.Z80 cup, ushort adr, bool fuckYou)
        {
            return ram[adr];
        }
        public void Write(Z80Lib.Z80 cpu, ushort adr, byte value)
        {
            ram[adr] = value;
        }
    }
    public class XInputOutput : Z80Lib.IInputOutputPort
    {
        Keyboard m_kbd;
        public XInputOutput(Keyboard kbd)
        {
            m_kbd = kbd;
        }
        public byte Read(Z80Lib.Z80 cpu, ushort port)
        {
            //byte rez = m_kbd.ReadForX(cpu.getContext().bc.b, cpu.getContext().bc.c);
            byte rez = m_kbd.ReadForX(port / 256, port % 256);


            //Debugger.Break();
            //or read from keyboard
            //return 0x1F;
            return rez;
            //return (byte)(port >> 8);
        }
        public void Write(Z80Lib.Z80 cpu, ushort port, byte val)
        {
            //
            //Debugger.Break();
        }
    }



}
