using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreatEscape
{
    public class Keyboard
    {
        private bool key1 = false;
        private bool key2 = false;
        private bool key3 = false;
        private bool key4 = false;

        private bool key0 = false;

        private bool keyY = false;
        private bool keySpace = false;
        private bool keyBreak = false;

        public bool key1pressed()
        {
            return key1;
        }
        public void SetKey1(bool s)
        {
            key1 = s;
        }
        public void SetKey2(bool s)
        {
            key2 = s;
        }
        public void SetKey3(bool s)
        {
            key3 = s;
        }
        public void SetKey4(bool s)
        {
            key4 = s;
        }


        internal void SetKey0(bool s)
        {
            key0 = s;
        }

        internal void SetKeyY(bool s)
        {
            keyY = s;
        }
        internal void SetKeySpace(bool s)
        {
            keySpace = s;
        }
        internal void SetKeyBreak(bool s)
        {
            keyBreak = s;
        }



        public bool ReadInPort(int portH, int portL, out byte a)
        {
            //read zx spectrum keys, only some combos of b and c will be valid
            if (portL == 0xFE) //all key ports share this c
            {
                switch (portH)
                {
                    case 0xF7:
                        if (key1)
                        {
                            a = 0x1E;  //all 5 bits on except the last (key 1)
                            a = 0b11110;
                            return true;
                        }
                        if (key2)
                        {
                            a = 0b11101;
                            return true;
                        }
                        if (key3)
                        {
                            a = 0b11011;
                            return true;
                        }
                        if (key4)
                        {
                            a = 0b10111;
                            return true;
                        }
                        break;
                    case 0xEF:
                        if (key0)
                        {
                            a = 0b11110;  //zero press
                            return true;
                        }
                        break;
                    case 0xDF:
                        if (keyY)
                        {
                            a = 0b01111;  //y press
                            return true;
                        }
                        break;
                    case 0x7F:
                        if (keySpace)
                        {
                            a = 0b11110;
                            return true;
                        }
                        break;
                    case 0xFE:
                        if (keyBreak)
                        {
                            a = 0b11110;
                            return true;
                        }
                        break;
                    case 0xFB:   //Q W E R T
                        break;
                    case 0xBF:   //enter, L K J H
                        break;
                    case 0xFD:   //A S D F G
                        break;

                    case 0x0: //in the case of "any" key, the "1" will be that "any key"
                        //always return true; (for penetrator quick start)
                        //a = 0b11110; return true;

                        if (key1)
                        {
                            a = 0x1E;  //all 5 bits on except the last (key 1)
                            a = 0b11110;
                            return true;
                        }
                        break;
                    case 0x7E:   //port of unknown origin, lets return 0x1f and hope for the best
                        a = 0b11111;
                        return true;

                    default:
                        Debug.Assert(false, "unknown FE port");
                        Debugger.Break();
                        break;

                }

            }
            //  bit 0     1   2 3 4 

            //FEFE  shift Z   X C V
            //FDFE  A     S   D F G
            //FBFE  Q     W   E R T
            //F7FE  1     2   3 4 5
            //EFFE  0     9   8 7 6
            //DFFE  P     O   I U Y
            //BFFE  enter L   K J H
            //7FFE  space Sym M N B

            a = 0;
            return false;

        }

        public byte ReadForX(int portH, int portL)
        {
            //reusing the same keyboard class for interpreter X
            //int b = cpu.getContext().bc.b;
            //int c = cpu.getContext().bc.c;

            byte rez;
            if (ReadInPort(portH, portL, out rez))
            {
                return rez;
            }


            return 0x1F; //5 keys all unpressed
        }



    }




}
