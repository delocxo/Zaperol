class IoBuiltins : IBuiltin
{
    public void Register(Dictionary<string, Value> globals)
    {
        globals["ioPrint"] = new Native(
            new([new TypeParameterSymbol("T")],
            new TypeParameterSymbol("T")),
            ["T"],
            (typeArgs, args) =>
            {
                string value = args[0].ToString();
                Console.Write(value);
                return args[0];
            },
            []
        ).ToValue();

        globals["ioPrintln"] = new Native(
            new([new TypeParameterSymbol("T")],
            new TypeParameterSymbol("T")),
            ["T"],
            (typeArgs, args) =>
            {
                string value = args[0].ToString();
                Console.WriteLine(value);
                return args[0];
            },
            []
        ).ToValue();

        globals["ioEPrint"] = new Native(
            new([new TypeParameterSymbol("T")],
            new TypeParameterSymbol("T")),
            ["T"],
            (typeArgs, args) =>
            {
                string value = args[0].ToString();
                Console.Error.Write(value);
                return args[0];
            },
            []
        ).ToValue();

        globals["ioEPrintln"] = new Native(
            new([new TypeParameterSymbol("T")],
            new TypeParameterSymbol("T")),
            ["T"],
            (typeArgs, args) =>
            {
                string value = args[0].ToString();
                Console.Error.WriteLine(value);
                return args[0];
            },
            []
        ).ToValue();

        globals["ioInput"] = new Native(
            new(
                [new StringSymbol()],
                new StringSymbol()
            ),
            [],
            (typeArgs, args) =>
            {
                string prompt = args[0].String;
                Console.Write(prompt);
                string input = Console.ReadLine() ?? "";
                return new(input);
            },
            []
        ).ToValue();

        globals["ioClear"] = new Native(
            new(
                [new StringSymbol()],
                new IntSymbol()
            ),
            [],
            (typeArgs, args) =>
            {
                Console.Clear();
                return new(0);
            },
            []
        ).ToValue();
    }
}