using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Controls;

namespace GreatEscape
{



    public sealed class Spectrum
    {
        HashSet<string> debug_nameSet = new HashSet<string>();



        private long aam_instruction_counter = 0;
        private IMemoryAccessVisualizer m_vizmem;
        private Keyboard m_keyboard;
        private string m_z80File;

        private byte m_opcode;
        private byte m_extOpcode; //used for ED, DD... (all four)

        private bool m_execLogEnabled = false;

        private RecordingLog m_recLog;
        private ExecLog m_execLogLogger;  
        private ExecLogV1FullSnapshots m_execLogTester; //yeah baby run all three

        public int highWaterMark = 0;


        List<ushort> m_pclog;
        private void SavePCLog()
        {
            //save program counter values
            if (m_pclog == null) m_pclog = new List<ushort>();
            m_pclog.Add(pc);
        }


        private bool DebugHelperAddLogInstruction(ushort p, ushort pend,
            Func<ExecLogState, ExecLogState> replayer)
        {
            //just forward the call
            return m_execLogLogger.AddLogInstruction((ushort)p, pend, replayer);
        }
        private Func<ExecLogState, ExecLogState> DebugHelperCreateLogRep(LogStateMaker f)
        {
            return f();
        }


        public void Step(out bool stop_error)
        {
            stop_error = false;
            //string hexadr = String.Format("0x{0:X4}", pc);
            //log current pc
            //m_vizmem.NewPC(pc);

            aam_instruction_counter++;

            //p and executedAddress will hold the same value, the pc at the start (p just for debugging)
            //pc will hold the address at the end (of the next instruction, ofc)

            int p = pc;
            executedAddress = pc;
            int instruction = ram[p];
            m_opcode = (byte)instruction;
            pc++;

            

            //DEBUG SECTION
            if (p < 16384)
            {
                Debug.Assert(false, "executing empty ROM");
            }

            //if (aam_instruction_counter == 0x320) Debugger.Break();

            /*
            if (dbg_logInstructions)
            {
                //Debug.Print("test " + pc);
                string line = String.Format("pc: {0:X4}      de: {1:X4}  bc: {2:X4}  hl: {3:X4}  a: {4:X2}", p, d * 256 + e, b * 256 + c, h * 256 + l, a);
                Debug.Print(line);
            }*/



            //END DEBUG SECTION

            //LOG

            //little wierd that we log before executing, will trigger the test


            //temp ce radit prije jer ionako spremamo samo adresu
            //first log entry is just the loaded state from z80 file


            // after the step, the last log should be equal the current state

            // m_zx -> state before this instruction
            // log -> state before last instruction

            /* baci
            //log testing
            if(m_execLogEnabled)
            {
                //get last log state
                ExecLogEntry? entry = m_execLog.GetLast();
                if( entry is not null)
                {
                    if( !entry.CompareToSpectrumState(this))
                    {
                        Debug.Assert(false, "Last log no good.");
                    }
                }
            }*/



            if (m_execLogEnabled)
            {
                //if ( m_execLog.LogInstruction((ushort)p, ram, this))
                //logging should move after the instruction
                //if (m_execLog.AddLogInstruction ((ushort)p, ram, this))
                if (false) //still not sure what to do about this
                {
                    m_instaExitRequested = true;
                    stop_error = true;
                    return;
                }


                /*
                xx
                moved to LogInstruction
                if (ExecLogEntry.MemoryFull)
                {
                    m_instaExitRequested = true;
                    stop_error = true;
                    return;
                }
                var e = new ExecLogEntry((ushort)p, ram, this );
                m_execLog.Add(e);

                //if execlog will do incremental updates, check if it gets the same state back
                //get last log state
                ExecLogEntry? entry = m_execLog.GetLast();
                if (entry is not null)
                {
                    if (!entry.CompareToSpectrumState(this))
                    {
                        //we need to get back the same state
                        Debug.Assert(false, "Last log no good.");
                    }
                }
                */


            }


            //END LOGGING section



            var toExecute = m_opcodes[instruction];
            var toLog = m_execLogEnabled ? m_loggingOpcodes[instruction] : null;


            //extended instructions in the switch?
            //extended log instructions?  when reading the action for instruction, always
            //take the logaction as well


            if (toExecute is null)
            {
                m_extOpcode = ram[pc++];
                //try extented
                //if i increase pc here, the old method will not work?
                switch (instruction)
                {
                    case 0xED:
                        toExecute = m_ED_opcodes[m_extOpcode];
                        toLog = m_execLogEnabled ? m_ED_loggingOpcodes[m_extOpcode] : null;
                        break;
                    case 0xDD: //IX instructions
                        toExecute = m_DD_opcodes[m_extOpcode];
                        toLog = m_execLogEnabled ? m_DD_loggingOpcodes[m_extOpcode] : null;
                        break;
                    default:
                        //normal instructions not handled by the table will end up here
                        //Debugger.Break(); //should stop on all others?
                        break;
                }

                //temp hack workaround
                if (toExecute is null)
                {
                    pc--;
                }



            }


            if (toExecute is not null)
            { //should go away after all instructions are here

                toExecute();

                //log after execution

                if (m_execLogEnabled)
                {
                    /* there is no logging in baseball
                    Debug.Assert(toLog is not null, "Take log action at the same moment when taking normal action.");
                    //var x = m_loggingOpcodes[instruction];


                    //var replayer = x();
                    //Func<ExecLogState, ExecLogState> replayer = x();
                    Func<ExecLogState, ExecLogState> replayer = DebugHelperCreateLogRep(toLog);


                    //if (m_execLog.AddLogInstruction((ushort)p, replayer)) //perhaps more params   ram, this))
                    if (DebugHelperAddLogInstruction((ushort)p, pc, replayer)) //perhaps more params   ram, this))
                    {
                        //perhaps some other action would be better on memory full condition
                        m_instaExitRequested = true;
                        stop_error = true;
                        return;
                    }*/

                    /*
                    //debug
                    string mingledName = replayer.Method.Name;
                    string t = replayer.Target.GetType().Name;

                    if ( !debug_nameSet.Contains( mingledName) ){
                        Debug.WriteLine($"logmethod {m_opcode:X2} mingled name: {mingledName} target: {t}");
                        debug_nameSet.Add(mingledName);
                    }
                    */

                }

                return;
            }




            //oldins

            if (instruction == 0x25)
            {
                I_DecH();
                return;
            }
            if (instruction == 0xC2)
            {
                I_JpNZ_Adr(); return;
            }
            if (instruction == 0x10)
            {
                I_DJNZ_r();
                return;
            }
            if (instruction == 0xD9)
            {
                EXX();
                return;
            }


            //2021 05 13
            if (instruction == 0x0D)
            {
                DEC_C();
                return;
            }
            if (instruction == 0x08)
            {
                EX_AFAF();
                return;
            }
            if (instruction == 0x3D)
            {
                DEC_A();
                return;
            }
            /*if ((instruction & 0xC7) == 0x06)
            {
                // 00rr r110  LD r, n
                LD_reg_n(instruction); //and register 
                return;
            }*/
            if (instruction == 0x26)
            {
                Debugger.Break(); //this should never be reached because it should be handled in the universal LD_reg_n();
                LD_H_N();
                return;
            }
            if (instruction == 0xC3)
            {
                JP_NN();
                return;
            }
            /*if (instruction == 0xCD)
            {
                CALL_NN();
                return;
            }*/
            /*if (instruction == 0x01)
            {
                LD_BC_NN();
                return;
            }*/
            if (instruction == 0x1E)
            {
                LD_E_N();
                return;
            }


            //extended instructions
            if (instruction == 0xED)
            {
                int ext_instruction = ram[pc];
                pc++;


                if (ext_instruction == 0x78)
                {
                    IN_A_C();
                    return;
                }
                if (ext_instruction == 0xA0)
                {
                    LDI();
                    return;
                }
                if (ext_instruction == 0xB0)
                {
                    LDIR();
                    return;
                }
                if (ext_instruction == 0x73)
                {
                    LD_NNI_SP();
                    return;
                }
                if (ext_instruction == 0x7B)
                {
                    LD_SP_NNI();
                    return;
                }
                if ((ext_instruction & 0b11001111) == 0b01001011)
                {
                    LD_RR_NNI(ext_instruction);
                    return;
                }

                if (ext_instruction == 0x43)
                {
                    LD_NNI_BC();
                    return;
                }
                if (ext_instruction == 0x53)
                {
                    LD_NNI_DE();
                    return;
                }
                if ((ext_instruction & 0b11001111) == 0b01000010)
                {
                    SBC_HL_ss(ext_instruction);
                    return;
                }
                if (ext_instruction == 0x44)
                {
                    NEG();
                    return;
                }
                if (ext_instruction == 0x67)
                {
                    RRD();
                    return;
                }
                if (ext_instruction == 0xb8)
                {
                    LDDR();
                    return;
                }

                if (ext_instruction == 0x5F)
                {
                    LD_A_R();
                    return;
                }


                Debug.Assert(false, "unknown ED instruction");
            }




            if (instruction == 0x2F)
            {
                CPL();
                return;
            }
            /*if (instruction == 0xE6)
            {
                AND_N();  //and imediate avlue
                return;
            }*/




            /*
            if ((instruction & 0xC4) == 0x04)      //this is wrong, should be & 0xc7,  mask all the improtant bits
            {    // 00 rrr 100       INC r
                INC_R(instruction);
                return;
            }*/

            /*if ((instruction & 0xF8) == 0xA0) //take higher 5 bits and compare 1111 1000 1010 0000
            {
                // 1010 0rrr   AND r
                AND_reg(instruction); //and register 
                return;
            }*/

            /*if (instruction == 0x28)
            {
                JR_Z_E();
                return;
            }*/
            if (instruction == 0x06)
            {
                LD_B_N();
                return;
            }
            if (instruction == 0x7B)
            {
                LD_A_E();
                return;
            }

            if (instruction == 0xc8)
            {
                RET_Z();
                return;
            }
            /*if (instruction == 0x3E)
            {
                LD_A_N();
                return;
            }*/
            /*if (instruction == 0xc9)
            {
                RET();
                return;
            }*/
            /*if (instruction == 0xFE)
            {
                CP_N();
                return;
            }*/
            if (instruction == 0x21)
            {
                LD_HL_NN();
                return;
            }
            if (instruction == 0x34)
            {
                INC_HLI();
                return;
            }
            if (instruction == 0x7E)
            {
                LD_A_HLI();
                return;
            }
            if (instruction == 0xC0)
            {
                RET_NZ();
                return;
            }
            if (instruction == 0xE5)
            {
                PUSH_HL();
                return;
            }
            /*if (instruction == 0x3A)
            {
                LD_A_NNI();
                return;
            }*/
            if (instruction == 0xBE)
            {
                CP_HLI();
                return;
            }
            /*if (instruction == 0x11)
            {
                LD_DE_NN();
                return;
            }*/
            if (instruction == 0xE1)
            {
                POP_HL();
                return;
            }

            //bit testing CB instructions
            if (instruction == 0xCB)
            {
                int cb_instruction = ram[pc];
                pc++;

                int mask = 0xC7;                          //  b 1100 0111;
                if ((cb_instruction & mask) == 0x46)
                {    //  b 01?? ?110
                    BIT_B_HLI(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00111000)
                { // 0011 1rrr SRL r
                    SRL_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00011000)
                {
                    RR_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00001000)
                {
                    RRC_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00010000)
                {
                    RL_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00000000)
                {
                    RLC_reg(cb_instruction);
                    return;
                }

                if ((cb_instruction & 0b11000000) == 0b01000000)
                {
                    BIT_x_reg(cb_instruction);
                    return;
                }

                if ((cb_instruction & 0b11000000) == 0b10000000)
                {
                    RES_x_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11000000) == 0b11000000)
                {
                    SET_x_reg(cb_instruction);
                    return;
                }
                if ((cb_instruction & 0b11111000) == 0b00100000)
                {
                    SLA_reg(cb_instruction);
                    return;
                }


                Debug.Assert(false, "unknown CB instruction");
            }

            if (instruction == 0x2A)
            {
                LD_HL_NNI();
                return;
            }
            if (instruction == 0x01)
            {
                //LD_BC_NN();
                return;
            }

            /*if ((instruction & 0xC0) == 0x40)
            //this instruction patter overwrites HLT instruction, which would be LD (HL), (HL) in this logic
            {   // 01 rrr xxx   LD r, x
                LD_R_RPRIME(instruction);
                if (instruction == 0x78)
                {
                    //LD_A_B
                    return;
                }
                return;
            }*/
            /*
            if (instruction == 0x32)
            {
                LD_NNI_A();
                return;
            }*/
            if (instruction == 0x1A)
            {
                LD_A_DEI();
                return;
            }

            if (pc == 0xF532)
            {
                Debugger.Break();
            }

            if ((instruction & 0xC7) == 0x04)    //bug was here, masking with C4 instead of C7
            {    // 00 rrr 100       INC r
                INC_R(instruction);
                return;
            }
            if (instruction == 0x13)
            {
                INC_DE();
                return;
            }
            if (instruction == 0xD5)
            {
                PUSH_DE();
                return;
            }
            if (instruction == 0x38)
            {
                JR_C_E();
                return;
            }
            if (instruction == 0x19)
            {
                ADD_HL_DE();
                return;
            }
            if (instruction == 0x09)
            {
                ADD_HL_BC();
                return;
            }

            if (instruction == 0xD1)
            {
                POP_DE();
                return;
            }
            if (instruction == 0x23)
            {
                INC_HL();
                return;
            }
            /*if (instruction == 0x22)
            {
                LD_NNI_HL();
                return;
            }*/
            if (instruction == 0x20)
            {
                JR_NZ_E();
                return;
            }
            if ((instruction & 0xF8) == 0x80)
            { // 1000 0rrr   ADD a,r
                ADD_A_R(instruction);
                return;
            }
            /*
            if (instruction == 0)
            {
                //NOP
                return;
            }*/

            /*if (instruction == 0xC5)
            {
                PUSH_BC();
                return;
            }*/
            /*if (instruction == 0xC1)
            {
                POP_BC();
                return;
            }*/
            if (instruction == 0xEE)
            {
                XOR_N();
                return;
            }
            /*
            if (instruction == 0xD3)
            {
                //OUT (n),A  ignore
                OUT_N_A();
                pc++; //skip n
                return;
            }*/
            if (instruction == 0x18)
            {
                JR();
                return;
            }
            if (instruction == 0x1B)
            {
                DEC_DE();
                return;
            }
            if (instruction == 0x29)
            {
                ADD_HL_HL();
                return;
            }
            if (instruction == 0x1F)
            {
                RRA();
                return;
            }
            if (instruction == 0xF5)
            {
                PUSH_AF();
                return;
            }
            if (instruction == 0xF1)
            {
                POP_AF();
                return;
            }
            if (instruction == 0xc6)
            {
                ADD_A_n();
                return;
            }
            /*if ((instruction & 0b11000111) == 0b11000100)
            { // 11cc c100  CALL cc, pq
                CALL_CC_NN(instruction);
                return;
            }*/
            if (instruction == 0xF3)
            {
                DI();
                return;
            }
            /*if (instruction == 0x31)
            {
                LD_SP_NN();
                return;
            }*/
            if (instruction == 0x12)
            {
                LD_DEI_A();
                return;
            }
            if ((instruction & 0b11111000) == 0b10101000)
            { // 1010 1rrr  XOR r
                XOR_reg();
                return;
            }
            /*if ((instruction & 0b11111000) == 0b1011_0000)
            { // 1010 1rrr  OR r
                OR_reg();
                return;
            }*/
            if (instruction == 0x30)
            {
                JR_NC_rel();
                return;
            }
            if ((instruction & 0b11000111) == 0b11000010)
            {
                //11cc c010 JP cc, nn
                JP_cond_NN(instruction);
                return;
            }
            if ((instruction & 0b11000111) == 0b00000101)
            {
                DEC_reg(instruction);
                return;
            }
            if ((instruction & 0b11111000) == 0b10111000)
            {
                CP_reg(instruction);
                return;
            }
            if (instruction == 0x0B)
            {
                DEC_BC();
                return;
            }
            if (instruction == 0xEB)
            {
                EX_DE_HL();
                return;
            }
            if (instruction == 0x2B)
            {
                DEC_HL();
                return;
            }
            if ((instruction & 0b11111000) == 0b10010000)
            {
                SUB_reg(instruction);
                return;
            }
            if (instruction == 0xD6)
            {
                SUB_n();
                return;
            }

            if ((instruction & 0b11000111) == 0b11000000)
            {
                RET_cond(instruction);
                return;
            }


            //IY instructions
            if (instruction == 0xFD)
            {
                int iy_instruction = ram[pc];
                pc++;

                if (iy_instruction == 0x21)
                {
                    LD_IY_NN();
                    return;
                }
                if (iy_instruction == 0x2A)
                {
                    LD_IY_NNI();
                    return;
                }

                if ((iy_instruction & 0b11000111) == 0b01000110)
                {// 01rr r110  LD r, (IY+d)
                    LD_r_IYb(iy_instruction);
                    return;
                }

                if ((iy_instruction & 0b11111000) == 0b01110000)
                {
                    LD_IYb_r(iy_instruction);
                    return;
                }
                if (iy_instruction == 0x36)
                {
                    LD_IYb_n();
                    return;
                }

                if (iy_instruction == 0x7E)
                {
                    Debugger.Break();
                    //LD_A_IYd()
                }
                if (iy_instruction == 0x86)
                {
                    ADD_A_IYb();
                    return;
                }
                if (iy_instruction == 0xE5)
                {
                    PUSH_IY();
                    return;
                }
                if (iy_instruction == 0xE1)
                {
                    POP_IY();
                    return;
                }

                if (iy_instruction == 0x09)
                {
                    ADD_IY_BC();
                    return;
                }
                if (iy_instruction == 0x19)
                {
                    ADD_IY_DE();
                    return;
                }
                if (iy_instruction == 0x34)
                {
                    INC_IYd();
                    return;
                }
                if (iy_instruction == 0x35)
                {
                    DEC_IYd();
                    return;
                }
                if (iy_instruction == 0x23)
                {
                    INC_IY();
                    return;
                }
                if (iy_instruction == 0xBE)
                {
                    CP_IYb();
                    return;
                }
                if (iy_instruction == 0x8E)
                {
                    ADC_A_IYb();
                    return;
                }
                if (iy_instruction == 0xAE)
                {
                    XOR_IYb();
                    return;
                }
                if (iy_instruction == 0x96)
                {
                    SUB_IYb();
                    return;
                }


                if (iy_instruction == 0xCB)
                {
                    //here we have a 4 byte instruction where 4th byte is needed to decode it
                    int ins3 = ram[pc + 1];
                    if ((ins3 & 0b11000111) == 0b11000110)
                    {
                        //set b, (IY+b)
                        SET_x_IYb(ins3);
                        return;
                    }
                    if ((ins3 & 0b11000111) == 0b01000110)
                    {
                        BIT_x_IYb(ins3);
                        return;
                    }
                    if ((ins3 & 0b11000111) == 0b10000110)
                    {
                        RES_x_IYb(ins3);
                        return;
                    }
                    Debug.Assert(false, "unknown FDCB instruction");
                }



                Debug.Assert(false, "unknown FD instruction");
            }

            //111111111111111111111111111111111111111

            //IX instructions
            if (instruction == 0xDD)
            {
                int ix_instruction = ram[pc++];  //just for this obsolete list
                m_extOpcode = (byte)(ix_instruction);  //all new inst versions will use this

                /*if ((ix_instruction & 0b11000111) == 0b01000110)
                {// 01rr r110  LD r, (IX+d)
                    LD_r_IXb();
                    return;
                }*/
                if (ix_instruction == 0x21)
                {
                    LD_IX_NN();
                    return;
                }
                if (ix_instruction == 0x36)
                {
                    LD_IXb_n();
                    return;
                }
                if ((ix_instruction & 0b11111000) == 0b01110000)
                {
                    LD_IXb_r(ix_instruction);
                    return;
                }
                if (ix_instruction == 0x34)
                {
                    INC_IXd();
                    return;
                }

                if (ix_instruction == 0x35)
                {
                    DEC_IXd();
                    return;
                }
                if (ix_instruction == 0x09)
                {
                    ADD_IX_BC();
                    return;
                }
                if (ix_instruction == 0x19)
                {
                    ADD_IX_DE();
                    return;
                }
                if (ix_instruction == 0x23)
                {
                    INC_IX();
                    return;
                }
                if (ix_instruction == 0xE5)
                {
                    PUSH_IX();
                    return;
                }
                if (ix_instruction == 0xE1)
                {
                    POP_IX();
                    return;
                }
                if (ix_instruction == 0xB6)
                {
                    OR_IXd();
                    return;
                }


                /* all from IY branch, probably going to need them all

                if (iy_instruction == 0x2A)
                {
                    LD_IY_NNI();
                    return;
                }


                if (iy_instruction == 0x7E)
                {
                    Debugger.Break();
                    //LD_A_IYd()
                }
                if (iy_instruction == 0x86)
                {
                    ADD_A_IYb();
                    return;
                }

                if (iy_instruction == 0x34)
                {
                    INC_IYd();
                    return;
                }
                if (iy_instruction == 0xBE)
                {
                    CP_IYb();
                    return;
                }
                if (iy_instruction == 0xCB)
                {
                    //here we have a 4 byte instruction where 4th byte is needed to decode it
                    int ins3 = ram[pc + 1];
                    if ((ins3 & 0b11000111) == 0b11000110)
                    {
                        //set b, (IY+b)
                        SET_x_IYb(ins3);
                        return;
                    }
                    if ((ins3 & 0b11000111) == 0b01000110)
                    {
                        BIT_x_IYb(ins3);
                        return;
                    }
                    if ((ins3 & 0b11000111) == 0b10000110)
                    {
                        RES_x_IYb(ins3);
                        return;
                    }
                    Debug.Assert(false, "unknown FDCB instruction");
                }
                */


                Debug.Assert(false, "unknown DD instruction");
            }

            //22222222222222222222222222222222222222222222222222222222222222


            if (instruction == 0x0A)
            {
                LD_A_BCI();
                return;
            }
            if (instruction == 0x03)
            {
                INC_BC();
                return;
            }
            if (instruction == 0xF6)
            {
                OR_N();
                return;
            }
            if (instruction == 0x17)
            {
                RLA();
                return;
            }
            if (instruction == 0x0F)
            {
                RRCA();
                return;
            }
            if (instruction == 0x07)
            {
                RLCA();
                return;
            }
            if (instruction == 0x37)
            {
                SCF();
                return;
            }
            if (instruction == 0x3F)
            {
                CCF();
                return;
            }
            if (instruction == 0xE9)
            {
                JP_HLI();
                return;
            }
            if (instruction == 0x02)
            {
                LD_BCI_A();
                return;
            }

            /*
            if (instruction == 0xDB)
            {
                IN_A_N();
                return;
            }*/


            //int pc2 = pc - 1;
            string hexadrp = String.Format("0x{0:X4}", p); //p is a saved copy of pc
            string dis = $"Unknown instruction ${instruction:X2} (dec {instruction})\r\n" +
                DebugUnknownInstruction(instruction, p);

            //Debug.Assert(false, "unknown instr");
            stop_error = true;
            stop_error_string = dis;
        }
        public string stop_error_string = "";

        internal void DebugAttribs()
        {

            int bgc = 0;

            //just cycle trough all background colors
            int i = 0;
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int atribadr = 16384 + 192 / 8 * 256 + i;
                    byte atrib = ram[atribadr];
                    atrib = (byte)(atrib & 0xC7);
                    byte n = (byte)(bgc & 7);
                    n = (byte)(n << 3);
                    atrib = (byte)(atrib | n);
                    ram[atribadr] = atrib;

                    bgc++;


                    i++;
                }
                bgc++;
            }

            /*
            for ( int i=0; i< 32 * 24; i++)
            {   

                int atribadr = 16384 + 192 / 8 * 256 + i;
                byte atrib = ram[atribadr];
                atrib = (byte)(atrib & 0xC7);
                byte n = (byte)(bgc & 7) ;
                n = (byte)(n << 3);
                atrib = (byte)(atrib | n);
                ram[atribadr] = atrib;

                bgc++;
            }*/
        }

        internal void RequestScreenUpdate()
        {
            Debugger.Break(); //see if this is called
            //m_vizmem.RequestScreenUpdate();
        }

        private Func<int>[] reg_readers;
        private Action<byte>[] reg_writers;

        private Func<int>[] longreg_readers;
        private Action<int>[] longreg_writers;

        private void Initialize_Reg_ReadersWriters()
        {
            //xx - reg
            // b 000
            // c 001
            // d 010
            // e 011
            // h 100
            // l 101
            //   110?  (hl)
            // a 111

            reg_readers = new Func<int>[8];
            reg_readers[0] = () => b;
            reg_readers[1] = () => c;
            reg_readers[2] = () => d;
            reg_readers[3] = () => e;
            reg_readers[4] = () => h;
            reg_readers[5] = () => l;
            reg_readers[7] = () => a;
            reg_readers[6] = () => ram[h * 256 + l];

            reg_writers = new Action<byte>[8];
            reg_writers[0] = x => b = (byte)x;
            reg_writers[1] = x => c = (byte)x;
            reg_writers[2] = x => d = (byte)x;
            reg_writers[3] = x => e = (byte)x;
            reg_writers[4] = x => h = (byte)x;
            reg_writers[5] = x => l = (byte)x;
            reg_writers[7] = x => a = (byte)x;
            reg_writers[6] = x =>
            {
                ram[h * 256 + l] = (byte)x;
                DebugMemoryWrite(h * 256 + l, 1, pc - 1);
            };

            longreg_readers = new Func<int>[4];
            longreg_readers[0] = () => b * 256 + c;
            longreg_readers[1] = () => d * 256 + e;
            longreg_readers[2] = () => h * 256 + l;
            longreg_readers[3] = () => sp;

            longreg_writers = new Action<int>[4];
            longreg_writers[0] = x => { b = (byte)(x / 256); c = (byte)(x % 256); };
            longreg_writers[1] = x => { d = (byte)(x / 256); e = (byte)(x % 256); };
            longreg_writers[2] = x => { h = (byte)(x / 256); l = (byte)(x % 256); };
            longreg_writers[3] = x => { sp = (ushort)x; };

        }


        Action[] m_opcodes;
        LogStateMaker[] m_loggingOpcodes; //set type after usage

        Action[] m_ED_opcodes;
        LogStateMaker[] m_ED_loggingOpcodes; //set type after usage

        Action[] m_DD_opcodes;
        LogStateMaker[] m_DD_loggingOpcodes; //set type after usage


        //xx public delegate Func<ExecLogState, ExecLogState> LogStateMaker(byte op, ExecLogState s);

        private void InitializeOpcodeArray()
        {
            //helpers
            //fetch operand predicate
            //could be replaced by a record? class?

            Func<object[], Func<byte, bool>> opcode = def => (Func<byte, bool>)def[0];  //fetch opcode 
            Func<object[], Action> instruction = def => (Action)def[1];            //fetch instr
            Func<object[], LogStateMaker> loggerinst = def => (LogStateMaker)def[2];            //fetch instruction logger
            //Func<object[], Func<ExecLogState, byte, Func<ExecLogState, ExecLogState>>> loggerinst       = def => 
            //    (Func<byte, ExecLogState, Func<ExecLogState,ExecLogState>>)def[2];            //fetch instruction logger

            //helpers not used anymore

            //fill the opcode decoder table
            //fill the opcode logger table
            m_opcodes = new Action[256];
            m_loggingOpcodes = new LogStateMaker[256];
            InitializeOpcodeArray_Loop(GenerateOpcodeList(), m_opcodes, m_loggingOpcodes);

           
            m_ED_opcodes = new Action[256];
            m_ED_loggingOpcodes = new LogStateMaker[256];
            InitializeOpcodeArray_Loop(Generate_ED_OpcodeList(), m_ED_opcodes, m_ED_loggingOpcodes);

            m_DD_opcodes = new Action[256];
            m_DD_loggingOpcodes = new LogStateMaker[256];
            InitializeOpcodeArray_Loop(Generate_DD_OpcodeList(), m_DD_opcodes, m_DD_loggingOpcodes);


        }


        private void InitializeOpcodeArray_Loop(IEnumerable<InstructionDef> generator, 
                                                Action[] destOpcodes, 
                                                LogStateMaker[] destLoggers )
        {
            foreach (var def in generator)
            {
                //detect opcodes that this instruction understands
                for (int i = 0; i< 256; i++)
                {
                    if (def.OpcodeDecoder((byte) i))
                    {
                        Debug.Assert(destOpcodes[i] == null, "don't overwrite opcodes");

                        destOpcodes[i] = def.Instruction;
                        destLoggers[i] = def.Logger;

                        //debug
                        //string name = def.Instruction.Method.Name;  DB
                        string name = def.Logger.Method.Name;
                        string nameInst = def.Instruction.Method.Name;
                        string nDec = def.OpcodeDecoder.Method.Name;
                        Debug.WriteLine($"op: {i:X2} logr: {name}       ins:{nameInst}   dec:{nDec}");
                        //def.Logger.Target.GetType().Name      Spectrum
                    }
                }
            }
        }





        //logger helpers
        private Func<ExecLogState, ExecLogState> modifyF()
        {
            byte capf = f;
            return state =>
            {
                state.registersC.f = capf;
                return state;
            };
        }
        private Func<ExecLogState, ExecLogState> modifyA()
        {
            byte capa = a;
            return state =>
            {
                state.registersC.a = capa;
                return state;
            };
        }

        #region opcodes

        private List<InstructionDef> GenerateOpcodeList()  //instruction def object
        {
            //list of object[3], 


            //array of 3 objects ->
            //                      op code (or more than one?)
            //                      actual instruction
            //                      memory opeorations logger

            //logger:
            //need something that can read the parameterized register, but also 
            //a specific one
            //like r.a  or r.fromindex(3)  (number coming from the instruction bits)
            //so like regreader and regWriter

            //writesTo(?);  //modify reg, depending on opcode.  
            //  - reg X used (but not if A)
            //  - reg A overwritten
            //touches (reads?) mem at pc(at the start on the instruction) with len of the inst.
            //executes mem at pc

            //so, this object needs to:
            //-change state of the memory and registers
            //-let us know what registers where changed
            //-also save what as the previous value???

            //optins: class with fields (with optional state?)
            //        delegate with state?


            //attempt 1:  just create lamda that does the needed state changes
            //            and returns the descriptions of what is changed
            //            (descriptions could be bitpacked?)
            //
            //
            //
            // LD H, (HL) problem, hl memory read operation can only be logged before the instr. execution
            //            since the adress gets deleted in the reading



            //List<object[]> instructions = new();
            List<InstructionDef> instructions = new();



            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => op == 0x25,
                Instruction = I_DecH,
                Logger = () =>
                {

                    //but wont this just capture the processor h?
                    //we need it to work or the Log Register h
                    //h = (byte)(h - 1);

                    //readsFrom(h); //touch reg h
                    //writesTo(h);  //modify reg h
                    //return state;

                    byte newH = h;

                    return state =>
                    {
                        //st.a = capRezValue;
                        state.registersC.h = newH;
                        return state;
                    };
                }

            });

            instructions.Add(new InstructionDef
            {
                // 1010 1rrr  XOR r
                OpcodeDecoder = op => (op & 0b11111000) == 0b10101000,
                Instruction = XOR_reg,
                Logger = () =>
                {

                    //instructions can works with the opcode being available in the state
                    //loggingInstructions should capure the opcode. do they?

                    int r = m_opcode & 7; //capturing the r?
                    byte capf = f;

                    //a = (byte)(a ^ reg_readers[r]());

                    if (r == 7) // A - 111
                    {

                        return st =>
                        {
                            //we capture nothing
                            st.registersC.a = 0;
                            st.registersC.f = capf;
                            return st;
                        };
                    }
                    byte capRezValue = a; //just capture the result

                    return st =>
                    {
                        //Debugger.Break(); //implement a yo writting a result
                        //Debug.Assert(r != 7, "a should not be used here");

                        st.registersC.a = capRezValue;
                        st.registersC.f = capf;
                        return st;
                    };

                    //reading: reg r, or even memory?
                    //if r is 110, then H, L, and (HL) memory is read
                    //if r is 111, (XOR A) then nothing is read, 0 is written is A


                }
            });

            instructions.Add(new InstructionDef
            {
                // 1010 0rrr  AND r
                OpcodeDecoder = op => (op & 0b1111_1000) == 0b1010_0000,
                Instruction = AND_reg,
                Logger = () =>
                {

                    //instructions can works with the opcode being available in the state
                    //loggingInstructions should capure the opcode. do they?

                    //full code
                    //int r = m_opcode & 7;
                    //int val = reg_readers[r]();

                    //a = (byte)(a & val);

                    byte capa = a;
                    byte capf = f;
                    //int r = m_opcode & 7; //capturing the r? needed if we log the reg usage as well

                    /*
                    if (r == 7) // A - 111
                    {
                        return st =>
                        {
                            //we capture nothing
                            st.registersC.a = 0;
                            st.registersC.f = capf;
                            return st;
                        };
                    }*/
                    return st =>
                    {
                        st.registersC.a = capa;
                        st.registersC.f = capf;
                        return st;
                    };
                }
            });




            instructions.Add(new InstructionDef
            {
                // 1011 0rrr  OR r
                OpcodeDecoder = op => (op & 0b1111_1000) == 0b1011_0000,
                Instruction = OR_reg,
                Logger = () =>
                {
                    /*
                    int r = instruction & 7;
                    int val = reg_readers[r]();

                    a = (byte)(a | val);
                    flag_adj_s(a);
                    flag_adj_z(a);
                    flag_adj_c(false);*/

                    int r = m_opcode & 7; //capturing the r
                    byte capf = f;

                    if (r == 7) // A - 111
                    {
                        return st =>
                        {
                            //we capture nothing
                            //st.registersC.a = 0;
                            st.registersC.f = capf;
                            return st;
                        };
                    }
                    byte capRezValue = a; //just capture the result

                    return st =>
                    {
                        st.registersC.a = capRezValue;
                        st.registersC.f = capf;
                        return st;
                    };

                    //reading: reg r, or even memory?
                    //if r is 110, then H, L, and (HL) memory is read
                    //if r is 111, (XOR A) then nothing is read, 0 is written is A

                }
            });


            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xDB == op,
                Instruction = IN_A_N,
                Logger = () =>
                {
                    byte capa = a;
                    return state =>
                    {
                        state.registersC.a = capa;
                        return state;
                    };
                }
            });

            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xE6 == op,
                Instruction = AND_N,
                Logger = () =>
                {
                    byte capa = a;
                    byte capf = f;
                    return state =>
                    {
                        state.registersC.a = capa;
                        state.registersC.f = capf;
                        return state;
                    };
                }
            });

            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xFE == op,
                Instruction = CP_N,
                Logger = () =>
                {
                    /*
                    byte capf = f;
                    return state => {
                        state.registersC.f = capf;
                        return state;
                    };*/
                    return modifyF();
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x28 == op,
                Instruction = JR_Z_E,
                Logger = () =>
                {
                    /*
                    sbyte e = (sbyte)ram[pc]; pc++;

                    bool z = Z();
                    if (z)
                    {
                        pc = (ushort)(pc + e);
                    }*/
                    ushort cappc = pc;
                    return state =>
                    {
                        state.registersC.pc = cappc;
                        return state;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x32 == op,
                Instruction = LD_NNI_A,
                Logger = () =>
                {
                    /*      int adr = ram[pc] + 256 * ram[pc + 1];
                            pc += 2;
                            ram[adr] = a;
                    */
                    //when the instruction was running because it could have been overwritten by the instruction
                    //in that case, i need an another logger that would run before the instruction itself (a prelogger?)
                    int capadr = ram[pc-2] + 256 * ram[pc - 1]; //if i take the adr now, it could be different
                    byte capa = a;
                    return state =>
                    {
                        state.ramC[capadr] = capa;
                        //Debug.Assert( ) could check if the adress was withing those two bytes
                        return state;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x31 == op,
                Instruction = LD_SP_NN,
                Logger = () =>
                {
                    /*      
                     *     sp = (ushort)(ram[pc + 1] * 256 + ram[pc]);
                            pc += 2;
                     *      
                    */
                    ushort capsp = (ushort)(ram[pc - 2] + 256 * ram[pc - 1]);
                    return state =>
                    {
                        state.registersC.sp = capsp;
                        return state;
                    };
                }
            });

            /* LD_reg_n handles this case
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x3E == op,
                Instruction = LD_A_N,
                Logger = () =>
                {
                    //byte capa = a;
                    //return state => {
                    //    state.registersC.a = capa;
                    //    return state;
                    //};
                    return modifyA();
                }
            });*/

            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xCD == op,
                Instruction = CALL_NN,
                Logger = () =>
                {
                    ushort cappc = pc;
                    ushort capsp = sp;
                    byte capmem1 = ram[sp];
                    byte capmem2 = ram[sp + 1];
                    return state =>
                    {
                        state.registersC.pc = cappc;
                        state.registersC.sp = capsp;
                        state.ramC[capsp] = capmem1;
                        state.ramC[capsp + 1] = capmem2;
                        return state;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => (op & 0b11000111) == 0b11000100,
                Instruction = CALL_CC_NN,
                Logger = () =>
                {
                    //this is a strange one, if we want to distinguish between call and no-call history
                    //right now, we just blindly copy regs and top of the stack
                    ushort cappc = pc;
                    ushort capsp = sp;
                    byte capmem1 = ram[sp];
                    byte capmem2 = ram[sp + 1];
                    //perhaps we can tell from checking the top of the stack content?
                    return state =>
                    {
                        state.registersC.pc = cappc;
                        state.registersC.sp = capsp;
                        state.ramC[capsp] = capmem1;
                        state.ramC[capsp + 1] = capmem2;
                        return state;
                    };
                }
            });



            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xC9 == op,
                Instruction = RET,
                Logger = () =>
                {
                    /*
                    int adr = ram[sp] + (256 * ram[sp + 1]);
                    sp += 2;
                    pc = (ushort)adr;*/
                    ushort cappc = pc;
                    ushort capsp = sp;
                    return state =>
                    {
                        state.registersC.pc = cappc;
                        state.registersC.sp = capsp;
                        return state;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x3A == op,
                Instruction = LD_A_NNI,
                Logger = () =>
                {
                    return modifyA();
                }
            });

            instructions.Add(new InstructionDef
            {
                //01 rrr xxx
                OpcodeDecoder = op => (op & 0xC0) == 0x40,
                Instruction = LD_R_RPRIME,
                Logger = () =>
                {

                    /*
                    //baci 11
                    //handle all LD r, r' instructions
                    int rprime = instruction & 7;
                    int r = (instruction / 8) & 7;

                    int val = reg_readers[rprime]();
                    reg_writers[r]((byte)val);
                    //baci22*/

                    int rprime = m_opcode & 7; //enough to just read the read reg
                    byte capval = (byte)reg_readers[rprime](); //using zx reg readers
                    byte capr = (byte)((m_opcode / 8) & 7);

                    return st =>
                    {
                        //using logstate regwriters
                        ExecLogState.st_reg_writers[capr](capval, st.ramC, st.registersC);
                        return st;
                    };



                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x00 == op,
                Instruction = () => { }, //NOP
                Logger = () =>
                {
                    return st => st;
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xD3 == op,
                Instruction = OUT_N_A, 
                Logger = () =>
                {
                    return st => st;
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x21 == op,
                Instruction = LD_HL_NN,
                Logger = () =>
                {
                    /*
                    h = ram[pc + 1];
                    l = ram[pc];
                    pc += 2;*/
                    byte caph = h;
                    byte capl = l;
                    return state => {
                        state.registersC.h = caph;
                        state.registersC.l = capl;
                        return state;
                    };
                }
            });

            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x11 == op,
                Instruction = LD_DE_NN,
                Logger = () =>
                {
                    byte capd = d;
                    byte cape = e;
                    return state => {
                        state.registersC.d = capd;
                        state.registersC.e = cape;
                        return state;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x01 == op,
                Instruction = LD_BC_NN,
                Logger = () =>
                {
                    byte capb = b;
                    byte capc = c;
                    return state => {
                        state.registersC.b = capb;
                        state.registersC.c = capc;
                        return state;
                    };
                }
            });

            //huopsky powod
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => (op & 0xC7) == 0x06,
                Instruction = LD_reg_n,
                Logger = () =>
                {
                    /* inst            
                        int r = (instruction / 8) & 7;
                        byte val = ram[pc]; pc++;
                        reg_writers[r](val);
                    */

                    //special case for logging LD (HL), N
                    //is it really needed?
                    int capr = (m_opcode / 8) & 7;
                    byte capval = ram[pc - 1];

                    if( capr == 6) //(HL)
                    {
                        ushort caphl = (ushort)(h * 256 + l);
                        return state =>
                        {
                            state.ramC[caphl] = capval;
                            return state;
                        };
                    }
                    //normal regs
                    return st =>
                    {
                        ExecLogState.st_reg_writers[capr](capval, st.ramC, st.registersC);
                        return st;
                    };
                }
            });


            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0x22 == op,
                Instruction = LD_NNI_HL,
                Logger = () =>
                {
                    // inst            
                    //    int adr = ram[pc] + 256 * ram[pc + 1];
                    //    pc += 2;
                    //    ram[adr] = l;
                    //    ram[adr + 1] = h;

                    ushort capadr = (ushort)(ram[pc - 2] + 256 * ram[pc - 1]);
                    byte caph = h;
                    byte capl = l;
                    
                    return st =>
                    {
                        st.ramC[capadr] = capl;
                        st.ramC[capadr+1] = caph;
                        return st;
                    };
                }
            });

            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xC5 == op,
                Instruction = PUSH_BC,
                Logger = () =>
                {
                    byte capb = b;
                    byte capc = c;
                    ushort capsp = sp;
                    return st =>
                    {
                        st.registersC.sp = capsp;
                        st.ramC[capsp] = capc;
                        st.ramC[capsp + 1] = capb;
                        return st;
                    };
                }
            });
            instructions.Add(new InstructionDef
            {
                OpcodeDecoder = op => 0xC1 == op,
                Instruction = POP_BC,
                Logger = () =>
                {
                    byte capb = b;
                    byte capc = c;
                    ushort capsp = sp;
                    return st =>
                    {
                        st.registersC.sp = capsp;
                        st.registersC.c = st.ramC[capsp - 2];
                        st.registersC.b = st.ramC[capsp - 1];
                        return st;
                    };
                }
            });







            //optable


            /* before, instruction defined an object[3] array, then cast when reading it
             * switching to a record class
            11              
            instructions.Add(new object[]{
                (Func<byte,bool>) ( op => op == 0x25),
                (Action) ( () =>  {
                    I_DecH();
                }),
                (LogStateMaker) (  (opc, state) => {
                    Debugger.Break();

                    //but wont this just capture the processor h?
                    //we need it to work or the Log Register h
                    h = (byte)(h - 1);

                    //readsFrom(h); //touch reg h
                    //writesTo(h);  //modify reg h
                    //return state;
                    return state => {
                        Debugger.Break(); //implement a
                        //st.a = capRezValue;
                        return state;
                    };



                })

            });

            instructions.Add(new object[] {

                 // 1010 1rrr  XOR r
                (Func<byte, bool>)(op => (op & 0b11111000) == 0b10101000),
                (Action)(() => XOR_reg() ),
                //(Func<byte, ExecLogState, Func<ExecLogState, ExecLogState>>)((opc, state) => {
                (LogStateMaker)((opc, state) => {
                    Debugger.Break();

                    //instructions can works with the opcode being available in the state
                    //loggingInstructions should capute the opcode

                    //
                    int r = opc & 7; //capturing the r?
                    
                    //a = (byte)(a ^ reg_readers[r]());
                    //byte capRezValue = (byte) ( reg_readers[r]() );

                    Debug.Assert(false, "add regreaderrs to state");
                    //byte capRezValue = (byte) ( state.reg_readers[r]() );
                    byte capRezValue = 0; //dummy
                    

                    return st => {
                        Debugger.Break(); //implement a
                        //st.a = capRezValue;
                        return st;
                    };

                    //reading: reg r, or even memory?
                    //if r is 110, then H, L, and (HL) memory is read
                    //if r is 111, (XOR A) then nothing is read, 0 is written is A


                })
            });
              
            
            22
            */

            return instructions;
        }//end GenerateInstructionsList 


        #endregion
        #region opcodesED

        private List<InstructionDef> Generate_ED_OpcodeList()  //instruction def object
        {
            List<InstructionDef> instructions = new()
            {
                new InstructionDef
                {
                    OpcodeDecoder = op => op == 0xB0,
                    //Instruction = () => LDIR(),
                    Instruction =  LDIR,
                    Logger = () =>
                    {
                        /* full inst        
                        int hl = h * 256 + l;
                        int bc = b * 256 + c;
                        int de = d * 256 + e;

                        ram[de] = ram[hl];
                        de++; hl++;
                        bc--;
                        if( bc > 0) { pc--; pc--; }

                        d = (byte)(de / 256); e = (byte)(de % 256);
                        b = (byte)(bc / 256); c = (byte)(bc % 256);
                        h = (byte)(hl / 256); l = (byte)(hl % 256);*/

                        //as usual, we are logging after the instruction
                        byte capData = ram[ h * 256 + l - 1];
                        byte capd = d;
                        byte cape = e;
                        byte capb = b;
                        byte capc = c;
                        byte caph = h;
                        byte capl = l;
                        return state => {
                            state.ramC[ capd * 256 + cape - 1] = capData;
                            state.registersC.d = capd;
                            state.registersC.e = cape;
                            state.registersC.b = capb;
                            state.registersC.c = capc;
                            state.registersC.h = caph;
                            state.registersC.l = capl;
                            return state;
                        };
                    }
                }
            };

            return instructions;
        } //end GenerateEDOpcodeList

        #endregion
        #region opcodesDD

        private List<InstructionDef> Generate_DD_OpcodeList()  //IX instructions
        {
            List<InstructionDef> instructions = new()
            {
                new InstructionDef
                {
                    OpcodeDecoder = op => op == 0x21,
                    Instruction =  LD_IX_NN,
                    Logger = () =>
                    {
                        ushort capix = ix;
                        return st =>
                        {
                            st.registersC.ix = capix;
                            return st;
                        };
                    }
                },
                new InstructionDef
                {
                    OpcodeDecoder = op => (op & 0b1100_0111) == 0b0100_0110,
                    Instruction =  LD_r_IXb,
                    Logger = () =>
                    {
                        sbyte b = (sbyte)ram[pc-1];
                        int capr = (m_extOpcode / 8) & 7;
                        int capadr = ix + b;
                        byte capval = ram[capadr];
                        return st =>
                        {
                            ExecLogState.st_reg_writers[capr](capval, st.ramC, st.registersC);
                            return st;
                        };
                    }
                },


            };



            return instructions;
        } //end Generate_DD_OpcodeList
        #endregion
        #region Instructions
        private void I_DecH()
        {

            //r 00 XXX 101
            //xx - reg
            // b 000
            // c 001
            // d 010
            // e 011
            // h 100
            // l 101
            //   110?  (hl)
            // a 111

            h = (byte)(h - 1);
            flag_adj_z(h);
        }

        private void INC_R(int instruction)
        {
            int r = (instruction / 8) & 7;
            int val = reg_readers[r]();
            val++;
            reg_writers[r]((byte)val);
            flag_adj_z((byte)val);
            //inc r should not have any effect on C flag
        }

        private void DEC_C()
        {
            c = (byte)(c - 1);
            flag_adj_z(c);
        }
        private void DEC_reg(int instruction)
        {
            int r = (instruction / 8) & 7;
            int val = reg_readers[r]();

            byte newval = (byte)(val - 1);

            reg_writers[r](newval);

            flag_adj_s(newval);
            flag_adj_z(newval);

        }



        private void DEC_A()
        {
            a = (byte)(a - 1);
            flag_adj_z(a);
            flag_adj_s(a);
        }
        private void INC_DE()
        {
            int de = d * 256 + e;
            de++;
            d = (byte)(de / 256);
            e = (byte)(de % 256);
        }
        private void INC_BC()
        {
            int x = bc();
            x++;
            b = (byte)(x / 256);
            c = (byte)(x % 256);
        }

        private void DEC_DE()
        {
            int de = d * 256 + e;
            de--;
            ushort ude = (ushort)de;
            //d = (byte)(de / 256);
            //e = (byte)(de % 256);
            d = (byte)(ude / 256);
            e = (byte)(ude % 256);
        }
        private void INC_HL()
        {
            int hl = h * 256 + l;
            hl++;
            h = (byte)(hl / 256);
            l = (byte)(hl % 256);
        }
        private void INC_IX()
        {
            ix = (ushort)(ix + 1);
        }
        private void INC_IY()
        {
            iy = (ushort)(iy + 1);
        }
        private void DEC_HL()
        {
            int hl = h * 256 + l;
            hl--;
            ushort uhl = (ushort)hl;
            h = (byte)(uhl / 256);
            l = (byte)(uhl % 256);
        }

        private void DEC_BC()
        {
            int bc = b * 256 + c;
            bc--;
            ushort ubc = (ushort)bc; ;
            b = (byte)(ubc / 256);
            c = (byte)(ubc % 256);
        }


        private void INC_HLI()
        {
            byte n = ram[256 * h + l];
            n = (byte)(n + 1);
            ram[256 * h + l] = n;
            flag_adj_z(n);
        }
        private void ADD_A_R(int instruction)
        {
            int r = instruction & 7;
            int val = reg_readers[r]();
            int rez = a + val;

            a = (byte)(rez);
            flag_adj_z((byte)rez);
            flag_adj_c(rez > 255);
            flag_adj_s(a);
        }

        private void SUB_reg(int instruction)
        {
            int r = instruction & 7;
            int val = reg_readers[r]();
            int rez = a - val;

            a = (byte)(rez);

            if (rez < 0)
            {
                //Debugger.Break();
                DebugLogSubBugs(pc - 1);
                //crate a list of locations with this bug
            }

            flag_adj_z((byte)rez);
            //flag_adj_c(rez > 255);   //sub function carry will be on if rez is less than 0
            flag_adj_c(rez < 0);
            flag_adj_s((byte)rez);
        }

        private void SUB_IYb()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            byte val = ram[adr];
            int rez = a - val;
            a = (byte)(rez);
            if (rez < 0)
            {
                //Debugger.Break();
                DebugLogSubBugs(pc - 1);
                //crate a list of locations with this bug
            }
            flag_adj_z((byte)rez);
            flag_adj_c(rez < 0);
            flag_adj_s((byte)rez);
        }


        private void SUB_n()
        {
            int val = ram[pc++];
            int rez = a - val;
            a = (byte)(rez);

            //if (rez < 0) Debugger.Break();
            if (rez < 0)
            {
                DebugLogSubBugs(pc - 2);
            }

            flag_adj_z((byte)rez);
            //flag_adj_c(rez > 255); //bug
            flag_adj_c(rez < 0);
            flag_adj_s((byte)rez);
        }



        private void ADD_A_n()
        {
            int n = ram[pc++];
            int rez = a + n;

            a = (byte)rez;
            flag_adj_z((byte)rez);
            flag_adj_c(rez > 255);
            flag_adj_s((byte)rez);
        }
        private void ADD_HL_DE()
        {
            int de = d * 256 + e;
            int hl = h * 256 + l;

            int rez = hl + de;
            h = (byte)(rez / 256);
            l = (byte)(rez % 256);
            flag_adj_c(rez > 65535);
        }
        private void ADD_HL_BC()
        {
            int bc = b * 256 + c;
            int hl = h * 256 + l;

            int rez = hl + bc;
            h = (byte)(rez / 256);
            l = (byte)(rez % 256);
            flag_adj_c(rez > 65535);
        }

        private void ADD_HL_HL()
        {
            int hl = h * 256 + l;

            int rez = hl + hl;
            h = (byte)(rez / 256);
            l = (byte)(rez % 256);
            flag_adj_c(rez > 0xFFFF);
        }

        private void SBC_HL_ss(int ed_instruction)
        {
            int ss = (ed_instruction / 16) & 3;
            int op = 0;
            switch (ss)
            {
                case 0: //bc
                    op = b * 256 + c;
                    break;
                case 1: //de
                    op = d * 256 + e;
                    break;
                case 2: //hl
                    op = h * 256 + l;
                    break;
                case 3: //sp
                    op = sp;
                    break;
            }
            int rez = (h * 256 + l) - op;
            if (C()) { rez = rez - 1; }

            /* bug lies here, negative numbers / 256 produce weirdness
            h = (byte)(rez / 256);
            l = (byte)(rez % 256);*/

            //if (rez < 0) Debugger.Break();

            ushort rez2 = (ushort)rez;
            h = (byte)(rez2 / 256);
            l = (byte)(rez2 % 256);

            flag_adj_c(rez < 0);
            flag_adj_z(rez);
            flag_adj_s(rez2); //might be good might be not good
        }



        private void AND_N()
        {
            byte n = ram[pc];
            pc += 1;
            a = (byte)(a & n);
            flag_adj_z(a);
            flag_adj_c(false);
        }

        private void OR_N()
        {
            byte n = ram[pc];
            pc += 1;

            a = (byte)(a | n);
            flag_adj_z(a);
            flag_adj_c(false);
            flag_adj_s(a);
        }


        private void AND_reg()
        {
            int r = m_opcode & 7;
            int val = reg_readers[r]();

            a = (byte)(a & val);
            flag_adj_z(a);
            flag_adj_c(false);
            flag_adj_s(a);
        }

        private void XOR_reg()
        {
            int r = m_opcode & 7;
            int val = reg_readers[r]();

            a = (byte)(a ^ val);
            flag_adj_s(a);
            flag_adj_z(a);
            flag_adj_c(false);
        }
        private void OR_reg()
        {
            int r = m_opcode & 7;
            int val = reg_readers[r]();

            a = (byte)(a | val);
            flag_adj_s(a);
            flag_adj_z(a);
            flag_adj_c(false);
        }

        private void OR_IXd()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = ix + b;
            a = (byte)(a | ram[adr]);
            flag_adj_s(a);
            flag_adj_z(a);
            flag_adj_c(false);
        }

        private void XOR_N()
        {
            byte n = ram[pc];
            pc += 1;
            a = (byte)(a ^ n);
            flag_adj_s(a);
            flag_adj_z(a);
            flag_adj_c(false);
        }
        private void XOR_IYb()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            a = (byte)(a ^ ram[adr]);
            flag_adj_s(a);
            flag_adj_z(a);
            flag_adj_c(false);
        }



        private void cp(byte n)
        {
            flag_adj_z((byte)(a - n));
            flag_adj_s((byte)(a - n));
            if (n > a)
            {
                //set carry
                f = (byte)(f | FLAG_C);
            }
            else
            {
                //carry off
                f = (byte)(f & (0xFF ^ FLAG_C));
            }

        }
        private void CP_N()
        {
            byte n = ram[pc]; pc++;
            //should have effect on C and Z similar to subtraction
            //flag_adj_z ( (byte) (a - n));
            cp(n);
        }

        private void CP_reg(int instruction)
        {
            int r = instruction & 7;
            int val = reg_readers[r]();
            cp((byte)val);
        }


        private void CP_HLI()
        {
            byte n = ram[256 * h + l];
            //flag_adj_z((byte)(a - n));
            cp(n);
        }
        private void OUT_N_A()
        {
            //ignored
            pc++;
        }



        private void flag_adj_z(byte x)
        {
            if (x == 0)
            {
                f = (byte)(f | FLAG_Z);             //turn on flag z
            }
            else
            {
                f = (byte)(f & (0xFF ^ FLAG_Z));   //turn off flag z
            }
        }
        private void flag_adj_z(int x) //used as 16 bit version of zero check
        {
            if (x == 0)
            {
                f = (byte)(f | FLAG_Z);             //turn on flag z
            }
            else
            {
                f = (byte)(f & (0xFF ^ FLAG_Z));   //turn off flag z
            }
        }


        private void flag_adj_s(byte x) //8 bit version
        {
            if ((x & 128) != 0)
            {
                f = (byte)(f | FLAG_S);             //turn on 
            }
            else
            {
                f = (byte)(f & (0xFF ^ FLAG_S));   //turn off 
            }
        }

        private void flag_adj_s(ushort x) //16 bit version
        {
            if ((x & 0x8000) != 0)
            {
                f = (byte)(f | FLAG_S);             //turn on 
            }
            else
            {
                f = (byte)(f & (0xFF ^ FLAG_S));   //turn off 
            }
        }


        private void flag_adj_c(bool set)
        {
            if (set)
            {
                f = (byte)(f | FLAG_C);             //turn on flag c
            }
            else
            {
                f = (byte)(f & (0xFF ^ FLAG_C));   //turn off flag c
            }
        }

        private void LD_R_RPRIME()
        {
            //this 

            // 01 rrr rrr
            // 000 b
            // 001 c
            // 010 d
            // 011 e
            // 100 h
            // 101 l
            // 110   ??
            // 111 a

            /*
            //array of register readers
            var reg_readers = new Func<int>[8];
            reg_readers[0] = () => b;
            reg_readers[7] = () => a;

            var reg_writers = new Action<byte>[8];
            reg_writers[7] = x => a = (byte)x;
            reg_writers[6] = x => ram[ h*256 + l] = (byte)x;
            */

            //handle all LD r, r' instructions
            int rprime = m_opcode & 7;
            int r = (m_opcode / 8) & 7;

            int val = reg_readers[rprime]();
            reg_writers[r]((byte)val);
        }

        //move reg readers and writers out of this function 

        private void LD_A_E()
        {
            // 01 rrr rrr
            // 000 b
            // 001 c
            // 010 d
            // 011 e
            // 100 h
            // 101 l
            // 110   ??
            // 111 a
            a = e;
        }
        private void LD_A_HLI()
        {
            a = ram[256 * h + l];
        }
        private void LD_A_BCI()
        {
            a = ram[256 * b + c];
        }

        private void LD_A_DEI()
        {
            a = ram[256 * d + e];
        }

        private void LD_DEI_A()
        { //LD (DE),A
            int de = d * 256 + e;
            ram[de] = a;

            DebugMemoryWrite(de, 1, pc - 1);
        }
        private void LD_BCI_A()
        {
            ram[bc()] = a;
            DebugMemoryWrite(bc(), 1, pc - 1);
        }


        private void LD_A_NNI()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            a = ram[adr];
        }
        private void LD_NNI_A()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            ram[adr] = a;
        }
        private void LD_NNI_HL()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            ram[adr] = l;
            ram[adr + 1] = h;

            DebugMemoryWrite(adr, 2, pc - 3);
        }
        private void LD_NNI_DE()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            ram[adr] = e;
            ram[adr + 1] = d;

            DebugMemoryWrite(adr, 2, pc - 3);
        }
        private void LD_NNI_BC()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            ram[adr] = c;
            ram[adr + 1] = b;

            DebugMemoryWrite(adr, 2, pc - 3);
        }

        private void LD_NNI_SP()
        {
            int adr = ram[pc] + 256 * ram[pc + 1];
            pc += 2;
            ram[adr] = (byte)(sp % 256);
            ram[adr + 1] = (byte)(sp / 256);
        }


        private void LD_reg_n()
        {
            int r = (m_opcode / 8) & 7;
            byte val = ram[pc]; pc++;
            reg_writers[r](val);
        }
        private void LD_H_N()
        {
            //r 00 XXX 110
            //xx - reg
            // b 000
            // c 001
            // d 010
            // e 011
            // h 100
            // l 101
            //   110?  ??
            // a 111


            byte n = ram[pc];
            pc += 1;
            h = n;
        }
        
        /*private void LD_A_N()  //LD_reg_n already handles this case
        {
            byte n = ram[pc];
            pc += 1;
            a = n;
        }*/

        //hack, we need the same R value as the X processor, to be able to keep comparing states
        //so, either recreate complete R logic, or just copy from X
        public Z80Lib.Z80 hack_procX;
        //private Z80Lib.Context ctx;
        //private Z80Lib.Z80 procX;

        private static byte m_refresh_reg = 0;
        private void LD_A_R()  //LD A,R
        {
            a = m_refresh_reg++;

            if (hack_procX is not null)
            {
                m_refresh_reg = hack_procX.getContext().R;
                a = hack_procX.getContext().R;
            }

            //Debugger.Break(); //this must go to keyboard logger
            m_keyboard.Overwrite_R_RegisterValue(ref a, aam_instruction_counter);

        }


        private void LD_E_N()
        {
            byte n = ram[pc];
            pc += 1;
            e = n;

        }
        private void LD_B_N()
        {
            byte n = ram[pc];
            pc += 1;
            b = n;
        }


        private void LD_BC_NN()
        {
            b = ram[pc + 1];
            c = ram[pc];
            pc += 2;
        }
        private void LD_DE_NN()
        {
            d = ram[pc + 1];
            e = ram[pc];
            pc += 2;
        }
        private void LD_SP_NN()
        {
            sp = (ushort)(ram[pc + 1] * 256 + ram[pc]);
            pc += 2;
        }

        private void LD_HL_NN()
        {
            h = ram[pc + 1];
            l = ram[pc];
            pc += 2;
        }
        private void LD_HL_NNI()
        {
            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;
            l = ram[adr];
            h = ram[adr + 1];
        }
        private void LD_SP_NNI()   //indirect nn, or (NN)
        {
            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;
            int vv = ram[adr] + (256 * ram[adr + 1]);
            sp = (ushort)vv;

        }
        private void LD_RR_NNI(int edinstruction)
        {
            //LD BC, ($81A4)  ED + 01rr 1011
            int rr = (edinstruction / 16) & 3;

            int adr = ram[pc] + (256 * ram[pc + 1]);  //adr from instruction
            pc += 2;
            int vv = ram[adr] + (256 * ram[adr + 1]); //read from adr


            longreg_writers[rr](vv);
        }



        private void JP_NN()
        {
            // + 2bytes address
            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;
            pc = (ushort)adr;
        }
        private void JP_HLI()
        {
            pc = (ushort)hl();
        }

        private void JP_cond_NN(int instruction)
        {
            //11cc c010 JP cc, nn

            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;

            int cond = (instruction / 8) & 7;
            switch (cond)
            {
                case 0b110: //P - positive, plus (7th bit is zero)
                    if (!S())
                    {
                        pc = (ushort)adr;
                        return;
                    }
                    break;
                case 0b111: //M - minus (7th bit is set)
                    if (S())
                    {
                        pc = (ushort)adr;
                        return;
                    }
                    break;
                case 0b001: //Z
                    if (Z())
                    {
                        pc = (ushort)adr;
                        return;
                    }
                    break;
                case 0b011: //C
                    if (C())
                    {
                        pc = (ushort)adr;
                        return;
                    }
                    break;
                case 0b010: //NC
                    if (!C())
                    {
                        pc = (ushort)adr;
                        return;
                    }
                    break;
                default:
                    Debugger.Break();
                    //add other conditions, page 282
                    break;
            }

        }



        private void I_JpNZ_Adr()
        {
            // // 11 ccc 010
            // + 2bytes address
            bool nz = NZ();

            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;

            if (nz)
            {
                pc = (ushort)adr;
            }

        }
        private void JR()
        {
            //bug here, jump distance should be a signed byte
            //int e = ram[pc]; pc++;
            sbyte e = (sbyte)ram[pc];
            pc++;
            pc = (ushort)(pc + e);

        }
        private void JR_Z_E()
        {
            sbyte e = (sbyte)ram[pc]; pc++;

            bool z = Z();
            if (z)
            {
                pc = (ushort)(pc + e);
            }
        }
        private void JR_NZ_E()
        {
            sbyte e = (sbyte)ram[pc]; pc++;
            if (NZ())
            {
                pc = (ushort)(pc + e);
            }
        }
        private void JR_C_E()
        {
            sbyte e = (sbyte)ram[pc]; pc++;

            bool c = C();
            if (c)
            {
                pc = (ushort)(pc + e);
            }
        }
        private void JR_NC_rel()
        {
            sbyte e = (sbyte)ram[pc]; pc++;

            bool c = C();
            if (!c)
            {
                pc = (ushort)(pc + e);
            }

        }




        private void CALL_NN()
        {
            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;

            ram[sp - 1] = (byte)(pc / 256);
            ram[sp - 2] = (byte)(pc % 256);
            sp -= 2;

            // + 2bytes address
            pc = (ushort)adr;

        }
        private void call() //call whatever is the next two bytes in memory
        {
            int adr = ram[pc] + (256 * ram[pc + 1]);
            pc += 2;

            ram[sp - 1] = (byte)(pc / 256);
            ram[sp - 2] = (byte)(pc % 256);
            sp -= 2;

            // + 2bytes address
            pc = (ushort)adr;

        }

        private void CALL_CC_NN()
        {
            // 11cc c100  CALL cc, pq

            //call on condition
            int condition = (m_opcode / 8) & 7;
            switch (condition)
            {
                case 1:  //Z
                    if (Z())
                    {
                        call();
                        return;
                    }
                    else
                    {
                        pc += 2; //ignore next 2 bytes
                    }
                    break;
                case 0:  //NZ
                    if (!Z())
                    {
                        call();
                        return;
                    }
                    else
                    {
                        pc += 2; //ignore next 2 bytes
                    }
                    break;
                case 0b111:  //M minus, sign flag is on
                    if (S())
                    {
                        call();
                        return;
                    }
                    else
                    {
                        pc += 2; //ignore next 2 bytes
                    }
                    break;


                default:
                    Debugger.Break();
                    break;
            }
        }


        private void RET()
        {
            int adr = ram[sp] + (256 * ram[sp + 1]);
            sp += 2;
            pc = (ushort)adr;
        }
        private void RET_Z()
        {
            bool z = Z();
            if (z)
            {
                int adr = ram[sp] + (256 * ram[sp + 1]);
                sp += 2;
                pc = (ushort)adr;
            }
            else
            {
                int a123 = 123;
            }
        }
        private void RET_NZ()
        {
            bool nz = NZ();
            if (nz)
            {
                int adr = ram[sp] + (256 * ram[sp + 1]);
                sp += 2;
                pc = (ushort)adr;
            }

        }
        private void RET_cond(int instruction)
        {
            int cond = (instruction / 8) & 7;
            Action jump = () =>
            {
                int adr = ram[sp] + (256 * ram[sp + 1]);
                sp += 2;
                pc = (ushort)adr;
            };

            switch (cond)
            {
                case 0b010: //NC
                    bool nc = !C();
                    if (nc)
                    {
                        int adr = ram[sp] + (256 * ram[sp + 1]);
                        sp += 2;
                        pc = (ushort)adr;
                    }
                    break;
                case 0b011: //C
                    if (C())
                    {
                        int adr = ram[sp] + (256 * ram[sp + 1]);
                        sp += 2;
                        pc = (ushort)adr;
                    }
                    break;
                case 0b110: //P
                    if (P()) { jump(); }
                    break;

                default:

                    Debugger.Break();
                    break;
            }
        }



        private void PUSH_HL()
        {
            ram[sp - 1] = h;
            ram[sp - 2] = l;
            sp -= 2;
        }
        private void PUSH_DE()
        {
            ram[sp - 1] = d;
            ram[sp - 2] = e;
            sp -= 2;
        }
        private void PUSH_BC()
        {
            ram[sp - 1] = b;
            ram[sp - 2] = c;
            sp -= 2;
        }
        private void PUSH_AF()
        {
            ram[sp - 1] = a;
            ram[sp - 2] = f;
            sp -= 2;
        }
        private void PUSH_IX()
        {
            ram[sp - 1] = (byte)(ix / 256);
            ram[sp - 2] = (byte)(ix % 256);
            sp -= 2;
        }
        private void PUSH_IY()
        {
            ram[sp - 1] = (byte)(iy / 256);
            ram[sp - 2] = (byte)(iy % 256);
            sp -= 2;
        }
        private void ADD_IX_BC()
        {
            int rez = ix + bc();
            bool newcarry = rez > 0xFFFF;
            ix = (ushort)rez;
            flag_adj_c(newcarry);
        }
        private void ADD_IY_BC()
        {
            int rez = iy + bc();
            bool newcarry = rez > 0xFFFF;
            iy = (ushort)rez;
            flag_adj_c(newcarry);
        }
        private void ADD_IX_DE()
        {
            int rez = ix + de();
            bool newcarry = rez > 0xFFFF;
            ix = (ushort)rez;
            flag_adj_c(newcarry);
        }
        private void ADD_IY_DE()
        {
            int rez = iy + de();
            bool newcarry = rez > 0xFFFF;
            iy = (ushort)rez;
            flag_adj_c(newcarry);
        }


        private void POP_HL()
        {
            l = ram[sp];
            h = ram[sp + 1];
            sp += 2;
        }
        private void POP_DE()
        {
            e = ram[sp];
            d = ram[sp + 1];
            sp += 2;
        }
        private void POP_BC()
        {
            c = ram[sp];
            b = ram[sp + 1];
            sp += 2;
        }
        private void POP_AF()
        {
            f = ram[sp];
            a = ram[sp + 1];
            sp += 2;
        }
        private void POP_IX()
        {
            ix = (ushort)(256 * ram[sp + 1] + ram[sp]);
            sp += 2;
        }
        private void POP_IY()
        {
            iy = (ushort)(256 * ram[sp + 1] + ram[sp]);
            sp += 2;
        }


        private void I_DJNZ_r()
        {
            b = (byte)(b - 1);
            byte radr = ram[pc++];

            //Debug.Assert(radr < 129, "should be signed");
            //Debug.Assert(radr > -129, "should be one byte");


            if (b > 0)
            {
                sbyte r = (sbyte)radr;
                pc = (ushort)(pc + r);
            }
        }

        private void EXX()
        {
            Swap(ref b, ref b_);
            Swap(ref c, ref c_);
            Swap(ref d, ref d_);
            Swap(ref e, ref e_);
            Swap(ref h, ref h_);
            Swap(ref l, ref l_);

        }
        private void EX_AFAF()
        {
            Swap(ref a, ref a_);
            Swap(ref f, ref f_);
        }
        private void Swap(ref byte a, ref byte b)
        {
            byte t = a;
            a = b;
            b = t;
        }
        private void EX_DE_HL()
        {
            Swap(ref d, ref h);
            Swap(ref e, ref l);
        }



        private void CPL()
        {
            //complement accumulator
            a = (byte)(a ^ 255);
        }

        private void IN_A_C()
        {
            //read from port C?
            a = 0x1F;   // binary 0001 1111  -> 5 keys not pressed?

            byte dum = 0;
            bool reckognizedPort = m_keyboard.ReadInPort(b, c, out dum, aam_instruction_counter);
            
            if (reckognizedPort)  //do not change a otherwise
            {
                a = dum;
            }

            //might not be smart to adjust z flag here, depends on the specification
            flag_adj_z(a);

        }

        private void IN_A_N()
        {
            //in a, (n)
            //read from port n

            byte n = ram[pc++];

            //duplicate code with IN_A_C


            byte dum = 0;
            bool reckognizedPort = m_keyboard.ReadInPort(a, n, out dum, aam_instruction_counter);

            a = 0x1F;   // binary 0001 1111  -> 5 keys not pressed?
            if (reckognizedPort)  //do not change a otherwise
            {
                a = dum;
            }
            //might not be smart to adjust z flag here, depends on the specification
            //flag_adj_z(a);
        }



        private void BIT_B_HLI(int cb_instruction)
        {
            //bit b, (HL) instruction        CB 46+8*b
            int bit = cb_instruction / 8;
            bit = bit & 7;

            byte n = ram[256 * h + l];
            int bitpos = 1 << bit;

            flag_adj_z((byte)(n & bitpos));
        }

        private void BIT_x_reg(int cb_instruction)
        {
            //BIT 4, C instruction  CB + 11bb brrr 

            int r = cb_instruction & 7;
            int bit = (cb_instruction / 8) & 7;
            byte n = (byte)(reg_readers[r]());
            int bitpos = 1 << bit;
            flag_adj_z((byte)(n & bitpos));
        }
        private void SET_x_reg(int cb_instruction)
        {
            //SET 7,(HL) instruction  CB + 11bbbrrr 

            int r = cb_instruction & 7;
            int bit = (cb_instruction / 8) & 7;
            byte n = (byte)(reg_readers[r]());
            int bitpos = 1 << bit;
            int rez = n | bitpos;
            reg_writers[r]((byte)rez);
        }
        private void RES_x_reg(int cb_instruction)
        {
            //RES 7,(HL) instruction  CB + 10bb brrr 

            int r = cb_instruction & 7;
            int bit = (cb_instruction / 8) & 7;
            byte n = (byte)(reg_readers[r]());
            int bitpos = 1 << bit;
            int rez = n & (0xFF ^ bitpos);
            reg_writers[r]((byte)rez);
        }

        private void RRA()
        {
            bool newcarry = (a % 2) > 0;
            a = (byte)(a >> 1); //to the right we rotate
                                //if( (f & FLAG_C) > 0){
            if (C())
            {
                a = (byte)(a | 128);  //turn on 7th bit
            }
            flag_adj_c(newcarry);
        }

        private void RRCA()
        {
            bool newcarry = (a & 1) != 0;

            a = (byte)(a >> 1); //to the right we rotate
                                //if( (f & FLAG_C) > 0){
            if (newcarry) //reuse for 8bit rotation
            {
                a = (byte)(a | 128);  //turn on 7th bit
            }
            flag_adj_c(newcarry);
        }


        private void RLA()
        {
            bool newcarry = (a & 128) != 0;
            int newval = a << 1;
            if (C())
            {
                newval = newval | 1;
            }
            a = (byte)newval;
            flag_adj_c(newcarry);
        }

        private void RLCA()
        {
            bool newcarry = (a & 128) != 0;
            int newval = a << 1;
            if (newcarry)
            {
                newval = newval | 1;
            }
            a = (byte)newval;
            flag_adj_c(newcarry);
        }



        private void RR_reg(int instruction)
        {
            //rotate right
            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val % 2) != 0;

            int newval = val >> 1;
            if (C())
            {
                newval = newval | 128; //c into 7th bit
            }
            reg_writers[r]((byte)newval);
            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
        }
        private void RRC_reg(int instruction)
        {
            //rotate right
            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val % 2) != 0;

            int newval = val >> 1;
            if (newcarry)
            {
                newval = newval | 128; //c into 7th bit
            }
            reg_writers[r]((byte)newval);
            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
        }

        private void RL_reg(int instruction)
        {
            //rotate left
            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val & 128) != 0;

            int newval = val << 1;
            if (C())
            {
                newval = newval | 1; //c into 0th bit
            }
            reg_writers[r]((byte)newval);
            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
            flag_adj_s((byte)newval);
        }
        private void RLC_reg(int instruction)
        {
            //rotate left
            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val & 128) != 0;

            int newval = val << 1;
            if (newcarry)
            {
                newval = newval | 1; //c into 0th bit
            }
            reg_writers[r]((byte)newval);
            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
            flag_adj_s((byte)newval);
        }




        private void SLA_reg(int instruction)
        {
            //shift left arithmetic
            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val & 128) != 0;

            int newval = val << 1;
            reg_writers[r]((byte)newval);

            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
            flag_adj_s((byte)newval);
        }



        private void SRL_reg(int instruction)
        {
            //shift right logical

            int r = instruction & 7;

            int val = reg_readers[r]();
            bool newcarry = (val % 2) != 0;

            int newval = val >> 1;
            reg_writers[r]((byte)newval);

            flag_adj_z((byte)newval);
            flag_adj_c(newcarry);
        }

        private void LDI()
        {
            int hl = h * 256 + l;
            int bc = b * 256 + c;
            int de = d * 256 + e;

            ram[de] = ram[hl];
            //bug lies here, all registers must be updated
            de++; hl++;
            bc--;
            d = (byte)(de / 256); e = (byte)(de % 256);
            b = (byte)(bc / 256); c = (byte)(bc % 256);
            h = (byte)(hl / 256); l = (byte)(hl % 256);
        }

        private void LDIRold()
        {
            int hl = h * 256 + l;
            int bc = b * 256 + c;
            int de = d * 256 + e;

            do
            {
                ram[de] = ram[hl];
                de++; hl++;
                bc--;
            } while (bc > 0);
            d = (byte)(de / 256); e = (byte)(de % 256);
            b = (byte)(bc / 256); c = (byte)(bc % 256);
            h = (byte)(hl / 256); l = (byte)(hl % 256);
        }
        private void LDIR()
        {
            int hl = h * 256 + l;
            int bc = b * 256 + c;
            int de = d * 256 + e;

            ram[de] = ram[hl];
            de++; hl++;
            bc--;
            if( bc > 0) { pc--; pc--; }
            
            d = (byte)(de / 256); e = (byte)(de % 256);
            b = (byte)(bc / 256); c = (byte)(bc % 256);
            h = (byte)(hl / 256); l = (byte)(hl % 256);
        }

        private void LDDR()
        {
            int hl = h * 256 + l;
            int bc = b * 256 + c;
            int de = d * 256 + e;

            do
            {
                ram[de] = ram[hl];
                de--; hl--;
                bc--;
            } while (bc > 0);
            d = (byte)(de / 256); e = (byte)(de % 256);
            b = (byte)(bc / 256); c = (byte)(bc % 256);
            h = (byte)(hl / 256); l = (byte)(hl % 256);
        }


        private void DI()
        {
            //from now on, there will be no interrupts.
        }


        private void LD_IX_NN()
        {
            ix = (ushort)(ram[pc] + 256 * ram[pc + 1]);
            pc += 2;
        }
        private void LD_IY_NN()
        {
            iy = (ushort)(ram[pc] + 256 * ram[pc + 1]);
            pc += 2;
        }

        private void LD_IY_NNI()
        {
            //LD IY,(NN)   load indirect

            int adr = (ushort)(ram[pc] + 256 * ram[pc + 1]);
            pc += 2;

            iy = (ushort)(ram[adr] + 256 * ram[adr + 1]);
        }


        private void LD_r_IXb()
        {
            sbyte b = (sbyte)ram[pc]; pc++;
            int r = (m_extOpcode / 8) & 7;
            int adr = ix + b;    //assuming b is always positive, turns out it was a wrong assumption
            byte val = ram[adr];
            reg_writers[r](val);
            Debug.Assert(r != 6); //(HL) is not available 
        }
        private void LD_r_IYb(int iy_instruction)
        {
            sbyte b = (sbyte)ram[pc]; pc++;  //offsets can be negative
            int r = (iy_instruction / 8) & 7;
            int adr = iy + b;    //assuming b is always positive
            byte val = ram[adr];
            reg_writers[r](val);
            Debug.Assert(r != 6); //(HL) is not available 
        }

        private void LD_IXb_n()
        {
            sbyte b = (sbyte)ram[pc++];
            byte n = ram[pc++];

            int adr = ix + b;
            ram[adr] = n;
        }
        private void LD_IYb_n()
        {
            sbyte b = (sbyte)ram[pc++];  //offsets can be negative
            byte n = ram[pc++];

            int adr = iy + b;
            ram[adr] = n;
        }

        private void ADC_A_IYb()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            int rez = a + ram[adr];
            if (C()) rez += 1;

            a = (byte)rez;
            flag_adj_c(rez > 255);
            flag_adj_z(a);
            flag_adj_s(a); //might be good might be not good
        }

        private void INC_IXd()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = ix + b;
            byte n = (byte)(ram[adr] + 1);
            ram[adr] = n;
            flag_adj_z(n);
            flag_adj_s(n);
        }
        private void INC_IYd()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            byte n = (byte)(ram[adr] + 1);
            ram[adr] = n;
            flag_adj_z(n);
            flag_adj_s(n);
        }
        private void DEC_IXd()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = ix + b;
            byte n = (byte)(ram[adr] - 1);
            ram[adr] = n;
            flag_adj_z(n);
            flag_adj_s(n);
        }
        private void DEC_IYd()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            byte n = (byte)(ram[adr] - 1);
            ram[adr] = n;
            flag_adj_z(n);
            flag_adj_s(n);
        }

        private void CP_IYb()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            int n = ram[adr];
            cp((byte)n);
        }



        private void LD_IXb_r(int ix_instruction)
        {
            sbyte b = (sbyte)ram[pc++];
            int r = ix_instruction & 7;
            byte val = (byte)reg_readers[r]();
            int adr = ix + b;
            ram[adr] = val;
        }
        private void LD_IYb_r(int iy_instruction)
        {
            sbyte b = (sbyte)ram[pc++];
            int r = iy_instruction & 7;
            byte val = (byte)reg_readers[r]();
            int adr = iy + b;
            ram[adr] = val;
        }


        private void ADD_A_IYb()
        {
            sbyte b = (sbyte)ram[pc++];
            int adr = iy + b;
            int n = ram[adr];
            int rez = a + n;
            a = (byte)rez;

            flag_adj_z((byte)rez);
            flag_adj_c(rez > 255);
            flag_adj_s((byte)rez);


        }


        private void SET_x_IYb(int ins3)
        {
            sbyte b = (sbyte)ram[pc++];
            Debug.Assert((byte)ins3 == ram[pc]);
            pc++;  //skip ins3 as well
            int bit = (ins3 / 8) & 7;

            int adr = iy + b;
            byte val = ram[adr];

            int bitpos = 1 << bit;
            int rez = val | bitpos;
            ram[adr] = (byte)rez;
        }
        private void BIT_x_IYb(int ins3)
        {
            sbyte b = (sbyte)ram[pc++];
            Debug.Assert((byte)ins3 == ram[pc]);
            pc++;  //skip ins3 as well
            int bit = (ins3 / 8) & 7;

            int adr = iy + b;
            byte val = ram[adr];

            int bitpos = 1 << bit;
            flag_adj_z((byte)(val & bitpos));

        }

        private void RES_x_IYb(int ins3)
        {
            sbyte b = (sbyte)ram[pc++];
            Debug.Assert((byte)ins3 == ram[pc]);
            pc++;  //skip ins3 as well
            int bit = (ins3 / 8) & 7;

            int adr = iy + b;
            byte val = ram[adr];

            int bitpos = 1 << bit;
            int rez = val & (0xFF ^ bitpos);
            ram[adr] = (byte)rez;
        }


        private void NEG()
        {
            flag_adj_c(a == 0);

            int rez = 0 - a;
            a = (byte)rez;
            flag_adj_z(a);
            flag_adj_s(a);
        }

        private void SCF()
        {
            flag_adj_c(true);
        }
        private void CCF()
        {
            flag_adj_c(!C());
        }

        private void RRD()
        {
            //wtf instruction
            //top (hl) nibble goes to bottom (HL)
            //bottom a goes to top (hl)
            //bottom (hl) goes to bottom a
            int t = a & 0xF; //save bottom a
            int m = ram[hl()];

            int aaa = (a & 0xF0) | (m & 0x0F);   //transfer bottom (hl) to bottom a
            m = (m >> 4) | (t << 4);           //transfer m and add a part
            ram[hl()] = (byte)m;
            a = (byte)aaa;

            flag_adj_z(a);
            flag_adj_s(a);
        }

        #endregion  




        public Spectrum(byte[] rom, byte[] z, IMemoryAccessVisualizer? viz, Keyboard kb, string ge)
        {
            //constructor that takes z80 file bytes

            this.m_z80File = ge; //used as a skoolkit file 
            this.m_vizmem = viz;
            this.m_keyboard = kb;
            Initialize_Reg_ReadersWriters();
            InitializeOpcodeArray();
            Array.Copy(rom, ram, rom.Length);


            //z80 file format
            /*
            Offset  Length  Description
            ---------------------------
            0       1       A register
            1       1       F register
            2       2       BC register pair (LSB, i.e. C, first)
            4       2       HL register pair
            6       2       Program counter
            8       2       Stack pointer
            10      1       Interrupt register
            11      1       Refresh register (Bit 7 is not significant!)
            12      1       Bit 0  : Bit 7 of the R-register
                            Bit 1-3: Border colour
                            Bit 4  : 1=Basic SamRom switched in
                            Bit 5  : 1=Block of data is compressed
                            Bit 6-7: No meaning
            13      2       DE register pair
            15      2       BC' register pair
            17      2       DE' register pair
            19      2       HL' register pair
            21      1       A' register
            22      1       F' register
            23      2       IY register (Again LSB first)
            25      2       IX register
            27      1       Interrupt flipflop, 0=DI, otherwise EI
            28      1       IFF2 (not particularly important...)
            29      1       Bit 0-1: Interrupt mode (0, 1 or 2)
                            Bit 2  : 1=Issue 2 emulation
                            Bit 3  : 1=Double interrupt frequency
                            Bit 4-5: 1=High video synchronisation
                                        3=Low video synchronisation
                                        0,2=Normal
                            Bit 6-7: 0=Cursor/Protek/AGF joystick
                                        1=Kempston joystick
                                        2=Sinclair 2 Left joystick (or user
                                        defined, for version 3 .z80 files)
                                        3=Sinclair 2 Right joystick
                */


            //create from z80 file
            a = z[0];
            f = z[1];

            c = z[2]; b = z[3];
            l = z[4]; h = z[5];

            sp = (ushort)(z[8] + (256 * (ushort)z[9]));

            //ignoring interuput register
            //ignoring r register

            byte border = (byte)(z[12] & 0x14);

            e = z[13]; d = z[14];

            c_ = z[15]; b_ = z[16];
            e_ = z[17]; d_ = z[18];
            l_ = z[19]; h_ = z[20];
            a_ = z[21];
            f_ = z[22];


            //pc must be 0, meaning there is an extended header
            if ((z[6] != 0) && (z[7] != 0))
            { //ver 1 format
                DoVer1Format(z);
            }
            else
            { //extended header

                //Debug.Assert((z[6] == 0) && (z[7] == 0), "we only understand extender format");


                /*
                        Offset  Length  Description
                        ---------------------------
                        * 30      2       Length of additional header block (see below)
                        * 32      2       Program counter
                        * 34      1       Hardware mode (see below)
                        * 35      1       If in SamRam mode, bitwise state of 74ls259.
                                        For example, bit 6=1 after an OUT 31,13 (=2*6+1)
                                        If in 128 mode, contains last OUT to 0x7ffd
                            If in Timex mode, contains last OUT to 0xf4
                        * 36      1       Contains 0xff if Interface I rom paged
                            If in Timex mode, contains last OUT to 0xff
                        * 37      1       Bit 0: 1 if R register emulation on
                                        Bit 1: 1 if LDIR emulation on
                            Bit 2: AY sound in use, even on 48K machines
                            Bit 6: (if bit 2 set) Fuller Audio Box emulation
                            Bit 7: Modify hardware (see below)
                        * 38      1       Last OUT to port 0xfffd (soundchip register number)
                        * 39      16      Contents of the sound chip registers
                        55      2       Low T state counter
                        57      1       Hi T state counter
                        58      1       Flag byte used by Spectator (QL spec. emulator)
                                        Ignored by Z80 when loading, zero when saving
                        59      1       0xff if MGT Rom paged
                        60      1       0xff if Multiface Rom paged. Should always be 0.
                        61      1       0xff if 0-8191 is ROM, 0 if RAM
                        62      1       0xff if 8192-16383 is ROM, 0 if RAM
                        63      10      5 x keyboard mappings for user defined joystick
                        73      10      5 x ASCII word: keys corresponding to mappings above
                        83      1       MGT type: 0=Disciple+Epson,1=Disciple+HP,16=Plus D
                        84      1       Disciple inhibit button status: 0=out, 0ff=in
                        85      1       Disciple inhibit flag: 0=rom pageable, 0ff=not
                        ** 86      1       Last OUT to port 0x1ffd
                The value of the word at position 30 is 23 for version 2 files, and 54 or 55 for version 3; the fields marked '*' are the ones that are present in the version 2 header. The final byte (marked '**') is present only if the word at position 30 is 55.

                In general, the fields have the same meaning in version 2 and 3 files, with the exception of byte 34:

                        Value:          Meaning in v2           Meaning in v3
                        -----------------------------------------------------
                            0             48k                     48k
                            1             48k + If.1              48k + If.1
                            2             SamRam                  SamRam
                            3             128k                    48k + M.G.T.
                            4             128k + If.1             128k
                            5             -                       128k + If.1
                            6             -                       128k + M.G.T.
                */

                int additionalHeader = z[30] + 256 * z[31];
                pc = (ushort)(z[32] + 256 * z[33]);

                byte hardwareVer = z[34];
                Debug.Assert(hardwareVer == 0, "zx spectrum 48k only");

                int ramstart = 30 + 2 + additionalHeader;
                CopyZ80ToRam(z, ramstart);
            }
        }

        //public Spectrum(byte[] rom, byte[] z, IMemoryAccessVisualizer? viz, Keyboard kb, string ge)
        public Spectrum(byte[] inmem, Registers inregs, Keyboard kb )
        {
            //contstructor that takes saved state from recording log
            Array.Copy(inmem, ram, inmem.Length);
            inregs.CopyToSpectrum(this);

            m_keyboard = kb;
            

        }



        public byte a;
        public byte f;
        public byte b, c, h, l, d, e;
        public ushort sp;
        public ushort pc;

        public byte b_, c_, h_, l_, d_, e_;
        public byte a_, f_;

        public ushort ix;
        public ushort iy;

        public byte[] ram = new byte[65536];

        public ushort executedAddress;


        /*  flags register
    Bit 7: Sign Flag
    Bit 6: Zero Flag
    Bit 5: Not Used
    Bit 4: Half Carry Flag
    Bit 3: Not Used
    Bit 2: Parity / Overflow Flag
    Bit 1: Add / Subtract Flag
    Bit 0: Carry Flag
            */

        public byte FLAG_C = 0x01;
        public byte FLAG_Z = 0x40;
        public byte FLAG_S = 0x80;

        private bool NZ()
        {
            //return (f & 0x40) == 0;
            return (f & FLAG_Z) == 0;

        }
        private bool Z()
        {
            return (f & FLAG_Z) != 0;
        }
        private bool C()
        {
            return (f & FLAG_C) != 0;
        }
        private bool S()
        {
            return (f & FLAG_S) != 0;
        }
        private bool P() //same as S, (S is never called S)
        {
            return (f & FLAG_S) == 0; //positive means there is no sign bit
        }

        public ushort bc()
        {
            int x = b * 256 + c;
            return (ushort)x;
        }
        public ushort de()
        {
            int x = d * 256 + e;
            return (ushort)x;
        }
        public ushort hl()
        {
            int x = h * 256 + l;
            return (ushort)x;
        }


        private void CopyZ80ToRam(byte[] z, int starta)
        {
            //this sht is not that simple,  ram is in blocks, first two numbers are size (uncompressed) third is the block number:
            // 4 ->
            /*
            The pages are numbered, depending on the hardware mode, in the following way:

                    Page    In '48 mode     In '128 mode    In SamRam mode
                    ------------------------------------------------------
                        0      48K rom         rom (basic)     48K rom
                        1      Interface I, Disciple or Plus D rom, according to setting
                        2      -               rom (reset)     samram rom (basic)
                        3      -               page 0          samram rom (monitor,..)
                        4      8000-bfff       page 1          Normal 8000-bfff
                        5      c000-ffff       page 2          Normal c000-ffff
                        6      -               page 3          Shadow 8000-bfff
                        7      -               page 4          Shadow c000-ffff
                        8      4000-7fff       page 5          4000-7fff
                        9      -               page 6          -
                    10      -               page 7          -
                    11      Multiface rom   Multiface rom   -
            In 48K mode, pages 4,5 and 8 are saved. 
                */

            int length = z.Length;
            int block = starta;


            do
            {


                int block_len = z[block] + 256 * z[block + 1];
                int block_num = z[block + 2];
                int dest = BlockAdrFromNumber(block_num);
                int block_start = block + 3;



                UncompressBlock(z, dest, block_start, block_len);

                block = block_start + block_len;

            } while (block < length);
            Debug.Assert(block == length, "blocks dont fill up");

        }





        private void UncompressBlock(byte[] z, int dest, int block_start, int block_len)
        {

            int debug_ED_count = 0;
            int debug_expanding = 0;

            //int len = z.Length - start;
            int adr = dest;
            int p = block_start;
            for (int i = 0; i < block_len; i++)
            {

                if (z[p] == 0xED)
                {
                    string test = "";
                    for (int kd = 0; kd < 10; kd++)
                    {
                        if (p + kd >= z.Length) break;
                        test += " " + z[p + kd];
                    }
                    int a123 = 123;
                }

                if (i == block_len - 1)
                { //handle last byte
                    ram[adr] = z[p];
                    adr++;
                    p++;
                    continue;

                }

                //int cur = i + start;
                //int cur = p;
                if ((z[p] == 0xED) && (z[p + 1] == 0xED))
                {
                    debug_ED_count++;

                    int repeat = z[p + 2];
                    byte val = z[p + 3];
                    Debug.Assert((repeat > 4) || (val == 0xED), "wierd compression");
                    for (int j = 0; j < repeat; j++)
                    {
                        ram[adr] = val;
                        adr++;

                    }
                    //inc pointer as well
                    p += 4;
                    i += 3;

                    debug_expanding += (repeat - 2);
                }
                else
                {
                    //uncompressed byte
                    ram[adr] = z[p];
                    adr++;
                    p++;
                }

            }

            int uncompressed_len = adr - dest;
            int excess = uncompressed_len - 16384;
            int extra_len = debug_expanding;
            int diff_between_companddecomp = uncompressed_len - block_len;
            string rez = "" + debug_ED_count + " " + block_len;

        }

        private int BlockAdrFromNumber(int block_num)
        {
            int dest = -1;
            switch (block_num)
            {
                case 4:
                    dest = 0x8000;
                    break;
                case 5:
                    dest = 0xC000;
                    break;
                case 8:
                    dest = 0x4000;
                    break;
                default:
                    Debug.Assert(false, "unknown block num");
                    break;
            }
            return dest;
        }

        private void DoVer1Format(byte[] z)
        {

            pc = (ushort)(z[6] + 256 * z[7]);


            int ramstart = 30;

            //1111111111111


            //int len = z.Length - start;
            int adr = 16384;
            int p = ramstart;

            int block_len = z.Length - ramstart;
            for (int i = 0; i < block_len; i++)
            {


                if (i == block_len - 1)
                { //handle last byte
                    ram[adr] = z[p];
                    adr++;
                    p++;
                    continue;
                }

                if ((z[p] == 0xED) && (z[p + 1] == 0xED))
                {
                    int repeat = z[p + 2];
                    byte val = z[p + 3];
                    Debug.Assert((repeat > 4) || (val == 0xED), "wierd compression");
                    for (int j = 0; j < repeat; j++)
                    {
                        ram[adr] = val;
                        adr++;
                    }
                    //inc pointer as well
                    p += 4; i += 3;

                }
                else
                {

                    if (adr >= 65536) return;

                    //uncompressed byte
                    ram[adr] = z[p];
                    adr++;
                    p++;
                }
                //ram[16384 + i] = z[i + start];

            }



        }


        /*
        private Brush[] m_brushes = new Brush[]{
        Brushes.Black,
        new SolidBrush(Color.FromArgb(0,    (byte)0,    (byte)0xee)),
        new SolidBrush(Color.FromArgb(0xee, (byte)0,    (byte)0   )),
        new SolidBrush(Color.FromArgb(0xee, (byte)0,    (byte)0xee)),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0xee)),
        new SolidBrush(Color.FromArgb(0xee, (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0xee, (byte)0xee, (byte)0xee)),
            };
        private Brush[] m_brushes_bright = new Brush[]{
        Brushes.Black,
        new SolidBrush(Color.FromArgb(0,    (byte)0,    (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0,    (byte)0   )),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0,    (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0,    (byte)0xee, (byte)0xFF)),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0xee, (byte)0   )),
        new SolidBrush(Color.FromArgb(0xFF, (byte)0xee, (byte)0xFF)),
        };*/


        /*
        public void DrawScreen(Graphics g)
        {
            Brush black = Brushes.Black;
            Brush fore;
            Brush background;

            int screen_start = 16384 + 0;
            int attributes_start = 16384 + 256 * 192 / 8;

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


                for (int x = 0; x < 256; x += 8)
                {
                    int atrbyte = ram[adr_attrib];
                    bool bright = (atrbyte & 0x40) > 0;
                    if (bright)
                    {
                        //take 3 last bits
                        fore = m_brushes_bright[atrbyte % 8];
                        //take next 3 bits
                        background = m_brushes_bright[atrbyte / 8 % 8];
                    }
                    else
                    {
                        fore = m_brushes[atrbyte % 8];
                        background = m_brushes[atrbyte / 8 % 8];
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        int bitmask = (128 >> i);
                        int membyte = ram[adr];
                        //var drawer = br => { g.FillRectangle(br, 4 * (x + i), 4 * y, 4, 4); return 0};
                        //Action<Brush> drawer = br => { g.FillRectangle(br, 4 * (x + i), 4 * y, 4, 4);};
                        var drawer = void (Brush br) => g.FillRectangle(br, 4 * (x + i), 4 * y, 4, 4);
                        if ((membyte & bitmask) > 0)
                        {
                            //g.FillRectangle(black, x +i, y, 1, 1);
                            //g.FillRectangle(black, 2*(x + i), 2*y, 2, 2);

                            //g.FillRectangle(black, 4 * (x + i), 4 * y, 4, 4);
                            //g.FillRectangle(fore, 4 * (x + i), 4 * y, 4, 4);
                            drawer(fore);

                        }
                        else
                        {
                            //g.FillRectangle(background, 4 * (x + i), 4 * y, 4, 4);
                            drawer(background);
                        }
                    }

                    adr++;
                    adr_attrib++;

                }
            }


        }
        */

        public byte[] GetRam()
        {
            return ram;
        }

        /*
        public void DrawScreen2(Graphics g)
        {
            DrawScreenFromRAM(g, ram);
        }
        public void DrawScreenFromRAM(Graphics g, byte[] ram)
        {
            //this actually does not depend on anything other than the ram, it does not need to be in thie class
            int PIX = 4;

            //new version, background will be drawn separately, to avoid drawing millions of background boxes
            Brush black = Brushes.Black;
            Brush fore;
            Brush background;

            int screen_start = 16384 + 0;
            int attributes_start = 16384 + 256 * 192 / 8;

            //draw attribs
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int adr_attrib = attributes_start + (y / 8) * 32;
                    int atrbyte = ram[adr_attrib];
                    bool bright = (atrbyte & 0x40) > 0;
                    if (bright)
                    {
                        //take next 3 bits
                        background = m_brushes_bright[atrbyte / 8 % 8];
                    }
                    else
                    {
                        background = m_brushes[atrbyte / 8 % 8];
                    }
                    //draw 8 x 8 pixels box
                    var drawer = void (Brush br) => g.FillRectangle(br, 8 * PIX * x, 8 * PIX * y, 8 * PIX, 8 * PIX);
                    drawer(background);


                }
            }


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


                for (int x = 0; x < 256; x += 8)
                {
                    int atrbyte = ram[adr_attrib];
                    bool bright = (atrbyte & 0x40) > 0;
                    if (bright)
                    {
                        //take 3 last bits
                        fore = m_brushes_bright[atrbyte % 8];
                        //take next 3 bits
                        background = m_brushes_bright[atrbyte / 8 % 8];
                    }
                    else
                    {
                        fore = m_brushes[atrbyte % 8];
                        background = m_brushes[atrbyte / 8 % 8];
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        int bitmask = (128 >> i);
                        int membyte = ram[adr];
                        //var drawer = br => { g.FillRectangle(br, 4 * (x + i), 4 * y, 4, 4); return 0};
                        //Action<Brush> drawer = br => { g.FillRectangle(br, 4 * (x + i), 4 * y, 4, 4);};
                        var drawer = void (Brush br) => g.FillRectangle(br, PIX * (x + i), PIX * y, PIX, PIX);
                        if ((membyte & bitmask) > 0)
                        {
                            //g.FillRectangle(black, x +i, y, 1, 1);
                            //g.FillRectangle(black, 2*(x + i), 2*y, 2, 2);

                            //g.FillRectangle(black, 4 * (x + i), 4 * y, 4, 4);
                            //g.FillRectangle(fore, 4 * (x + i), 4 * y, 4, 4);
                            drawer(fore);

                        }
                        else
                        {
                            //drawer(background); //moved to separate loop
                        }
                    }

                    adr++;
                    adr_attrib++;

                }
            }


        }
        */

        public void Poke(int adr, int a)
        {
            byte b = (byte)a;
            ram[adr] = b;

        }



        public class MemWriteLog
        {
            public int dest;
            public int size;
            public int originInstuctionLoc;
            public MemWriteLog(int d, int s, int o)
            {
                dest = d; size = s; originInstuctionLoc = o;
            }
        }
        public List<MemWriteLog> dbg_mlog = new();
        private void DebugMemoryWrite(int destinationAdr, int size, int writeInstructionAdr)
        {
            return;
            var m = new MemWriteLog(destinationAdr, size, writeInstructionAdr);
            dbg_mlog.Add(m);
        }

        public void ClearLines()
        {
            dbg_mlog = new();
        }

        public StringWriter dbg_rez;
        private void DebugLogSubBugs(int bugadr)
        {
            if (dbg_rez == null) dbg_rez = new StringWriter();
            dbg_rez.WriteLine($"sub bug {bugadr:X4}");


        }



        public void PaintMemAccess(Graphics g)
        {
            //65536 to 1900 converter

            foreach (var a in dbg_mlog)
            {
                int x1 = a.originInstuctionLoc / 65;
                int y1 = 10;

                int x2 = a.dest / 65;
                int y2 = 310;

                g.DrawLine(Pens.Black, x1, y1, x2, y2);

            }

        }

        public byte[] CopyOfMemory()
        {
            return (byte[])ram.Clone();
        }

        public void CopyRegistersToXZ80(ref Z80Lib.Context ctx)
        {
            //at least those we use
            ctx.pc.w = pc;

            ctx.af.a = a;
            ctx.af_.a = a_;


            ctx.hl.h = h;
            ctx.hl.l = l;
            ctx.hl_.h = h_;
            ctx.hl_.l = l_;

            ctx.bc.b = b;
            ctx.bc.c = c;
            ctx.bc_.b = b_;
            ctx.bc_.c = c_;

            ctx.de.d = d;
            ctx.de.e = e;
            ctx.de_.d = d_;
            ctx.de_.e = e_;

            ctx.ix.w = ix;
            ctx.iy.w = iy;

            ctx.sp.w = sp;
        }

        public bool ComparetoXZ80(Z80Lib.Z80 x)
        {
            var ctx = x.getContext();

            int xhl = ctx.hl.w;
            if (ctx.af.a != a) return false;
            if (ctx.hl.h != h) return false;
            if (ctx.hl.l != l) return false;
            if (ctx.bc.b != b) return false;
            if (ctx.bc.c != c) return false;
            if (ctx.de.d != d) return false;
            if (ctx.de.e != e) return false;
            if (ctx.ix.w != ix) return false;
            if (ctx.iy.w != iy) return false;





            if (ctx.pc.w != pc) return false;

            return true;
        }

        public string CompareAndGiveReason(Z80Lib.Z80 x)
        {
            string rez = "";
            var ctx = x.getContext();
            var xpc = ctx.pc.w;
            if (xpc != pc) rez = rez + $"xpc: {xpc:X4}  pc: {pc:X4}";
            if (ctx.af.a != a) rez = rez + $"xa: {ctx.af.a:X4}  a: {a:X4}";

            return rez;
        }


        //debug dump bytes for onlinedissasembler
        public string DebugDumpBytes(int start, int size) //start is in bytes
        {
            var sb = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                sb.Append($"{ram[start + i]:X2} ");
            }
            return sb.ToString();
        }

        public void DebugFixAttribsBottom()

        {
            for (int i = 0; i < 32; i++)
            {
                ram[736 + 22528] = 7;  //trying something like black on white
            }
        }

        public string Disassemble()
        {
            return DebugUnknownInstruction(0, pc);
        }

        

        //this function needs a name
        public string DebugUnknownInstruction(int instruction, int ipc)
        {
            //ipc = original pc at the start of the instruction
            //call external program capturing the output

            return  SkoolkitIPC.DisassembleTemp(ipc, m_z80File);

            /* moved to SkoolkitIPC 
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
                + m_z80File;

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
            */

        }



        //public void StartLog(ExecLog log, ExecLogV1FullSnapshots logTester)
        public void StartLog(RecordingLog log, ExecLogV1FullSnapshots logTester)
        {
            m_execLogEnabled = true; //not sure about this one
            m_recLog = log;
            m_execLogTester = logTester; //to be removed
        }



        private bool m_instaExitRequested = false;
        public RecordingLog StopLog()
        {
            //stop execution
            m_execLogEnabled = false;
            m_instaExitRequested = true;

            m_recLog.SetLastInstructionCounter(aam_instruction_counter);

            return m_recLog;
        }
        public RecordingLog GetCurrentPartialLog()
        {
            Debugger.Break();
            //return Recording?
            return m_recLog;
        }


        public override string ToString()
        {
            return $"pc:{pc:X4}";
        }

        internal void SetStateScreenOnly(ExecLogState entry)
        {
            //copy entry to member variables
            Array.Copy(entry.ramC, 0x4000, ram, 0x4000, 256 / 8 * 192 + 256 * 3);
            //registers not copied

            //this was off, for the continuation run working
            //but if i do change pc, then i can change the address for disassembler

            pc = entry.executedAddress;

        }


        public void LoopSteps(PictureBoxReplacement scr)
        {
            
            bool stop_error = false;
            bool exit = false;
            do
            {

                //SavePCLog();

                Step(out stop_error);

                if (aam_instruction_counter % 5000 == 0)
                {
                    scr.Refresh();
                }

                exit = false;
            } while (exit == false);
        }


        public void LoopSteps(PictureBoxReplacement scr, int steps)
        {
            mscr = scr;

            for (int i = 0; i < steps; i++)
            {
                //debug
                int dbgadr = 0x5800 + (9 * 32);  //attrib at coors 9,0
                int dbgval = ram[dbgadr];

                if (pc == 0x6A38) //setup room
                {
                    //st();
                }

                bool stop_error = false;
                Step(out stop_error);

                if (stop_error) break;

                DebugChecks();


                if (aam_instruction_counter % 5000 == 0)
                {
                    //scr.Refresh();
                }

                //check for modified mem
                if (ram[dbgadr] != dbgval)
                {
                    scr.Refresh();
                    //Debugger.Break();
                }

            }
        }

        private void DebugChecks()
        {
            if (ram[0xE1BF] == 0)
            {
                //Debugger.Break();
            }


        }



        private PictureBoxReplacement mscr;
        public void st()
        {
            mscr.Refresh();
            Debugger.Break();
        }

        public bool dbg_logInstructions;
        public void DebugStartInstructionLog()
        {
            dbg_logInstructions = true;
        }
        public void DebugStopIL()
        {
            dbg_logInstructions = false;
        }
        public void DebugROM()
        {
            //rainbow rom
            for (int i = 0; i < 16384; i++)
            {
                ram[i] = (byte)i;
            }
        }






        internal string DisplayPC()
        {
            string hexadr = String.Format("0x{0:X4}", pc);
            return hexadr;
        }

        public string DisplayRegisters()
        {
            var rez = new StringBuilder();

            rez.AppendFormat("BC {0:X2}{1:X2}\r\n", b, c);
            rez.AppendFormat("DE {0:X2}{1:X2}\r\n", d, e);
            rez.AppendFormat("HL {0:X2}{1:X2}\r\n", h, l);
            rez.AppendFormat("A {0:X2}\r\n", a);


            rez.AppendFormat("\r\nstack:");
            for (int i = 0; i < 10; i++)
            {
                int x1 = i * 2 + sp;
                int x2 = i * 2 + sp + 1;
                if (x2 > 65535)
                {
                    rez.AppendFormat(" END");
                    break;
                }
                rez.AppendFormat(" {0:X4}", i * 2 + ram[x1] + 256 * ram[x2]);
                if (i * 2 + sp + 2 >= 65535 - 1)
                {
                    rez.AppendFormat(" END");
                    break;
                }
            }

            return rez.ToString();
        }

        public long GetInstructionCount()
        {
            return aam_instruction_counter;
        }


    } // end of class Spectrum 




    /* this form does not allow parameterless construction usage with named property initializers
    public record InstructionDef(Func<byte, bool> OpcodeDecoder,
                                 Action Instruction,
                                 LogStateMaker Logger);
    */


    //public delegate Func<ExecLogState, ExecLogState> LogStateMaker(byte op, ExecLogState s);
    public delegate Func<ExecLogState, ExecLogState> LogStateMaker();

    public record InstructionDef
    {
        public Func<byte, bool> OpcodeDecoder { get; init; } //? = default;
        public Action Instruction { get; init; }
        public LogStateMaker Logger { get; init; }
    }


    /*
    (Func<byte, bool>) (op => op == 0x25),
                (Action) (() =>  {
                    I_DecH();
}),
                (LogStateMaker)((opc, state) => {

    */



    public class PictureBoxReplacement
    {
        private Action action;
        public PictureBoxReplacement(Action a)
        {
            action = a;
        }
        public void Refresh()
        {
            action();
        }
    }
}
