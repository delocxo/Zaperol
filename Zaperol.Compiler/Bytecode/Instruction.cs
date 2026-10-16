struct Instruction
{
    public Instruction(OpCode opCode)
    {
        OpCode = opCode;
    }

    public Instruction(int a, OpCode opCode)
    {
        A = a;
        OpCode = opCode;
    }

    public Instruction(int a, int b, OpCode opCode)
    {
        A = a;
        B = b;
        OpCode = opCode;
    }

    public Instruction(int a, int b, int c, OpCode opCode)
    {
        A = a;
        B = b;
        C = c;
        OpCode = opCode;
    }

    public Instruction(int a, int b, int c, int d, OpCode opCode)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        OpCode = opCode;
    }

    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
    public int D { get; set; }
    public OpCode OpCode { get; }

    public override string ToString()
    {
        return $"{OpCode}, {A}, {B}, {C}, {D}";
    }
}