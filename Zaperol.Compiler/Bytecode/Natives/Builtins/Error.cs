class ErrorBuiltins : IBuiltin
{
    public void Register(Dictionary<string, Value> globals)
    {
        globals["try"] = new Native(
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