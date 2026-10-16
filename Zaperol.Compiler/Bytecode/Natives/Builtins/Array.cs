class ArrayBuiltins : IBuiltin
{
    public void Register(Dictionary<string, Value> globals)
    {
        globals["arrayCreate"] = new Native(
            new([new IntSymbol()],
            new ArraySymbol(new TypeParameterSymbol("T"))),
            ["T"],
            (typeArgs, args) =>
            {
                int capacity = args[0].AsInt32();
                Value initial = DefaultValue(typeArgs[0]);
                Value[] array = new Value[capacity];
                Array.Fill(array, initial);
                return new(array);
            },
            []
        ).ToValue();

        globals["arrayLength"] = new Native(
            new(
                [new ArraySymbol(new TypeParameterSymbol("T"))],
                new IntSymbol()
            ),
            ["T"],
            (typeArgs, args) =>
            {
                return new(args[0].Array.Length);
            },
            []
        ).ToValue();

        globals["arrayGet"] = new Native(
            new(
                [new ArraySymbol(new TypeParameterSymbol("T")), new IntSymbol()],
                new TypeParameterSymbol("T")
            ),
            ["T"],
            (typeArgs, args) =>
            {
                Value[] array = args[0].Array;
                int index = args[1].AsInt32();
                return array[index];
            },
            []
        ).ToValue();

        globals["arraySet"] = new Native(
            new(
                [new ArraySymbol(new TypeParameterSymbol("T")), new IntSymbol(), new TypeParameterSymbol("T")],
                new IntSymbol()
            ),
            ["T"],
            (typeArgs, args) =>
            {
                Value[] array = args[0].Array;
                int index = args[1].AsInt32();
                Value value = args[2];
                array[index] = value;
                return new Value(0);
            },
            []
        ).ToValue();

        globals["arrayCopy"] = new Native(
            new(
                [
                    // Source
                    new ArraySymbol(new TypeParameterSymbol("T")), 
                    // Source Index
                    new IntSymbol(),
                    // Destination 
                    new ArraySymbol(new TypeParameterSymbol("T")), 
                    // Destination Index
                    new IntSymbol(),
                    // COunt
                    new IntSymbol(),
                ],
                new IntSymbol()
            ),
            ["T"],
            (typeArgs, args) =>
            {
                Value[] source = args[0].Array;
                int sourceIndex = args[1].AsInt32();
                Value[] dest = args[2].Array;
                int destIndex = args[3].AsInt32();
                int count = args[4].AsInt32();
                Array.Copy(source, sourceIndex, dest, destIndex, count);
                return new Value(0);
            },
            []
        ).ToValue();

        globals["arrayClear"] = new Native(
            new(
                [new ArraySymbol(new TypeParameterSymbol("T"))],
                new IntSymbol()
            ),
            ["T"],
            (typeArgs, args) =>
            {
                Value[] array = args[0].Array;
                Array.Fill(array, DefaultValue(typeArgs[0]));
                return new Value(0);
            },
            []
        ).ToValue();
    }

    Value DefaultValue(TypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            IntSymbol => new Value(0L),
            FloatSymbol => new Value(0.0),
            BoolSymbol => new Value(false),

            StringSymbol or ArraySymbol or FunctionSymbol =>
                new Value((object?)null),

            _ => throw new InvalidOperationException(
                $"Cannot create a default value for {typeSymbol}"
            )
        };
    }
}