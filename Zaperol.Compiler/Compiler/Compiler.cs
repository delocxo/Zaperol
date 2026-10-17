class Compiler
{
    public Chunk Chunk { get; } = new();
    public LocalResolver LocalResolver = new();
    public TypeSymbol ReturnType { get; set; }

    Dictionary<string, bool> _compiledFiles = new();
    Stack<IfContext> _ifContexts = [];
    Stack<LoopContext> _loopContexts = [];
    Dictionary<string, BoundFunctionStmt> _functions;
    Dictionary<string, (FunctionSymbol Symbol, Chunk Chunk)> _compiledFunctions;
    Dictionary<string, TypeSymbol> _genericArguments;
    Dictionary<string, BoundClassStmt> _classes;

    public Compiler(
        TypeSymbol returnType,
        Dictionary<string,
        BoundFunctionStmt> functions,
        Dictionary<string, TypeSymbol> genericArguments,
        Dictionary<string, (FunctionSymbol Symbol, Chunk Chunk)> compiledFunctions,
        Dictionary<string, BoundClassStmt> classes)
    {
        ReturnType = returnType;
        _functions = functions;
        _genericArguments = genericArguments;
        _compiledFunctions = compiledFunctions;
        _classes = classes;
    }

    public void CompileFile(string path, Position? position)
    {
        // try
        // {
        if (!File.Exists(path))
            throw new Error($"File '{path}' does not exist", position);

        path = Path.GetFullPath(path);

        if (_compiledFiles.TryGetValue(path, out bool finished))
        {
            if (!finished)
                throw new Error($"Circular import detected: '{path}'", position);
            return;
        }

        _compiledFiles[path] = false;

        string source = File.ReadAllText(path);
        Lexer lexer = new(source, path);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var ast = parser.Parse();
        Semantics semantics = new();
        var newAst = semantics.Check(ast);

        foreach (Stmt stmt in newAst)
        {
            if (stmt is BoundFunctionStmt function)
                _functions[function.Name] = function;

            else if (stmt is BoundClassStmt boundClassStmt)
                _classes[boundClassStmt.Name] = boundClassStmt;
        }

        CompileStmts(newAst);

        _compiledFiles[path] = true;
        // }
        // catch (Error e)
        // {
        //     e.Exit();
        // }
        // catch (Exception e)
        // {
        //     new Error(e.Message, null).Exit();
        // }
    }

    public void CompileStmts(List<Stmt> stmts)
    {
        foreach (Stmt stmt in stmts)
            CompileStmt(stmt);
    }

    public void RequireReturn(Position? position)
    {
        if (Chunk.Instructions.Count == 0 || Chunk.Instructions[^1].OpCode != OpCode.Ret)
            throw new Error("Expected a final return statment", position);
    }

    void CompileStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case BoundVarStmt boundVarStmt:
                {
                    TypeSymbol value = CompileExpr(boundVarStmt.Expr);
                    Local local = LocalResolver.Declare(
                        boundVarStmt.Name,
                        ref Chunk.LocalCount,
                        ResolveType(boundVarStmt.TypeSymbol),
                        boundVarStmt.Position
                    );
                    LocalResolver.Assign(boundVarStmt.Name, value, boundVarStmt.Position);
                    Chunk.AddInstruction(local.Slot, OpCode.StoreLocal, boundVarStmt.Position);
                    break;
                }

            case InferStmt inferStmt:
                {
                    TypeSymbol value = CompileExpr(inferStmt.Expr);
                    if (value is EmptyArraySymbol)
                        throw new Error($"'{inferStmt.Name}' cannot infer from an empty array", inferStmt.Position);
                    Local local = LocalResolver.Declare(
                        inferStmt.Name,
                        ref Chunk.LocalCount,
                        value,
                        inferStmt.Position
                    );
                    Chunk.AddInstruction(local.Slot, OpCode.StoreLocal, inferStmt.Position);
                    break;
                }

            case ExprStmt exprStmt:
                {
                    CompileExpr(exprStmt.Expr);
                    Chunk.AddInstruction(OpCode.Pop, exprStmt.Position);
                    break;
                }

            case ReturnStmt returnStmt:
                {
                    var value = CompileExpr(returnStmt.Expr);
                    if (ReturnType is not InvalidSymbol &&
                    !TypeSymbol.TypeCheck(value, ReturnType))
                    {
                        throw new Error($"Return type does not match type {value}", returnStmt.Position);
                    }
                    Chunk.AddInstruction(OpCode.Ret, returnStmt.Position);
                    break;
                }

            case IfStmt ifStmt:
                CompileIf(ifStmt);
                break;

            case ControlFlowStmt controlFlowStmt:
                CompileControlFlow(controlFlowStmt);
                break;

            case WhileStmt whileStmt:
                CompileWhile(whileStmt);
                break;
        }
    }

    void CompileIf(IfStmt ifStmt)
    {
        List<int> endJumps = [];

        foreach (var branch in ifStmt.IfBranches)
        {
            CompileExpr(branch.Condition)
                .Expect(TypeSymbol.BoolSymbol, branch.Position);

            int falseJump = Chunk.AddInstruction(OpCode.JumpIfFalsePop, branch.Position);

            _ifContexts.Push(new());

            LocalResolver.BeginScope();

            CompileStmts(branch.Stmts);

            LocalResolver.EndScope();

            endJumps.AddRange(_ifContexts.Pop().Leaves);

            int endJump = Chunk.AddInstruction(OpCode.Jump, branch.Position);

            endJumps.Add(endJump);

            Chunk.PatchA(falseJump);
        }

        if (ifStmt.ElseBody != null)
        {
            _ifContexts.Push(new());

            LocalResolver.BeginScope();

            CompileStmts(ifStmt.ElseBody);

            LocalResolver.EndScope();

            endJumps.AddRange(_ifContexts.Pop().Leaves);
        }

        foreach (int endJump in endJumps)
            Chunk.PatchA(endJump);
    }

    void CompileWhile(WhileStmt whileStmt)
    {
        var branch = whileStmt.IfBranch;

        int loopStart = Chunk.Instructions.Count;

        CompileExpr(branch.Condition)
            .Expect(TypeSymbol.BoolSymbol, branch.Position);

        int falseJump = Chunk.AddInstruction(OpCode.JumpIfFalsePop, whileStmt.Position);

        _loopContexts.Push(new());

        LocalResolver.BeginScope();

        CompileStmts(branch.Stmts);

        LocalResolver.EndScope();

        Chunk.AddInstruction(loopStart, OpCode.Jump, whileStmt.Position);

        Chunk.PatchA(falseJump);

        var context = _loopContexts.Pop();

        foreach (int breakJump in context.Breaks)
            Chunk.PatchA(breakJump);

        foreach (int continueJump in context.Continues)
            Chunk.PatchA(continueJump, loopStart);
    }

    void CompileControlFlow(ControlFlowStmt controlFlowStmt)
    {
        switch (controlFlowStmt.Type)
        {
            case ControlFlowType.Leave:
                {
                    var context = _ifContexts.Peek();
                    int jump = Chunk.AddInstruction(OpCode.Jump, controlFlowStmt.Position);
                    context.Leaves.Add(jump);
                    break;
                }

            case ControlFlowType.Break:
                {
                    var context = _loopContexts.Peek();
                    int jump = Chunk.AddInstruction(OpCode.Jump, controlFlowStmt.Position);
                    context.Breaks.Add(jump);
                    break;
                }

            case ControlFlowType.Continue:
                {
                    var context = _loopContexts.Peek();
                    int jump = Chunk.AddInstruction(OpCode.Jump, controlFlowStmt.Position);
                    context.Continues.Add(jump);
                    break;
                }
        }
    }

    (FunctionSymbol Symbol, Chunk Chunk) CompileFunction(
        BoundFunctionStmt function,
        List<TypeSymbol> typeArguments,
        Position position,
        ClassSymbol? owner = null)
    {
        if (typeArguments.Count != function.Generics.Count)
            throw new Error(
                $"Function '{function.Name}' expects {function.Generics.Count} generic(s), got {typeArguments.Count}",
                position);

        string key = $"{function.Name}<{string.Join(
            ", ",
            typeArguments.Select(x => x
        ))}>";

        if (owner != null)
            key = $"{owner}.{key}";


        if (_compiledFunctions.TryGetValue(key, out var compiled))
            return compiled;

        Dictionary<string, TypeSymbol> genericArguments = [];

        if (owner != null)
        {
            var def = _classes[owner.ClassName];
            foreach (var pair in def.Generics.Zip(owner.TypeArguments))
                genericArguments.Add(pair.First, pair.Second);
        }

        foreach (var pair in function.Generics.Zip(typeArguments))
            genericArguments.Add(pair.First, pair.Second);

        Compiler compiler = new Compiler(
            function.ReturnType,
            _functions,
            genericArguments,
            _compiledFunctions,
            _classes
        );

        compiler.ReturnType = compiler.ResolveType(function.ReturnType);

        List<TypeSymbol> resolvedParameterTypes = [];

        foreach (var parameter in function.Paramaters)
        {
            var parameterType = compiler.ResolveType(parameter.TypeSymbol);
            compiler.LocalResolver.Declare(
                parameter.Name,
                ref compiler.Chunk.LocalCount,
                parameterType,
                function.Position
            );
            resolvedParameterTypes.Add(parameterType);
        }

        if (owner != null)
        {
            if (function.Paramaters.Count == 0)
                throw new Error(
                    "Methods must declare 'this' as their first parameter",
                    function.Position
                );

            if (function.Paramaters[0].Name != "this")
                throw new Error(
                    "The first method parameter must be named 'this'",
                    function.Position
                );

            resolvedParameterTypes[0].Expect(owner, function.Position);
        }

        var functionSymbol = new FunctionSymbol(resolvedParameterTypes, compiler.ReturnType);

        _compiledFunctions[key] = (functionSymbol, compiler.Chunk);

        compiler.CompileStmts(function.Body);
        compiler.RequireReturn(function.Position);
        compiler.Chunk.ConvertListsToArrays();

        return _compiledFunctions[key];
    }

    (FunctionSymbol Symbol, Chunk Chunk) CompileClass(BoundClassStmt classStmt, List<TypeSymbol> typeArguments, Position position)
    {
        if (classStmt.Generics.Count != typeArguments.Count)
            throw new Error(
                $"Class '{classStmt.Name}' expects {classStmt.Generics.Count} generic(s), got {typeArguments.Count}",
                position);

        string key = $"{classStmt.Name}<{string.Join(
            ", ",
            typeArguments.Select(x => x
        ))}>";

        if (_compiledFunctions.TryGetValue(key, out var compiled))
            return compiled;

        Dictionary<string, TypeSymbol> genericArguments = classStmt.Generics
            .Zip(typeArguments)
            .ToDictionary(key => key.First, value => value.Second);

        var classSymbol = new ClassSymbol(classStmt.Name, typeArguments);

        Compiler compiler = new Compiler(
            classSymbol,
            _functions,
            genericArguments,
            _compiledFunctions,
            _classes
        );

        var functionSymbol = new FunctionSymbol([], classSymbol);

        _compiledFunctions[key] = (functionSymbol, compiler.Chunk);

        foreach (var field in classStmt.Fields)
            compiler.CompileStmt(field);

        foreach (var field in classStmt.Fields)
        {
            Local local = compiler.LocalResolver.Resolve(field.Name, field.Position);
            compiler.Chunk.AddInstruction(local.Slot, OpCode.LoadLocal, field.Position);
        }

        compiler.Chunk.AddInstruction(classStmt.Fields.Count, OpCode.MakeArray, position);

        compiler.Chunk.AddInstruction(OpCode.Ret, position);

        compiler.Chunk.ConvertListsToArrays();

        return (functionSymbol, compiler.Chunk);
    }

    TypeSymbol CompileExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                {
                    Chunk.AddLoadConst(new Value(intExpr.Value), intExpr.Position);
                    return new IntSymbol();
                }

            case FloatExpr floatExpr:
                {
                    Chunk.AddLoadConst(new Value(floatExpr.Value), floatExpr.Position);
                    return new FloatSymbol();
                }

            case StringExpr stringExpr:
                {
                    Chunk.AddLoadConst(new Value(stringExpr.Value), stringExpr.Position);
                    return new StringSymbol();
                }

            case BoolExpr boolExpr:
                {
                    Chunk.AddLoadConst(new Value(boolExpr.Value), boolExpr.Position);
                    return new BoolSymbol();
                }

            case NameExpr nameExpr:
                {
                    if (_functions.TryGetValue(nameExpr.Name, out var function))
                    {
                        var compiled = CompileFunction(function, [], nameExpr.Position);
                        int constant = Chunk.AddConst(new Value(compiled.Chunk));
                        Chunk.AddInstruction(constant, OpCode.LoadConst, nameExpr.Position);
                        return compiled.Symbol;
                    }
                    else if (_classes.TryGetValue(nameExpr.Name, out var classStmt))
                    {
                        var compiled = CompileClass(classStmt, [], nameExpr.Position);
                        int constant = Chunk.AddConst(new Value(compiled.Chunk));
                        Chunk.AddInstruction(constant, OpCode.LoadConst, nameExpr.Position);
                        return compiled.Symbol;
                    }
                    else if (LocalResolver.TryResolve(nameExpr.Name, out var local))
                    {
                        Chunk.AddInstruction(local!.Slot, OpCode.LoadLocal, nameExpr.Position);
                        return local.TypeSymbol;
                    }
                    else if (Globals.Values.TryGetValue(nameExpr.Name, out var value) && value.Object is Native native)
                    {
                        var bound = BindNative(native, [], nameExpr.Position);
                        Chunk.AddLoadConst(bound.ToValue(), nameExpr.Position);
                        return bound.FunctionSymbol;
                    }

                    throw new Error($"'{nameExpr.Name}' does not exist in this scope", nameExpr.Position);
                }

            case UnaryExpr unaryExpr:
                return CompilerUnary(unaryExpr);

            case BinaryExpr binaryExpr:
                return CompileBinary(binaryExpr);

            case ArrayExpr arrayExpr:
                return CompileArray(arrayExpr);

            case TypeofExpr typeofExpr:
                {
                    var symbol = CompileExpr(typeofExpr.Expr);

                    Chunk.AddInstruction(OpCode.Pop, typeofExpr.Position);

                    Chunk.AddLoadConst(new Value(symbol.Name), typeofExpr.Position);

                    return new StringSymbol();
                }

            case AssignExpr assignExpr:
                {
                    var value = CompileExpr(assignExpr.Expr);

                    int valueLocal = Chunk.GetCompilerLocal();

                    Chunk.AddInstruction(valueLocal, OpCode.StoreLocal, assignExpr.Position);

                    Chunk.AddInstruction(valueLocal, OpCode.LoadLocal, assignExpr.Position);

                    Local local = LocalResolver.Assign(assignExpr.Name, value, assignExpr.Position);

                    Chunk.AddInstruction(local.Slot, OpCode.StoreLocal, assignExpr.Position);

                    Chunk.AddInstruction(valueLocal, OpCode.LoadLocal, assignExpr.Position);

                    return value;
                }

            case BoundGenericNameExpr boundNameExpr:
                {
                    var typeArguments = boundNameExpr.Generics
                            .Select(ResolveType)
                            .ToList();

                    if (_functions.TryGetValue(boundNameExpr.Name, out var function))
                    {
                        var compiled = CompileFunction(function, typeArguments, boundNameExpr.Position);
                        int constant = Chunk.AddConst(new Value(compiled.Chunk));
                        Chunk.AddInstruction(constant, OpCode.LoadConst, boundNameExpr.Position);
                        return compiled.Symbol;
                    }
                    else if (_classes.TryGetValue(boundNameExpr.Name, out var classStmt))
                    {
                        var compiled = CompileClass(classStmt, typeArguments, boundNameExpr.Position);
                        int constant = Chunk.AddConst(new Value(compiled.Chunk));
                        Chunk.AddInstruction(constant, OpCode.LoadConst, boundNameExpr.Position);
                        return compiled.Symbol;
                    }
                    else if (Globals.Values.TryGetValue(boundNameExpr.Name, out var value) && value.Object is Native native)
                    {
                        var bound = BindNative(native, typeArguments.ToArray(), boundNameExpr.Position);
                        Chunk.AddLoadConst(bound.ToValue(), boundNameExpr.Position);
                        return bound.FunctionSymbol;
                    }

                    throw new Error($"'{boundNameExpr.Name}' does not exist in this scope", boundNameExpr.Position);
                }

            case CallExpr callExpr:
                {
                    if (callExpr.Target is MemberExpr memberExpr)
                    {
                        var memberTarget = CompileExpr(memberExpr.Target);
                        if (memberTarget is ClassSymbol classSymbol)
                        {
                            int receiverLocal = Chunk.GetCompilerLocal();
                            Chunk.AddInstruction(receiverLocal, OpCode.StoreLocal, memberExpr.Position);

                            string methodName = "";
                            List<TypeSymbol> typeArguments = [];

                            if (memberExpr.Member is NameExpr nameExpr)
                            {
                                methodName = nameExpr.Name;
                                typeArguments = [];
                            }
                            else
                            {
                                var genericNameExpr = (BoundGenericNameExpr)memberExpr.Member;
                                methodName = genericNameExpr.Name;
                                typeArguments = genericNameExpr.Generics
                                    .Select(ResolveType)
                                    .ToList();
                            }

                            var def = _classes[classSymbol.ClassName];
                            int index = def.Methods.FindIndex(x => x.Name == methodName);

                            if (index == -1)
                                throw new Error(
                                    $"Class '{classSymbol.ClassName}' does not contain method '{methodName}'",
                                    memberExpr.Position
                                );

                            var method = def.Methods[index];

                            var compiled = CompileFunction(
                                method,
                                typeArguments,
                                memberExpr.Position,
                                classSymbol
                            );

                            Chunk.AddLoadConst(new(compiled.Chunk), memberExpr.Position);

                            Chunk.AddInstruction(receiverLocal, OpCode.LoadLocal, memberExpr.Position);

                            int expectedCount = compiled.Symbol.ParameterSymbols.Count - 1;

                            if (callExpr.Arguments.Count != expectedCount)
                                throw new Error(
                                    $"Method expects {expectedCount} argument(s), got {callExpr.Arguments.Count}",
                                    callExpr.Position
                                );

                            var methodArgs = callExpr.Arguments
                                .Select(CompileExpr)
                                .ToList();

                            for (int i = 0; i < methodArgs.Count; i++)
                            {
                                methodArgs[i].Expect(
                                    compiled.Symbol.ParameterSymbols[i + 1],
                                    callExpr.Arguments[i].Position
                                );
                            }

                            Chunk.AddInstruction(methodArgs.Count + 1, OpCode.Call, callExpr.Position);

                            return compiled.Symbol.ReturnType;
                        }
                    }
                    var target = CompileExpr(callExpr.Target);
                    var arguments = callExpr.Arguments
                        .Select(CompileExpr)
                        .ToList();

                    if (target is not FunctionSymbol functionSymbol)
                        throw new Error($"Type {target} is not callable", callExpr.Position);

                    if (callExpr.Arguments.Count != functionSymbol.ParameterSymbols.Count)
                        throw new Error(
                                $"Function expects {functionSymbol.ParameterSymbols.Count} argument(s), got {arguments.Count}",
                                callExpr.Position
                            );

                    foreach (var pair in arguments.Zip(functionSymbol.ParameterSymbols))
                        if (!TypeSymbol.TypeCheck(pair.First, pair.Second))
                            throw new Error($"Argument type does not match function parameter type", callExpr.Position);

                    Chunk.AddInstruction(arguments.Count, OpCode.Call, callExpr.Position);

                    return functionSymbol.ReturnType;
                }

            case IndexExpr indexExpr:
                {
                    var target = CompileExpr(indexExpr.Target);
                    var index = CompileExpr(indexExpr.Index);

                    if (target is ArraySymbol arraySymbol)
                    {
                        index.Expect(new IntSymbol(), indexExpr.Position);
                        Chunk.AddInstruction(OpCode.ArrayIndexGet, indexExpr.Position);
                        return arraySymbol.ElementType;
                    }
                    else if (target is StringSymbol stringSymbol)
                    {
                        index.Expect(new IntSymbol(), indexExpr.Position);
                        Chunk.AddInstruction(OpCode.StringIndexGet, indexExpr.Position);
                        return stringSymbol;
                    }

                    throw new Error($"Type {target} cannot be inded read", indexExpr.Position);
                }

            case IndexSetExpr indexSetExpr:
                {
                    var indexExpr = indexSetExpr.IndexExpr;
                    var target = CompileExpr(indexExpr.Target);
                    var index = CompileExpr(indexExpr.Index);
                    var value = CompileExpr(indexSetExpr.Value);

                    if (target is ArraySymbol arraySymbol)
                    {
                        index.Expect(new IntSymbol(), indexExpr.Position);
                        value.Expect(arraySymbol.ElementType, indexSetExpr.Position);
                        Chunk.AddInstruction(OpCode.ArrayIndexSet, indexExpr.Position);
                        return arraySymbol.ElementType;
                    }

                    throw new Error($"Type {target} cannot be index set", indexExpr.Position);
                }

            case MemberExpr memberExpr:
                {
                    var target = CompileExpr(memberExpr.Target);

                    if (target is ClassSymbol classSymbol)
                    {
                        if (memberExpr.Member is not NameExpr nameExpr)
                            throw new Error("Expected a valid member name", memberExpr.Position);

                        var @class = _classes[classSymbol.Name];
                        int index = @class.Fields.FindIndex(x => x.Name == nameExpr.Name);

                        if (index == -1)
                            throw new Error(
                                $"Class '{classSymbol.ClassName}' does not contain field '{nameExpr.Name}'",
                                memberExpr.Position
                            );

                        var field = @class.Fields[index];
                        Chunk.AddInstruction(index, OpCode.ClassGetField, memberExpr.Position);

                        var bindings = @class.Generics
                            .Zip(classSymbol.TypeArguments)
                            .ToDictionary(pair => pair.First, pair => pair.Second);

                        return ResolveType(field.TypeSymbol, bindings);
                    }

                    throw new Error($"Type {target} cannot be member read", memberExpr.Position);
                }

            case MemberSetExpr memberSetExpr:
                {
                    MemberExpr memberExpr = memberSetExpr.MemberExpr;
                    var target = CompileExpr(memberExpr.Target);
                    var value = CompileExpr(memberSetExpr.Value);

                    if (target is ClassSymbol classSymbol)
                    {
                        if (memberExpr.Member is not NameExpr nameExpr)
                            throw new Error("Expected a valid member name", memberExpr.Position);

                        var @class = _classes[classSymbol.Name];
                        int index = @class.Fields.FindIndex(x => x.Name == nameExpr.Name);

                        if (index == -1)
                            throw new Error(
                                $"Class '{classSymbol.ClassName}' does not contain field '{nameExpr.Name}'",
                                memberExpr.Position
                            );

                        var field = @class.Fields[index];

                        var bindings = @class.Generics
                            .Zip(classSymbol.TypeArguments)
                            .ToDictionary(pair => pair.First, pair => pair.Second);

                        var symbol = ResolveType(field.TypeSymbol, bindings);
                        value.Expect(symbol, memberSetExpr.Position);

                        Chunk.AddInstruction(index, OpCode.ClassSetField, memberExpr.Position);

                        return symbol;
                    }

                    throw new Error($"Type {target} cannot be member read", memberExpr.Position);
                }

            case BoundDefaultExpr boundDefaultExpr:
                {
                    var type = ResolveType(boundDefaultExpr.Type);
                    Chunk.AddLoadConst(Value.DefaultValue(type), boundDefaultExpr.Position);
                    return type;
                }

            default:
                throw new Error($"Unsupported expression {expr.GetType().Name}", expr.Position);
        }
    }

    TypeSymbol CompilerUnary(UnaryExpr unaryExpr)
    {
        var right = CompileExpr(unaryExpr.Right);
        if (unaryExpr.Op == TokenType.Sub)
        {
            if (right is IntSymbol)
            {
                Chunk.AddInstruction(OpCode.NegateInt, unaryExpr.Position);
                return right;
            }
            else if (right is FloatSymbol)
            {
                Chunk.AddInstruction(OpCode.NegateFloat, unaryExpr.Position);
                return right;
            }
            throw new Error($"Type {right} cannot be negated", unaryExpr.Position);
        }
        else
        {
            if (right is not BoolSymbol)
                throw new Error($"Type {right} cannot be flipped", unaryExpr.Position);

            Chunk.AddInstruction(OpCode.FlipBool, unaryExpr.Position);
            return right;
        }
    }

    TypeSymbol CompileBinary(BinaryExpr binaryExpr)
    {
        if (binaryExpr.Op == TokenType.And)
            return CompileAnd(binaryExpr);

        else if (binaryExpr.Op == TokenType.Or)
            return CompileOr(binaryExpr);

        Position position = binaryExpr.Position;
        var left = CompileExpr(binaryExpr.Left);
        var right = CompileExpr(binaryExpr.Right);

        if (binaryExpr.Op is TokenType.IsEqual or TokenType.NotEqual)
        {
            if (left is EmptyArraySymbol && right is ArraySymbol)
                left = right;
            else if (left is ArraySymbol && right is EmptyArraySymbol)
                right = left;
        }

        OpCode op;
        TypeSymbol returnType = TypeSymbol.InvalidSymbol;

        switch (binaryExpr.Op)
        {
            case TokenType.Add:
                {
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                        {
                            op = OpCode.AddNumbers;
                            returnType = new FloatSymbol();
                        }
                        else
                        {
                            op = OpCode.AddIntInt;
                            returnType = new IntSymbol();
                        }
                    }
                    else if (left is StringSymbol && right is StringSymbol)
                    {
                        op = OpCode.Concat;
                        returnType = new StringSymbol();
                    }
                    else
                        throw BinaryError(left, right, "+", position);
                    break;
                }

            case TokenType.Sub:
                {
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                        {
                            op = OpCode.SubNumbers;
                            returnType = new FloatSymbol();
                        }
                        else
                        {
                            op = OpCode.SubIntInt;
                            returnType = new IntSymbol();
                        }
                    }
                    else
                        throw BinaryError(left, right, "-", position);
                    break;
                }

            case TokenType.Mul:
                {
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                        {
                            op = OpCode.MulNumbers;
                            returnType = new FloatSymbol();
                        }
                        else
                        {
                            op = OpCode.MulIntInt;
                            returnType = new IntSymbol();
                        }
                    }
                    else
                        throw BinaryError(left, right, "*", position);
                    break;
                }

            case TokenType.Div:
                {
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                        {
                            op = OpCode.DivNumbers;
                            returnType = new FloatSymbol();
                        }
                        else
                        {
                            op = OpCode.DivIntInt;
                            returnType = new IntSymbol();
                        }
                    }
                    else
                        throw BinaryError(left, right, "/", position);
                    break;
                }

            case TokenType.Mod:
                {
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                        {
                            op = OpCode.ModNumbers;
                            returnType = new FloatSymbol();
                        }
                        else
                        {
                            op = OpCode.ModIntInt;
                            returnType = new IntSymbol();
                        }
                    }
                    else
                        throw BinaryError(left, right, "%", position);
                    break;
                }

            case TokenType.Less:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.LessNumbers;
                        else
                            op = OpCode.LessIntInt;
                    }
                    else
                        throw BinaryError(left, right, "<", position);
                    break;
                }

            case TokenType.Greater:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.GreaterNumbers;
                        else
                            op = OpCode.GreaterIntInt;
                    }
                    else
                        throw BinaryError(left, right, ">", position);
                    break;
                }

            case TokenType.LessEq:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.LessEqualNumbers;
                        else
                            op = OpCode.LessEqualIntInt;
                    }
                    else
                        throw BinaryError(left, right, "<=", position);
                    break;
                }

            case TokenType.GreaterEq:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.GreaterEqaulNumbers;
                        else
                            op = OpCode.GreaterEqualIntInt;
                    }
                    else
                        throw BinaryError(left, right, ">=", position);
                    break;
                }

            case TokenType.IsEqual:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.EqaulsNumbers;
                        else
                            op = OpCode.EqualsIntInt;
                    }
                    else
                    {
                        op = (left, right) switch
                        {
                            (StringSymbol, StringSymbol) => op = OpCode.EqualsStrings,
                            (BoolSymbol, BoolSymbol) => op = OpCode.EqualsBools,
                            (ArraySymbol, ArraySymbol) => OpCode.EqualsArrays,
                            _ => throw BinaryError(left, right, "==", position)
                        };
                    }

                    break;
                }

            case TokenType.NotEqual:
                {
                    returnType = new BoolSymbol();
                    if (left.IsNumeric() && right.IsNumeric())
                    {
                        if (left is FloatSymbol || right is FloatSymbol)
                            op = OpCode.NotEqualNumbers;
                        else
                            op = OpCode.NotEqualIntInt;
                    }
                    else
                    {
                        op = (left, right) switch
                        {
                            (StringSymbol, StringSymbol) => op = OpCode.NotEqualStrings,
                            (BoolSymbol, BoolSymbol) => op = OpCode.NotEqualBools,
                            (ArraySymbol, ArraySymbol) => OpCode.NotEqualArrays,
                            _ => throw BinaryError(left, right, "!=", position)
                        };
                    }

                    break;
                }
            default:
                throw BinaryError(left, right, binaryExpr.Op.ToString(), position);
        }

        Chunk.AddInstruction(
            left is IntSymbol ? 1 : 0,
            right is IntSymbol ? 1 : 0,
            op,
            position
        );

        return returnType;
    }

    TypeSymbol CompileAnd(BinaryExpr binaryExpr)
    {
        var left = CompileExpr(binaryExpr.Left)
            .Expect(TypeSymbol.BoolSymbol, binaryExpr.Position);

        int end = Chunk.AddInstruction(OpCode.JumpIfFalse, binaryExpr.Position);

        Chunk.AddInstruction(OpCode.Pop, binaryExpr.Position);

        var right = CompileExpr(binaryExpr.Right)
            .Expect(TypeSymbol.BoolSymbol, binaryExpr.Position);

        Chunk.PatchA(end);

        return TypeSymbol.BoolSymbol;
    }

    TypeSymbol CompileOr(BinaryExpr binaryExpr)
    {
        var left = CompileExpr(binaryExpr.Left)
            .Expect(TypeSymbol.BoolSymbol, binaryExpr.Position);

        int end = Chunk.AddInstruction(OpCode.JumpIfTrue, binaryExpr.Position);

        Chunk.AddInstruction(OpCode.Pop, binaryExpr.Position);

        var right = CompileExpr(binaryExpr.Right)
            .Expect(TypeSymbol.BoolSymbol, binaryExpr.Position);

        Chunk.PatchA(end);

        return TypeSymbol.BoolSymbol;
    }

    Error BinaryError(TypeSymbol left, TypeSymbol right, string op, Position position)
        => new Error($"Cannot apply '{op}' to {left} and {right}", position);

    TypeSymbol CompileArray(ArrayExpr arrayExpr)
    {
        var elements = arrayExpr.Elements
            .Select(CompileExpr)
            .ToArray();

        if (elements.Length == 0)
        {
            Chunk.AddInstruction(0, OpCode.MakeArray, arrayExpr.Position);
            return new EmptyArraySymbol();
        }

        var arrayType = elements[0];

        foreach (var element in elements)
        {
            if (element is EmptyArraySymbol && arrayType is ArraySymbol)
                continue;

            if (!TypeSymbol.TypeCheck(element, arrayType))
                throw new Error($"Array element does not match array element type", arrayExpr.Position);
        }

        Chunk.AddInstruction(elements.Length, OpCode.MakeArray, arrayExpr.Position);

        return new ArraySymbol(arrayType);
    }

    public TypeSymbol ResolveType(TypeSymbol type, Dictionary<string, TypeSymbol> bindings)
    {
        return type switch
        {
            TypeParameterSymbol parameter =>
                bindings[parameter.Name],

            ArraySymbol array =>
                new ArraySymbol(ResolveType(array.ElementType, bindings)),

            FunctionSymbol function =>
                new FunctionSymbol(
                    function.ParameterSymbols
                        .Select(p => ResolveType(p, bindings))
                        .ToList(),
                    ResolveType(function.ReturnType, bindings)
                ),

            ClassSymbol classSymbol =>
                new ClassSymbol(
                    classSymbol.ClassName,
                    classSymbol.TypeArguments
                        .Select(t => ResolveType(t, bindings))
                        .ToList()
                ),

            _ => type
        };
    }

    public TypeSymbol ResolveType(TypeSymbol type)
    {
        return ResolveType(type, _genericArguments);
    }

    Native BindNative(Native native, TypeSymbol[] typeArguments, Position position)
    {
        if (typeArguments.Length != native.GenericParameters.Length)
            throw new Error(
                $"Expected {native.GenericParameters.Length} type arguments, got {typeArguments.Length}",
                position
            );

        var bindings = native.GenericParameters
            .Zip(typeArguments)
            .ToDictionary(key => key.First, value => value.Second);

        var functionSymbol = (FunctionSymbol)ResolveType(native.FunctionSymbol, bindings);
        return native.Bound(functionSymbol, typeArguments);
    }
}