class ErrorBuiltins : IBuiltin, IClass
{
    public UnBoundClassStmt[] Unbound => [new(
        "TryResult",
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
            new UnboundVarStmt(
                "error",
                new PrimitiveParserSymbol("string", Position.Protocal),
                new UnboundDefaultExpr(
                    new PrimitiveParserSymbol("string", Position.Protocal),
                    Position.Protocal
                ),
                Position.Protocal
            ),
            new UnboundVarStmt(
                "errorType",
                new PrimitiveParserSymbol("string", Position.Protocal),
                new UnboundDefaultExpr(
                    new PrimitiveParserSymbol("string", Position.Protocal),
                    Position.Protocal
                ),
                Position.Protocal
            )
        ],
        Position.Protocal
    )];

    public BoundClassStmt[] Bound => [new(
        "TryResult",
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
        new BoundVarStmt(
            "error",
            new StringSymbol(),
            new BoundDefaultExpr(
                new StringSymbol(),
                Position.Protocal
            ),
            Position.Protocal
        ),
        new BoundVarStmt(
            "errorType",
            new StringSymbol(),
            new BoundDefaultExpr(
                new StringSymbol(),
                Position.Protocal
            ),
            Position.Protocal
        )
        ],
        [],
        Position.Protocal
    )];

    public void Register(Dictionary<string, Value> globals)
    {
        globals["try"] = new Native(
            new FunctionSymbol(
                [
                    new FunctionSymbol(
                        [],
                        new TypeParameterSymbol("T")
                    )
                ],
                new ClassSymbol(
                    "TryResult",
                    [new TypeParameterSymbol("T")]
                )
            ),
            ["T"],
            (typeArgs, args) =>
            {
                try
                {
                    Value callback = args[0];
                    Value result = default;

                    if (callback.Object is Native native)
                        result = native.NativeDelegate(typeArgs, []);
                    else
                        result = Vm.ExecuteAsync(callback.Function)
                            .GetAwaiter()
                            .GetResult();

                    return new Value(
                    [
                        result,
                        new Value(true),
                        new Value(""),
                        new Value("")
                    ]);
                }
                catch (Exception e)
                {
                    return new Value(
                    [
                        Value.DefaultValue(typeArgs[0]),
                        new Value(false),
                        new Value(e.Message),
                        new Value(e.GetType().Name)
                    ]);
                }
            },
            []
        ).ToValue();
    }
}