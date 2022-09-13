using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GreatEscape
{
    public class Keyboard
    {
        private bool key1 = false;
        private bool key2 = false;
        private bool key3 = false;
        private bool key4 = false;
        private bool key5 = false;
        
        private bool key6 = false;
        private bool key7 = false;
        private bool key8 = false;
        private bool key9 = false;
        private bool key0 = false;

        private bool keyQ = false;
        private bool keyW = false;
        private bool keyE = false;
        private bool keyR = false;
        private bool keyT = false;

        private bool keyY = false;
        private bool keyU = false;
        private bool keyI = false;
        private bool keyO = false;
        private bool keyP = false;

        private bool keyA = false;
        private bool keyS = false;
        private bool keyD = false;
        private bool keyF = false;
        private bool keyG = false;

        private bool keyEnter = false;
        private bool keyL = false;
        private bool keyK = false;
        private bool keyJ = false;
        private bool keyH = false;

        private bool keyCaps = false;
        private bool keyZ = false;
        private bool keyX = false;
        private bool keyC = false;
        private bool keyV = false;

        private bool keySpace = false;
        private bool keySymbol = false;
        private bool keyM = false;
        private bool keyN = false;
        private bool keyB = false;



        //private bool keyBreak = false;

        public bool key1pressed() { return key1; }
        
        //12345
        public void SetKey1(bool s) { key1 = s; }
        public void SetKey2(bool s) { key2 = s; }
        public void SetKey3(bool s) { key3 = s; }
        public void SetKey4(bool s) { key4 = s; }
        public void SetKey5(bool s) { key5 = s; }
        
        //09876
        internal void SetKey0(bool s) { key0 = s; }
        internal void SetKey9(bool s) { key9 = s; }
        internal void SetKey8(bool s) { key8 = s; }
        internal void SetKey7(bool s) { key7 = s; }
        internal void SetKey6(bool s) { key6 = s; }

        //qwert
        internal void SetKeyQ(bool s) { keyQ = s; }
        internal void SetKeyW(bool s) { keyW = s; }
        internal void SetKeyE(bool s) { keyE = s; }
        internal void SetKeyR(bool s) { keyR = s; }
        internal void SetKeyT(bool s) { keyT = s; }


        //yuiop
        internal void SetKeyY(bool s) { keyY = s;  }
        internal void SetKeyU(bool s) { keyU = s; }
        internal void SetKeyI(bool s) { keyI = s; }
        internal void SetKeyO(bool s) { keyO = s; }
        internal void SetKeyP(bool s) { keyP = s;  }


        //FDFE  A     S   D F G
        internal void SetKeyA(bool s) { keyA = s; }
        internal void SetKeyS(bool s) { keyS = s; }
        internal void SetKeyD(bool s) { keyD = s; }
        internal void SetKeyF(bool s) { keyF = s; }
        internal void SetKeyG(bool s) { keyG = s; }


        //enter L   K J H
        internal void SetKeyEnter(bool s) { keyEnter = s; }
        internal void SetKeyL(bool s) { keyL = s; }
        internal void SetKeyK(bool s) { keyK = s; }
        internal void SetKeyJ(bool s) { keyJ = s; }
        internal void SetKeyH(bool s) { keyH = s; }


        //space symbol M N B
        internal void SetKeySpace(bool s) { keySpace = s; }
        internal void SetKeyOemComma(bool s) { keySymbol = s; }
        internal void SetKeyM(bool s) { keyM = s; }
        internal void SetKeyN(bool s) { keyN = s; }
        internal void SetKeyB(bool s) { keyB = s; }

        //caps Z X C V
        internal void SetKeyLeftShift(bool s) { keyCaps = s; }
        internal void SetKeyZ(bool s) { keyZ = s; }
        internal void SetKeyX(bool s) { keyX = s; }
        internal void SetKeyC(bool s) { keyC = s; }
        internal void SetKeyV(bool s) { keyV = s; }



        public bool ReadInPort(int portH, int portL, out byte a)
        {
            //you should be able to press two keys at once in the same segment
            int retvalue = 0x1F;

            //read zx spectrum keys, only some combos of b and c will be valid
            if (portL == 0xFE) //all key ports share this c
            {
                switch (portH)
                {
                    case 0xF7:
                        if (key1) { a = 0b11110; return true; }
                        if (key2) { a = 0b11101; return true; }
                        if (key3) { a = 0b11011; return true; }
                        if (key4) { a = 0b10111; return true; }
                        if (key5) { a = 0b01111; return true; }
                        break;
                    case 0xEF:
                        if (key0) { a = 0b11110; return true; }
                        if (key9) { a = 0b11101; return true; }
                        if (key8) { a = 0b11011; return true; }
                        if (key7) { a = 0b10111; return true; }
                        if (key6) { a = 0b01111; return true; }
                        break;
                    case 0xFB:   //Q W E R T
                        if (keyQ) { a = 0b11110; return true; }
                        if (keyW) { a = 0b11101; return true; }
                        if (keyE) { a = 0b11011; return true; }
                        if (keyR) { a = 0b10111; return true; }
                        if (keyT) { a = 0b01111; return true; }
                        break;
                    case 0xDF:
                        //if (keyP) { a = 0b11110; return true; }
                        //if (keyO) { a = 0b11101; return true; }
                        //if (keyI) { a = 0b11011; return true; }
                        //if (keyU) { a = 0b10111; return true; }
                        //if (keyY) { a = 0b01111; return true; }
                        if (keyP) retvalue &= 0b11110;
                        if (keyO) retvalue &= 0b11101;
                        if (keyI) retvalue &= 0b11011;
                        if (keyU) retvalue &= 0b10111;
                        if (keyY) retvalue &= 0b01111;
                        a = (byte)retvalue; return true;
                        break;
                    case 0xFD:   //A S D F G
                        if (keyA) { a = 0b11110; return true; }
                        if (keyS) { a = 0b11101; return true; }
                        if (keyD) { a = 0b11011; return true; }
                        if (keyF) { a = 0b10111; return true; }
                        if (keyG) { a = 0b01111; return true; }
                        break;
                    case 0xBF:   //enter, L K J H
                        //this will not work for more than one key in this segment
                        if (keyEnter) { a = 0b11110; return true; }
                        if (keyL) { a = 0b11101; return true; }
                        if (keyK) { a = 0b11011; return true; }
                        if (keyJ) { a = 0b10111; return true; }
                        if (keyH) { a = 0b01111; return true; }
                        break;
                    case 0x7F:
                        if (keySpace)  { a = 0b11110; return true; }
                        if (keySymbol) { a = 0b11101; return true; }
                        if (keyM)      { a = 0b11011; return true; }
                        if (keyN)      { a = 0b10111; return true; }
                        if (keyB)      { a = 0b01111; return true; }
                        break;
                    case 0xFE:

                        if (keyCaps) { a = 0b11110; return true; }
                        if (keyZ)    { a = 0b11101; return true; }
                        if (keyX)    { a = 0b11011; return true; }
                        if (keyC)    { a = 0b10111; return true; }
                        if (keyV)    { a = 0b01111; return true; }
                        
                        
                        //if (keyBreak) {  a = 0b11110; return true; }
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

            //what happens with this false 
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
