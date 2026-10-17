class CallFrame
{
    public CallFrame(Chunk chunk)
    {
        Chunk = chunk;
        Locals = new Value[chunk.LocalCount];
    }

    public Chunk Chunk { get; }
    public int Ip { get; set; }
    public Value[] Locals { get; }

    public int InstructionCount => Chunk.InstructionsArray.Length;

    public ref Instruction GetInstruction()
    {
        return ref Chunk.InstructionsArray[Ip++];
    }

    public ref Value GetConstant(int index)
    {
        return ref Chunk.ConstantsArray[index];
    }
}

static class Vm
{
    public static async Task<Value> ExecuteAsync(Chunk chunk)
    {
        Stack<CallFrame> callFrames = new(1024);
        Stack<Value> stack = new(1024);
        callFrames.Push(new CallFrame(chunk));

        while (true)
        {
            CallFrame frame = callFrames.Peek();

            ref Instruction instruction = ref frame.GetInstruction();

            switch (instruction.OpCode)
            {
                case OpCode.LoadLocal:
                    {
                        int index = instruction.A;
                        stack.Push(frame.Locals[index]);
                        break;
                    }

                case OpCode.StoreLocal:
                    {
                        int index = instruction.A;
                        frame.Locals[index] = stack.Pop();
                        break;
                    }

                case OpCode.LoadConst:
                    {
                        int index = instruction.A;
                        stack.Push(frame.GetConstant(index));
                        break;
                    }

                case OpCode.MakeArray:
                    {
                        int elementCount = instruction.A;
                        Value[] array = new Value[elementCount];
                        for (int i = elementCount - 1; i >= 0; i--)
                            array[i] = stack.Pop();
                        stack.Push(new(array));
                        break;
                    }

                case OpCode.AddIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int + right.Int));
                        break;
                    }

                case OpCode.AddNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a + b));
                        break;
                    }

                case OpCode.SubIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int - right.Int));
                        break;
                    }

                case OpCode.SubNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a - b));
                        break;
                    }

                case OpCode.MulIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int * right.Int));
                        break;
                    }

                case OpCode.MulNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a * b));
                        break;
                    }

                case OpCode.DivIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int / right.Int));
                        break;
                    }

                case OpCode.DivNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a / b));
                        break;
                    }

                case OpCode.ModIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int % right.Int));
                        break;
                    }

                case OpCode.ModNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a % b));
                        break;
                    }

                case OpCode.LessIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int < right.Int));
                        break;
                    }

                case OpCode.LessNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a < b));
                        break;
                    }

                case OpCode.GreaterIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int > right.Int));
                        break;
                    }

                case OpCode.GreaterNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a > b));
                        break;
                    }

                case OpCode.LessEqualIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int <= right.Int));
                        break;
                    }

                case OpCode.LessEqualNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a <= b));
                        break;
                    }

                case OpCode.GreaterEqualIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int >= right.Int));
                        break;
                    }

                case OpCode.GreaterEqaulNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a >= b));
                        break;
                    }

                case OpCode.EqualsIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int == right.Int));
                        break;
                    }

                case OpCode.EqaulsNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a == b));
                        break;
                    }

                case OpCode.NegateInt:
                    {
                        Value value = stack.Pop();
                        stack.Push(new(-value.Int));
                        break;
                    }

                case OpCode.NegateFloat:
                    {
                        Value value = stack.Pop();
                        stack.Push(new(-value.Float));
                        break;
                    }

                case OpCode.EqualsStrings:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.String == right.String));
                        break;
                    }

                case OpCode.EqualsBools:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Bool == right.Bool));
                        break;
                    }

                case OpCode.EqualsArrays:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(ReferenceEquals(left.Object, right.Object)));
                        break;
                    }

                case OpCode.NotEqualIntInt:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Int != right.Int));
                        break;
                    }

                case OpCode.NotEqualNumbers:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        double a = instruction.A == 1 ? left.Int : left.Float;
                        double b = instruction.B == 1 ? right.Int : right.Float;
                        stack.Push(new(a != b));
                        break;
                    }

                case OpCode.NotEqualStrings:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.String != right.String));
                        break;
                    }

                case OpCode.NotEqualBools:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.Bool != right.Bool));
                        break;
                    }

                case OpCode.NotEqualArrays:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(!ReferenceEquals(left.Object, right.Object)));
                        break;
                    }

                case OpCode.Concat:
                    {
                        Value right = stack.Pop();
                        Value left = stack.Pop();
                        stack.Push(new(left.String + right.String));
                        break;
                    }

                case OpCode.FlipBool:
                    {
                        Value value = stack.Pop();
                        stack.Push(new(!value.Bool));
                        break;
                    }

                case OpCode.ArrayIndexGet:
                    {
                        int index = stack.Pop().AsInt32();
                        var array = stack.Pop().Array;
                        stack.Push(array[index]);
                        break;
                    }

                case OpCode.StringIndexGet:
                    {
                        int index = stack.Pop().AsInt32();
                        var str = stack.Pop().String;
                        stack.Push(new(str[index].ToString()));
                        break;
                    }

                case OpCode.ArrayIndexSet:
                    {
                        Value value = stack.Pop();
                        int index = stack.Pop().AsInt32();
                        var array = stack.Pop().Array;
                        array[index] = value;
                        stack.Push(value);
                        break;
                    }

                case OpCode.ClassGetField:
                    {
                        Value @class = stack.Pop();
                        int index = instruction.A;
                        stack.Push(@class.Array[index]);
                        break;
                    }

                case OpCode.ClassSetField:
                    {
                        Value value = stack.Pop();
                        Value @class = stack.Pop();
                        int index = instruction.A;
                        @class.Array[index] = value;
                        stack.Push(value);
                        break;
                    }

                case OpCode.JumpIfFalse:
                    {
                        Value condition = stack.Peek();
                        if (!condition.Bool)
                            frame.Ip = instruction.A;
                        break;
                    }

                case OpCode.JumpIfTrue:
                    {
                        Value condition = stack.Peek();
                        if (condition.Bool)
                            frame.Ip = instruction.A;
                        break;
                    }

                case OpCode.JumpIfFalsePop:
                    {
                        Value condition = stack.Pop();
                        if (!condition.Bool)
                            frame.Ip = instruction.A;
                        break;
                    }

                case OpCode.JumpIfTruePop:
                    {
                        Value condition = stack.Pop();
                        if (condition.Bool)
                            frame.Ip = instruction.A;
                        break;
                    }

                case OpCode.Jump:
                    {
                        frame.Ip = instruction.A;
                        break;
                    }

                case OpCode.Pop:
                    {
                        stack.Pop();
                        break;
                    }

                case OpCode.Call:
                    {
                        int argCount = instruction.A;
                        var args = new Value[argCount];

                        for (int i = argCount - 1; i >= 0; i--)
                            args[i] = stack.Pop();

                        Value target = stack.Pop();

                        if (target.Object is Native native)
                        {
                            stack.Push(native.NativeDelegate(native.TypeSymbols, args));
                            break;
                        }

                        var function = target.Function;
                        var callFrame = new CallFrame(function);

                        for (int i = 0; i < argCount; i++)
                            callFrame.Locals[i] = args[i];

                        callFrames.Push(callFrame);
                        break;
                    }

                case OpCode.Ret:
                    {
                        callFrames.Pop();
                        if (callFrames.Count == 0)
                            return stack.Pop();
                        break;
                    }
            }
        }
    }
}
