using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TomasuloSimulator
{
    class State
    {
        public List<ReserveStation> RS;
        public List<Register> Reg;
        public List<LoadUnit> LU;
        public List<Instruction> Ins;
        public int NextToIssue;

        public State(List<Instruction> Ins)
        {
            RS = new List<ReserveStation>();
            RS.Add(new ReserveStation("Add1"));
            RS.Add(new ReserveStation("Add2"));
            RS.Add(new ReserveStation("Add3"));
            RS.Add(new ReserveStation("Mult1"));
            RS.Add(new ReserveStation("Mult2"));
            Reg = new List<Register>();
            for (int i = 0; i < 16; i++)
                Reg.Add(new Register());
            LU = new List<LoadUnit>();
            LU.Add(new LoadUnit("Load1"));
            LU.Add(new LoadUnit("Load2"));
            LU.Add(new LoadUnit("Load3"));
            this.Ins = Ins;
        }

        public void save_result(Unit u, Value v)
        {
            foreach (Register r in Reg)
            {
                if (Object.ReferenceEquals(r.Qi, u))
                {
                    r.Qi = null;
                    r.value = v;
                }
            }
        }

        public void step()
        {
            foreach (LoadUnit lu in LU)
            {
                if (lu.busy)
                {
                    if (lu.ins.exec_start_time == 0)
                    {
                        lu.ins.exec_start_time = CPU.cycle;
                        lu.time = CPU.load_time - 1;
                        lu.address = String.Format("R[R{0}]+{1}", lu.ins.o3, lu.ins.o2);
                    }
                    else if (lu.ins.exec_end_time == 0)
                    {
                        lu.time--;
                        if (lu.time == 0)
                        {
                            lu.ins.exec_end_time = CPU.cycle;
                            lu.result = new Value("M[" + lu.address + "]");
                        }
                    }
                    else
                    {
                        lu.ins.wb_time = CPU.cycle;
                        lu.busy = false;
                        save_result(lu, lu.result);
                    }
                }
            }

            foreach (ReserveStation rs in RS)
            {

            }

            if (NextToIssue < Ins.Count)
            {
                switch (Ins[NextToIssue].op)
                {
                    case OpType.LD:
                        foreach (LoadUnit lu in LU)
                        {
                            if (!lu.busy)
                            {
                                Ins[NextToIssue].issue_time = CPU.cycle;
                                lu.ins = Ins[NextToIssue];
                                lu.busy = true;
                                lu.address = lu.ins.o2.ToString();
                                lu.result = null;
                                Reg[lu.ins.o1 / 2].Qi = lu;
                                NextToIssue++;
                                break;
                            }
                        }
                        break;
                    default:
                        int start, end;
                        if (Ins[NextToIssue].op == OpType.ADD || Ins[NextToIssue].op == OpType.SUB)
                        {
                            start = 0;
                            end = 3;
                        }
                        else
                        {
                            start = 3;
                            end = 5;
                        }
                        for (int i = start; i < end; i++)
                        {
                            if (!RS[i].busy)
                            {
                                Ins[NextToIssue].issue_time = CPU.cycle;
                                RS[i].ins = Ins[NextToIssue];
                                RS[i].busy = true;
                                //RS[i].time = Ins[NextToIssue].op==OpType.ADD || Ins[NextToIssue].op==OpType.SUB ? CPU.add_time :
                                //    Ins[NextToIssue].op==OpType.MUL?CPU.mul_time:CPU.div_time;
                                NextToIssue++;
                                break;
                            }
                        }
                        break;
                }
            }
        }
    }

    enum OpType
    {
        LD, ADD, SUB, MUL, DIV
    }

    class Value
    {
        private static int count = 0;

        public String v;
        public int num;

        public Value(String v)
        {
            count++;
            num = count;
            this.v = v;
        }

        public override string ToString()
        {
            return "M" + num.ToString();
        }
    }

    class Unit
    {
        public int time;
        public String name;
        public bool busy;
        public Value result;
        public Instruction ins;

        public Unit(String name)
        {
            this.name = name;
        }
    }

    class ReserveStation : Unit
    {
        public Value Vj;
        public Value Vk;
        public Unit Qj;
        public Unit Qk;

        public ReserveStation(string name) : base(name)
        {
        }
    }

    class LoadUnit : Unit
    {
        public String address;

        public LoadUnit(string name) : base(name)
        {
        }
    }

    class Instruction
    {
        public OpType op;
        public int o1;
        public int o2;
        public int o3;
        public int issue_time;
        public int exec_start_time;
        public int exec_end_time;
        public int wb_time;

        public override String ToString()
        {
            switch (op)
            {
                case OpType.LD:
                    return String.Format("L.D F{0},{1}(R{2})", o1, o2, o3);
                case OpType.ADD:
                    return String.Format("ADD.D F{0},F{1},F{2}", o1, o2, o3);
                case OpType.SUB:
                    return String.Format("SUB.D F{0},F{1},F{2}", o1, o2, o3);
                case OpType.MUL:
                    return String.Format("MULT.D F{0},F{1},F{2}", o1, o2, o3);
                case OpType.DIV:
                    return String.Format("DIV.D F{0},F{1},F{2}", o1, o2, o3);
            }
            return "";
        }
    }

    class Register
    {
        public Unit Qi;
        public Value value;
    }
}
