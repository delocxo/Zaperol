class Chunk
{
    public List<Instruction> Instructions { get; } = [];
    public List<Value> Constants { get; } = [];
    public List<Position> Positions { get; } = [];
    public int LocalCount;

    public Instruction[] InstructionsArray { get; set; } = [];
    public Value[] ConstantsArray { get; set; } = [];
    public Position[] PositionsArray { get; set; } = [];

    public void ConvertListsToArrays()
    {
        InstructionsArray = Instructions.ToArray();
        ConstantsArray = Constants.ToArray();
        PositionsArray = Positions.ToArray();
    }

    public int GetCompilerLocal() => LocalCount++;

    public int AddConst(Value value)
    {
        Constants.Add(value);
        return Constants.Count - 1;
    }

    public int AddLoadConst(Value value, Position position)
    {
        int constant = AddConst(value);
        return AddInstruction(constant, OpCode.LoadConst, position);
    }

    public int AddInstruction(Instruction instruction, Position position)
    {
        Instructions.Add(instruction);
        Positions.Add(position);
        return Instructions.Count - 1;
    }

    public void PatchA(int index, int value)
    {
        Instruction instruction = Instructions[index];
        instruction.A = value;
        Instructions[index] = instruction;
    }

    public void PatchA(int index)
    {
        PatchA(index, Instructions.Count);
    }

    public int AddInstruction(OpCode opCode, Position position)
        => AddInstruction(new Instruction(opCode), position);

    public int AddInstruction(int a, OpCode opCode, Position position)
        => AddInstruction(new Instruction(a, opCode), position);

    public int AddInstruction(int a, int b, OpCode opCode, Position position)
        => AddInstruction(new Instruction(a, b, opCode), position);

    public int AddInstruction(int a, int b, int c, OpCode opCode, Position position)
        => AddInstruction(new Instruction(a, b, c, opCode), position);

    public int AddInstruction(int a, int b, int c, int d, OpCode opCode, Position position)
        => AddInstruction(new Instruction(a, b, c, d, opCode), position);
}