using System.Diagnostics;
using System.Globalization;

class Parser
{
    List<Token> _tokens;
    int _i = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> stmts = new List<Stmt>();

        while (NotAtEnd())
        {
            stmts.Add(ParseStmt());
        }

        return stmts;
    }

    Stmt ParseStmt()
    {
        if (Check(TokenType.Var))
            return ParseVar();

        else if (Check(TokenType.Infer))
            return ParseInfer();

        else if (Check(TokenType.Return))
            return ParseReturn();

        else if (Check(TokenType.If))
            return ParseIf();

        else if (Check(TokenType.While))
            return ParseWhile();

        else if (Check(TokenType.Leave))
            return ParseControlFlow(ControlFlowType.Leave);

        else if (Check(TokenType.Break))
            return ParseControlFlow(ControlFlowType.Break);

        else if (Check(TokenType.Continue))
            return ParseControlFlow(ControlFlowType.Continue);

        else if (Check(TokenType.Func))
            return ParseFunction();

        else if (Check(TokenType.Class))
            return ParseClass();

        else if (Check(TokenType.Enum))
            return ParseEnum();

        return ParseExprStmt();
    }

    ExprStmt ParseExprStmt()
    {
        Expr expr = ParseExpr();
        Expect(TokenType.Semicolon);
        return new ExprStmt(expr, expr.Position);
    }

    UnboundVarStmt ParseVar()
    {
        Position position = GetPosNext();

        ParserSymbol symbol = ParseType();

        string name = ParseName("Expected variable name");

        Expect(TokenType.Equal);

        Expr expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new UnboundVarStmt(name, symbol, expr, position);
    }

    InferStmt ParseInfer()
    {
        Position position = GetPosNext();

        string name = ParseName("Expected variable name");

        Expect(TokenType.Equal);

        Expr expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new InferStmt(name, expr, position);
    }

    ReturnStmt ParseReturn()
    {
        Position position = GetPosNext();

        Expr expr = ParseExpr();

        Expect(TokenType.Semicolon);

        return new ReturnStmt(expr, position);
    }

    IfStmt ParseIf()
    {
        List<IfBranch> branches = [ParseBranch(TokenType.If)];

        while (Check(TokenType.ElseIf))
            branches.Add(ParseBranch(TokenType.ElseIf));

        List<Stmt>? elsebody = null;

        if (Match(TokenType.Else))
            elsebody = ParseBody();

        return new IfStmt(branches, elsebody, branches[0].Position);
    }

    IfBranch ParseBranch(TokenType tokenType)
    {
        Position position = Expect(tokenType);

        Expect(TokenType.LeftParen);

        Expr condition = ParseExpr();

        Expect(TokenType.RightParen);

        var body = ParseBody();

        return new IfBranch(condition, body, position);
    }

    WhileStmt ParseWhile()
    {
        var branch = ParseBranch(TokenType.While);
        return new WhileStmt(branch, branch.Position);
    }

    ControlFlowStmt ParseControlFlow(ControlFlowType controlFlowType)
    {
        Position position = GetPosNext();
        Expect(TokenType.Semicolon);
        return new ControlFlowStmt(controlFlowType, position);
    }

    UnBoundFunctionStmt ParseFunction()
    {
        UnBoundFunctionParamater ParseParameter()
        {
            var type = ParseType();
            string name = ParseName("Expected parameter name");
            return new(name, type);
        }

        Position position = GetPosNext();

        var returnType = ParseType();

        string name = ParseName("Expected function name");

        List<string> generics = [];

        if (Check(TokenType.Less))
            generics = ParseNames(TokenType.Less, TokenType.Greater);

        Expect(TokenType.LeftParen);

        List<UnBoundFunctionParamater> paramaters = [];

        if (!Check(TokenType.RightParen))
        {
            paramaters.Add(ParseParameter());

            while (Match(TokenType.Comma))
                paramaters.Add(ParseParameter());
        }

        Expect(TokenType.RightParen);

        var body = ParseBody();

        return new(name, body, paramaters, generics, returnType, position);
    }

    UnBoundClassStmt ParseClass()
    {
        Position position = GetPosNext();

        string name = ParseName("Expected class name");

        List<string> generics = [];

        if (Check(TokenType.Less))
            generics = ParseNames(TokenType.Less, TokenType.Greater);

        var body = ParseBody();

        return new(name, generics, body, position);
    }

    UnboundEnumStmt ParseEnum()
    {
        Position position = GetPosNext();

        string name = ParseName("Expected enum name");

        List<string> values = ParseNames(TokenType.LeftBrace, TokenType.RightBrace);

        return new(name, values, position);
    }

    Error ThrowUnexpected()
    {
        Token token = Current();
        string? keyword = Lexer.GetKeywordFromType(token.TokenType);
        string? symbol = Lexer.GetSymbolFromType(token.TokenType);
        if (keyword != null)
            throw new Error($"Unexpected keyword '{keyword}'", token.Position);
        else if (symbol != null)
            throw new Error($"Unexpected symbol '{symbol}'", token.Position);
        else
            throw new Error($"Unexpected token '{token.TokenType}'", token.Position);
    }

    ParserSymbol ParseType()
    {
        Position position = Current().Position;

        if (Match(TokenType.LeftBracket))
        {
            ParserSymbol inner = ParseType();

            Expect(TokenType.RightBracket);

            return new ArrayParserSymbol(inner, position);
        }
        else if (Match(TokenType.Func))
        {
            Expect(TokenType.LeftParen);

            List<ParserSymbol> types = [ParseType()];

            while (Match(TokenType.Comma))
                types.Add(ParseType());

            Expect(TokenType.RightParen);

            return new FunctionParserSymbol(types, position);
        }

        string typeName = ParseName("Expected type name");

        if (Match(TokenType.Less))
        {
            List<ParserSymbol> types = [ParseType()];

            while (Match(TokenType.Comma))
                types.Add(ParseType());

            Expect(TokenType.Greater);

            return new GenericParserSymbol(typeName, types, position);
        }

        return new PrimitiveParserSymbol(typeName, position);
    }
    string ParseName()
    {
        string name = Current().Lexeme;
        Eat("Expected name", TokenType.Identifier);
        return name;
    }

    string ParseName(string message)
    {
        string name = Current().Lexeme;
        Eat(message, TokenType.Identifier);
        return name;
    }

    List<Stmt> ParseBody()
    {
        List<Stmt> stmts = new List<Stmt>();

        Expect(TokenType.LeftBrace);

        while (NotAtEnd() && !Check(TokenType.RightBrace))
            stmts.Add(ParseStmt());

        Expect(TokenType.RightBrace);

        return stmts;
    }

    List<string> ParseNames(TokenType end)
    {
        if (Match(end))
            return new List<string>();

        List<string> names = new List<string>()
            {
                ParseName()
            };

        while (Match(TokenType.Comma))
            names.Add(ParseName());

        Expect(end);

        return names;
    }

    List<string> ParseNames(TokenType start, TokenType end)
    {
        Expect(start);
        return ParseNames(end);
    }

    List<Expr> ParseArgs(TokenType start, TokenType end)
    {
        Expect(start);

        if (Match(end))
            return new List<Expr>();

        List<Expr> args = new List<Expr>()
            {
                ParseExpr()
            };

        while (Match(TokenType.Comma))
            args.Add(ParseExpr());

        Expect(end);

        return args;
    }

    bool Check(params TokenType[] types)
    {
        for (int i = 0; i < types.Length; i++)
            if (Current().TokenType == types[i])
                return true;
        return false;
    }

    Token Current() => _tokens[_i];
    Token Peek() => _tokens[_i + 1];
    bool NotAtEnd() => !Check(TokenType.Eof);
    bool PeekNotAtEnd() => _i + 1 < _tokens.Count;
    bool AtEnd() => Check(TokenType.Eof);
    void Next() => _i++;

    Position GetPosNext()
    {
        Position position = Current().Position;
        Next();
        return position;
    }

    void Eat(string message, params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return;
        }
        throw new Error(message, Current().Position);
    }

    bool Match(params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return true;
        }
        return false;
    }

    Position Expect(TokenType type)
    {
        if (Check(type))
        {
            return GetPosNext();
        }
        string? keyword = Lexer.GetKeywordFromType(type);
        string? symbol = Lexer.GetSymbolFromType(type);
        if (keyword != null)
            throw new Error($"Expected keyword '{keyword}'", Current().Position);
        else if (symbol != null)
            throw new Error($"Expected symbol '{symbol}'", Current().Position);
        else
            throw new Error($"Expected token '{type}'", Current().Position);
    }

    UnBoundGenericNameExpr ParseGenericName(string name, Position position)
    {
        Expect(TokenType.Less);

        List<ParserSymbol> generics = [ParseType()];

        while (Match(TokenType.Comma))
            generics.Add(ParseType());

        Expect(TokenType.Greater);

        return new UnBoundGenericNameExpr(name, generics, position);
    }

    Expr ParsePrimary()
    {
        Token token = Current();

        if (Match(TokenType.Number))
        {
            if (long.TryParse(token.Lexeme, CultureInfo.InvariantCulture, out long longResult))
                return new IntExpr(longResult, token.Position);

            else if (double.TryParse(token.Lexeme, CultureInfo.InvariantCulture, out double doubleResult))
                return new FloatExpr(doubleResult, token.Position);

            throw new UnreachableException();
        }
        else if (Match(TokenType.String))
            return new StringExpr(token.Lexeme, token.Position);
        else if (Match(TokenType.True))
            return new BoolExpr(true, token.Position);
        else if (Match(TokenType.False))
            return new BoolExpr(false, token.Position);
        else if (Match(TokenType.Identifier))
        {
            if (Match(TokenType.DoubleColon))
                return ParseGenericName(token.Lexeme, token.Position);
            return new NameExpr(token.Lexeme, token.Position);
        }
        else if (Match(TokenType.LeftParen))
        {
            Expr expr = ParseExpr();
            Expect(TokenType.RightParen);
            return expr;
        }
        else if (Check(TokenType.LeftBracket))
        {
            List<Expr> elements = ParseArgs(TokenType.LeftBracket, TokenType.RightBracket);
            return new ArrayExpr(elements, token.Position);
        }
        else if (Match(TokenType.Typeof))
        {
            Expect(TokenType.LeftParen);

            Expr expr = ParseExpr();

            Expect(TokenType.RightParen);

            return new TypeofExpr(expr, token.Position);
        }
        else if (Match(TokenType.Default))
        {
            Expect(TokenType.LeftParen);

            var symbol = ParseType();

            Expect(TokenType.RightParen);

            return new UnboundDefaultExpr(symbol, token.Position);
        }

        throw ThrowUnexpected();
    }

    Expr ParsePostfix()
    {
        Expr left = ParsePrimary();
        while (Check(TokenType.LeftParen, TokenType.LeftBracket, TokenType.Period))
        {
            if (Check(TokenType.LeftParen))
            {
                Position position = Current().Position;

                List<Expr> args = ParseArgs(TokenType.LeftParen, TokenType.RightParen);

                left = new CallExpr(left, args, position);

                continue;
            }

            if (Check(TokenType.LeftBracket))
            {
                Position position = Current().Position;

                Expect(TokenType.LeftBracket);

                Expr index = ParseExpr();

                Expect(TokenType.RightBracket);

                left = new IndexExpr(left, index, position);

                continue;
            }

            if (Check(TokenType.Period))
            {
                Position position = Current().Position;

                Next();

                Position namePos = Current().Position;
                string name = ParseName();

                Expr member = new NameExpr(name, namePos);

                if (Match(TokenType.DoubleColon))
                    member = ParseGenericName(name, namePos);

                left = new MemberExpr(left, member, position);

                continue;
            }

            break;
        }
        return left;
    }

    Expr ParseUnary()
    {
        if (Check(TokenType.Sub, TokenType.Bang, TokenType.BitwiseNot, TokenType.At))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            return new UnaryExpr(right, op.TokenType, op.Position);
        }
        return ParsePostfix();
    }

    Expr ParseTerm()
    {
        Expr left = ParseUnary();

        while (Check(TokenType.Mul, TokenType.Div, TokenType.Mod))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseFactor()
    {
        Expr left = ParseTerm();

        while (Check(TokenType.Add, TokenType.Sub))
        {
            Token op = Current();

            Next();

            Expr right = ParseTerm();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseShift()
    {
        Expr left = ParseFactor();

        while (Check(TokenType.BitwiseLeftShift, TokenType.BitwiseRightShift))
        {
            Token op = Current();

            Next();

            Expr right = ParseFactor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseAnd()
    {
        Expr left = ParseBitwiseShift();

        while (Check(TokenType.BitwiseAnd))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseShift();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseXor()
    {
        Expr left = ParseBitwiseAnd();

        while (Check(TokenType.BitwiseXor))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseOr()
    {
        Expr left = ParseBitwiseXor();

        while (Check(TokenType.BitwiseOr))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseXor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseComparison()
    {
        Expr left = ParseBitwiseOr();

        while (Check(TokenType.Less, TokenType.Greater, TokenType.LessEq, TokenType.GreaterEq))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseOr();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseEquality()
    {
        Expr left = ParseComparison();

        while (Check(TokenType.NotEqual, TokenType.IsEqual))
        {
            Token op = Current();

            Next();

            Expr right = ParseComparison();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseAnd()
    {
        Expr left = ParseEquality();

        while (Check(TokenType.And))
        {
            Token op = Current();

            Next();

            Expr right = ParseEquality();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseOr()
    {
        Expr left = ParseAnd();

        while (Check(TokenType.Or))
        {
            Token op = Current();

            Next();

            Expr right = ParseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseAssign()
    {
        Expr left = ParseOr();

        if (Match(TokenType.Equal))
        {
            Expr expr = ParseAssign();

            if (left is NameExpr nameExpr)
                return new AssignExpr(nameExpr.Name, expr, left.Position);

            else if (left is IndexExpr indexExpr)
                return new IndexSetExpr(indexExpr, expr);

            else if (left is MemberExpr memberExpr)
                return new MemberSetExpr(memberExpr, expr);

            throw new Error("Invalid assign target", left.Position);
        }

        return left;
    }

    Expr ParseExpr() => ParseAssign();
}