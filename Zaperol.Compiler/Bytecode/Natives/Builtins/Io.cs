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
    }
}