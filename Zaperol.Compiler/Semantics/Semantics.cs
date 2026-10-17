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
    HashSet<string> _names = [];
    Dictionary<string, UnBoundClassStmt> _classes = new();

    int _loopDepth = 0;
    int _ifDepth = 0;
    int _functionDepth = 0;

    bool AtTopLevel => _loopDepth == 0 && _ifDepth == 0 && _functionDepth == 0;

    public List<Stmt> Check(List<Stmt> stmts)
    {
        List<Stmt> newStmts = [];
        foreach (Stmt stmt in stmts)
            if (stmt is UnBoundClassStmt unBoundClassStmt)
            {
                AddName(unBoundClassStmt.Name, "class", unBoundClassStmt.Position);
                _classes[unBoundClassStmt.Name] = unBoundClassStmt;
            }
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
                if (!AtTopLevel)
                    throw new Error($"Functions can only be at the top level", unBoundFunctionStmt.Position);

                AddName(unBoundFunctionStmt.Name, "function", unBoundFunctionStmt.Position);
                return CheckFunction(unBoundFunctionStmt);

            case UnBoundClassStmt unBoundClassStmt:
                if (!AtTopLevel)
                    throw new Error($"Classes can only be at the top level", unBoundClassStmt.Position);
                return CheckClass(unBoundClassStmt);

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

        AddGenerics(generics, unBoundFunctionStmt.Position);

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

    BoundClassStmt CheckClass(UnBoundClassStmt unBoundClassStmt)
    {
        HashSet<string> generics = [];
        foreach (string generic in unBoundClassStmt.Generics)
            if (!generics.Add(generic))
                throw new Error($"'{generic}' is a duplicate function generic", unBoundClassStmt.Position);

        AddGenerics(generics, unBoundClassStmt.Position);

        List<BoundVarStmt> fields = [];
        HashSet<string> fieldsHashSet = [];

        foreach (Stmt stmt in unBoundClassStmt.Body)
        {
            if (stmt is not UnboundVarStmt var)
                throw new Error($"Only var statements are allowed inside classes", stmt.Position);

            if (!fieldsHashSet.Add(var.Name))
                throw new Error($"'{var.Name}' is a duplicate field", var.Position);

            fields.Add((BoundVarStmt)CheckStmt(var));
        }

        _genericsStack.Pop();

        return new BoundClassStmt(
            unBoundClassStmt.Name,
            unBoundClassStmt.Generics,
            fields,
            unBoundClassStmt.Position
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

            case IndexExpr indexExpr:
                return new IndexExpr(
                    CheckExpr(indexExpr.Target),
                    CheckExpr(indexExpr.Index),
                    indexExpr.Position
                );

            case IndexSetExpr indexSetExpr:
                return new IndexSetExpr(
                    (IndexExpr)CheckExpr(indexSetExpr.IndexExpr),
                    CheckExpr(indexSetExpr.Value)
                );

            case MemberExpr memberExpr:
                return new MemberExpr(
                    CheckExpr(memberExpr.Target),
                    memberExpr.MemberName,
                    memberExpr.Position
                );

            case MemberSetExpr memberSetExpr:
                return new MemberSetExpr(
                    (MemberExpr)CheckExpr(memberSetExpr.MemberExpr),
                    CheckExpr(memberSetExpr.Value)
                );

            case UnboundDefaultExpr defaultExpr:
                return new BoundDefaultExpr(
                    ParserSymbolToTypeSymbol(defaultExpr.Type),
                    defaultExpr.Position
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

                    if (TypeRegistery.TryGetValue(primitive.TypeName, out var callback))
                        return callback();

                    if (_classes.TryGetValue(primitive.TypeName, out var classStmt))
                    {
                        if (classStmt.Generics.Count != 0)
                            throw new Error($"Class '{primitive.TypeName}' requires {classStmt.Generics.Count} type arguments", primitive.Position);

                        return new ClassSymbol(primitive.TypeName, []);
                    }

                    throw new Error($"'{primitive.TypeName}' is not a valid type", primitive.Position);
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

            case GenericParserSymbol generic:
                {
                    if (!_classes.TryGetValue(generic.TypeName, out var classStmt))
                        throw new Error(
                           $"'{generic.TypeName}' is not a valid class type",
                           generic.Position
                        );

                    if (generic.Generics.Count != classStmt.Generics.Count)
                        throw new Error(
                            $"Class '{generic.TypeName}' expects {classStmt.Generics.Count} type argument(s), got {generic.Generics.Count}",
                            generic.Position
                        );

                    var arguments = generic.Generics
                        .Select(ParserSymbolToTypeSymbol)
                        .ToList();

                    return new ClassSymbol(generic.TypeName, arguments);
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

    void AddName(string name, string type, Position position)
    {
        if (!_names.Add(name))
            throw new Error($"'{name}' is already an existing {type}", position);
    }

    void AddGenerics(HashSet<string> generics, Position position)
    {
        HashSet<string> newGenerics = [.. generics];
        foreach (var existingGenerics in _genericsStack)
        {
            foreach (string generic in existingGenerics)
                if (!newGenerics.Add(generic))
                    throw new Error($"'{generic}' is a duplicate generic parameter", position);
        }
        _genericsStack.Push(generics);
    }
}