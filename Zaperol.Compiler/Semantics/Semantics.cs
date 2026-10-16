using System.Diagnostics;

class Semantics
{
    public static Dictionary<string, Func<TypeSymbol>> TypeRegistery = new()
    {
        ["int"] = () => new IntSymbol(),
        ["float"] = () => new FloatSymbol(),
        ["string"] = () => new StringSymbol(),
        ["bool"] = () => new BoolSymbol()
    };

    static Stack<HashSet<string>> _genericsStack = [];

    int _loopDepth = 0;
    int _ifDepth = 0;
    int _functionDepth = 0;

    public List<Stmt> Check(List<Stmt> stmts)
    {
        List<Stmt> newStmts = [];
        foreach (Stmt stmt in stmts)
        {
            newStmts.Add(CheckStmt(stmt));
        }
        return newStmts;
    }

    Stmt CheckStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case UnboundVarStmt unboundVarStmt:
                {
                    TypeSymbol symbol = ParserSymbolToTypeSymbol(unboundVarStmt.ParserSymbol);
                    Expr expr = CheckExpr(unboundVarStmt.Expr);
                    var bound = new BoundVarStmt(
                        unboundVarStmt.Name,
                        symbol,
                        expr,
                        unboundVarStmt.Position
                    );
                    return bound;
                }

            case InferStmt inferStmt:
                {
                    inferStmt.Expr = CheckExpr(inferStmt.Expr);
                    return inferStmt;
                }

            case ExprStmt exprStmt:
                {
                    exprStmt.Expr = CheckExpr(exprStmt.Expr);
                    return exprStmt;
                }

            case ReturnStmt returnStmt:
                {
                    returnStmt.Expr = CheckExpr(returnStmt.Expr);
                    return returnStmt;
                }

            case IfStmt ifStmt:
                {
                    foreach (var branch in ifStmt.IfBranches)
                    {
                        branch.Condition = CheckExpr(branch.Condition);
                        branch.Stmts = CheckWithDepth(branch.Stmts, ref _ifDepth);
                    }
                    if (ifStmt.ElseBody != null)
                        ifStmt.ElseBody = CheckWithDepth(ifStmt.ElseBody, ref _ifDepth);
                    return ifStmt;
                }

            case WhileStmt whileStmt:
                {
                    var branch = whileStmt.IfBranch;
                    branch.Condition = CheckExpr(branch.Condition);
                    branch.Stmts = CheckWithDepth(branch.Stmts, ref _loopDepth);
                    return whileStmt;
                }

            case ControlFlowStmt controlFlowStmt:
                {
                    switch (controlFlowStmt.Type)
                    {
                        case ControlFlowType.Leave:
                            if (_ifDepth <= 0)
                                throw new Error("Cannot use leave outside a if context", controlFlowStmt.Position);
                            break;

                        case ControlFlowType.Break:
                            if (_loopDepth <= 0)
                                throw new Error("Cannot use break outside a loop context", controlFlowStmt.Position);
                            break;

                        case ControlFlowType.Continue:
                            if (_loopDepth <= 0)
                                throw new Error("Cannot use continue outside a loop context", controlFlowStmt.Position);
                            break;
                    }
                    return controlFlowStmt;
                }

            case UnBoundFunctionStmt unBoundFunctionStmt:
                if (_functionDepth > 0 || _ifDepth > 0 || _loopDepth > 0)
                    throw new Error($"Functions can only be at the top level", unBoundFunctionStmt.Position);
                return CheckFunction(unBoundFunctionStmt);

            default:
                throw new UnreachableException();
        }
    }

    BoundFunctionStmt CheckFunction(UnBoundFunctionStmt unBoundFunctionStmt)
    {
        HashSet<string> generics = [];
        foreach (string generic in unBoundFunctionStmt.Generics)
            if (!generics.Add(generic))
                throw new Error($"'{generic}' is a duplicate function generic", unBoundFunctionStmt.Position);

        _genericsStack.Push(generics);

        var returnType = ParserSymbolToTypeSymbol(unBoundFunctionStmt.ReturnType);

        HashSet<string> parameters = [];
        List<BoundFunctionParamater> boundParameters = [];

        foreach (var parameter in unBoundFunctionStmt.Paramaters)
        {
            if (!parameters.Add(parameter.Name))
                throw new Error($"'{parameter.Name}' is a duplicate function parameter", parameter.Position);

            var boundSymbol = ParserSymbolToTypeSymbol(parameter.ParserSymbol);
            boundParameters.Add(new(parameter.Name, boundSymbol));
        }

        List<Stmt> newStmts = CheckWithFunctionDepth(unBoundFunctionStmt.Body);

        _genericsStack.Pop();

        return new BoundFunctionStmt(
            unBoundFunctionStmt.Name,
            newStmts,
            boundParameters,
            unBoundFunctionStmt.Generics,
            returnType,
            unBoundFunctionStmt.Position
        );
    }

    Expr CheckExpr(Expr expr)
    {
        switch (expr)
        {
            case UnaryExpr unaryExpr:
                return new UnaryExpr(
                    CheckExpr(unaryExpr.Right),
                    unaryExpr.Op,
                    unaryExpr.Position
                );

            case BinaryExpr binaryExpr:
                return new BinaryExpr(
                    CheckExpr(binaryExpr.Left),
                    CheckExpr(binaryExpr.Right),
                    binaryExpr.Op,
                    binaryExpr.Position
                );

            case ArrayExpr arrayExpr:
                return new ArrayExpr(
                    arrayExpr.Elements.Select(CheckExpr).ToList(),
                    arrayExpr.Position
                );

            case TypeofExpr typeofExpr:
                return new TypeofExpr(
                    CheckExpr(typeofExpr.Expr),
                    typeofExpr.Position
                );

            case AssignExpr assignExpr:
                return new AssignExpr(
                    assignExpr.Name,
                    CheckExpr(assignExpr.Expr),
                    assignExpr.Position
                );

            case UnBoundGenericNameExpr unBoundGenericNameExpr:
                {
                    var generics = unBoundGenericNameExpr.Generics
                        .Select(ParserSymbolToTypeSymbol)
                        .ToList();
                    return new BoundGenericNameExpr(unBoundGenericNameExpr.Name, generics, unBoundGenericNameExpr.Position);
                }

            case CallExpr callExpr:
                return new CallExpr(
                    CheckExpr(callExpr.Target),
                    callExpr.Arguments
                        .Select(CheckExpr)
                        .ToList(),
                    callExpr.Position
                );

            default:
                return expr;
        }
    }

    TypeSymbol ParserSymbolToTypeSymbol(ParserSymbol parserSymbol)
    {
        switch (parserSymbol)
        {
            case PrimitiveParserSymbol primitive:
                {
                    if (_genericsStack.TryPeek(out var parameterTypes))
                    {
                        if (parameterTypes.Contains(primitive.TypeName))
                            return new TypeParameterSymbol(primitive.TypeName);
                    }
                    if (!TypeRegistery.TryGetValue(primitive.TypeName, out var callback))
                        throw new Error($"'{primitive.TypeName}' is not a valid type", primitive.Position);
                    return callback();
                }

            case ArrayParserSymbol array:
                {
                    TypeSymbol elementType = ParserSymbolToTypeSymbol(array.TypeSymbol);
                    return new ArraySymbol(elementType);
                }

            case FunctionParserSymbol function:
                {
                    var parameters = function.ParameterTypes
                        .Select(ParserSymbolToTypeSymbol)
                        .ToList();
                    var returnType = parameters[^1];
                    parameters.RemoveAt(parameters.Count - 1);
                    return new FunctionSymbol(parameters, returnType);
                }

            default:
                throw new UnreachableException();
        }
    }

    List<Stmt> CheckWithDepth(List<Stmt> stmts, ref int depth)
    {
        depth++;
        try
        {
            return Check(stmts);
        }
        finally
        {
            depth--;
        }
    }

    List<Stmt> CheckWithFunctionDepth(List<Stmt> stmts)
    {
        int prevIfDepth = _ifDepth;
        int prevLoopDepth = _loopDepth;
        _ifDepth = 0;
        _loopDepth = 0;
        _functionDepth++;
        try
        {
            return Check(stmts);
        }
        finally
        {
            _functionDepth--;
            _ifDepth = prevIfDepth;
            _loopDepth = prevLoopDepth;
        }
    }
}