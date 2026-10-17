class Result : IBuiltin, IClass
{
    public UnBoundClassStmt[] Unbound => [new(
        "Result",
        ["T"],
        [
            new UnboundVarStmt(
                "data",
                new PrimitiveParserSymbol("T", Position.Protocal),
                new UnboundDefaultExpr(
                    new PrimitiveParserSymbol("T", Position.Protocal),
                    Position.Protocal
                ),
                Position.Protocal
            ),
            new UnboundVarStmt(
                "success",
                new PrimitiveParserSymbol("bool", Position.Protocal),
                new UnboundDefaultExpr(
                    new PrimitiveParserSymbol("bool", Position.Protocal),
                    Position.Protocal
                ),
                Position.Protocal
            ),
        ],
        Position.Protocal
    )];

    public BoundClassStmt[] Bound => [new(
        "Result",
        ["T"],
        [
            new BoundVarStmt(
            "data",
            new TypeParameterSymbol("T"),
            new BoundDefaultExpr(
                new TypeParameterSymbol("T"),
                Position.Protocal
            ),
            Position.Protocal
        ),
        new BoundVarStmt(
            "success",
            new BoolSymbol(),
            new BoundDefaultExpr(
                new BoolSymbol(),
                Position.Protocal
            ),
            Position.Protocal
        ),
        ],
        [],
        Position.Protocal
    )];

    public static readonly ClassSymbol ResultSymbol = new("Result", [new TypeParameterSymbol("T")]);

    // Dont mind this name...
    public static ClassSymbol ResultSymbolWithTParameter(string typeParameter)
    {
        return ResultSymbol with { TypeArguments = [new TypeParameterSymbol(typeParameter)] };
    }

    public static Value MakeResult(Value value, bool success)
        => new([
            value,
            new(success)
        ]);

    public void Register(Dictionary<string, Value> globals)
    {

    }
}