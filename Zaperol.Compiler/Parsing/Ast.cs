using System;
using System.Collections.Generic;
using System.Text;

abstract record Expr(Position Position);
record IntExpr(long Value, Position Position) : Expr(Position);
record FloatExpr(double Value, Position Position) : Expr(Position);
record StringExpr(string Value, Position Position) : Expr(Position);
record BoolExpr(bool Value, Position Position) : Expr(Position);
record NameExpr(string Name, Position Position) : Expr(Position);
record UnaryExpr(Expr Right, TokenType Op, Position Position) : Expr(Position);
record BinaryExpr(Expr Left, Expr Right, TokenType Op, Position Position) : Expr(Position);
record ArrayExpr(List<Expr> Elements, Position Position) : Expr(Position);
record TypeofExpr(Expr Expr, Position Position) : Expr(Position);
record AssignExpr(string Name, Expr Expr, Position Position) : Expr(Position);
record UnBoundGenericNameExpr(string Name, List<ParserSymbol> Generics, Position Position) : Expr(Position);
record BoundGenericNameExpr(string Name, List<TypeSymbol> Generics, Position Position) : Expr(Position);
record CallExpr(Expr Target, List<Expr> Arguments, Position Position) : Expr(Position);
record IndexExpr(Expr Target, Expr Index, Position Position) : Expr(Position);
record IndexSetExpr(IndexExpr IndexExpr, Expr Value) : Expr(IndexExpr.Position);
record MemberExpr(Expr Target, Expr Member, Position Position) : Expr(Position);
record MemberSetExpr(MemberExpr MemberExpr, Expr Value) : Expr(MemberExpr.Position);
record UnboundDefaultExpr(ParserSymbol Type, Position Position) : Expr(Position);
record BoundDefaultExpr(TypeSymbol Type, Position Position) : Expr(Position);

abstract class Stmt
{
    public Stmt(Position position)
    {
        Position = position;
    }

    public Position Position { get; }
}

class ExprStmt : Stmt
{
    public ExprStmt(Expr expr, Position position) : base(position)
    {
        Expr = expr;
    }

    public Expr Expr { get; set; }
}

class UnboundVarStmt : Stmt
{
    public UnboundVarStmt(string name, ParserSymbol parserSymbol, Expr expr, Position position) : base(position)
    {
        Name = name;
        ParserSymbol = parserSymbol;
        Expr = expr;
    }

    public string Name { get; }
    public ParserSymbol ParserSymbol { get; }
    public Expr Expr { get; }
}

class BoundVarStmt : Stmt
{
    public BoundVarStmt(string name, TypeSymbol typeSymbol, Expr expr, Position position) : base(position)
    {
        Name = name;
        TypeSymbol = typeSymbol;
        Expr = expr;
    }

    public string Name { get; }
    public TypeSymbol TypeSymbol { get; }
    public Expr Expr { get; set; }
}

class InferStmt : Stmt
{
    public InferStmt(string name, Expr expr, Position position) : base(position)
    {
        Name = name;
        Expr = expr;
    }

    public string Name { get; }
    public Expr Expr { get; set; }
}

class ReturnStmt : Stmt
{
    public ReturnStmt(Expr expr, Position position) : base(position)
    {
        Expr = expr;
    }

    public Expr Expr { get; set; }
}

class IfBranch
{
    public IfBranch(Expr condition, List<Stmt> stmts, Position position)
    {
        Condition = condition;
        Stmts = stmts;
        Position = position;
    }


    public Expr Condition { get; set; }
    public List<Stmt> Stmts { get; set; }
    public Position Position { get; }
}

class IfStmt : Stmt
{
    public IfStmt(List<IfBranch> ifBranches, List<Stmt>? elseBody, Position position) : base(position)
    {
        IfBranches = ifBranches;
        ElseBody = elseBody;
    }

    public List<IfBranch> IfBranches { get; }
    public List<Stmt>? ElseBody { get; set; }
}

class WhileStmt : Stmt
{
    public WhileStmt(IfBranch ifBranch, Position position) : base(position)
    {
        IfBranch = ifBranch;
    }

    public IfBranch IfBranch { get; }
}

enum ControlFlowType
{
    Leave,
    Break,
    Continue
}

class ControlFlowStmt : Stmt
{
    public ControlFlowStmt(ControlFlowType type, Position position) : base(position)
    {
        Type = type;
    }

    public ControlFlowType Type { get; }
}

class UnBoundFunctionParamater
{
    public UnBoundFunctionParamater(string name, ParserSymbol parserSymbol)
    {
        Name = name;
        ParserSymbol = parserSymbol;
    }

    public string Name { get; }
    public ParserSymbol ParserSymbol { get; }
    public Position Position => ParserSymbol.Position;
}

class UnBoundFunctionStmt : Stmt
{
    public UnBoundFunctionStmt(
        string name,
        List<Stmt> body,
        List<UnBoundFunctionParamater> paramaters,
        List<string> generics,
        ParserSymbol returnType,
        Position position) : base(position)
    {
        Name = name;
        Body = body;
        Paramaters = paramaters;
        Generics = generics;
        ReturnType = returnType;
    }

    public string Name { get; }
    public List<Stmt> Body { get; }
    public List<UnBoundFunctionParamater> Paramaters { get; }
    public List<string> Generics { get; }
    public ParserSymbol ReturnType { get; }
}

class BoundFunctionParamater
{
    public BoundFunctionParamater(string name, TypeSymbol typeSymbol)
    {
        Name = name;
        TypeSymbol = typeSymbol;
    }

    public string Name { get; }
    public TypeSymbol TypeSymbol { get; }
}

class BoundFunctionStmt : Stmt
{
    public BoundFunctionStmt(
        string name,
        List<Stmt> body,
        List<BoundFunctionParamater> paramaters,
        List<string> generics,
        TypeSymbol returnType,
        Position position) : base(position)
    {
        Name = name;
        Body = body;
        Paramaters = paramaters;
        Generics = generics;
        ReturnType = returnType;
    }

    public string Name { get; }
    public List<Stmt> Body { get; }
    public List<BoundFunctionParamater> Paramaters { get; }
    public List<string> Generics { get; }
    public TypeSymbol ReturnType { get; }
}

class UnBoundClassStmt : Stmt
{
    public UnBoundClassStmt(
        string name,
        List<string> generics,
        List<Stmt> body,
        Position position) : base(position)
    {
        Name = name;
        Generics = generics;
        Body = body;
    }

    public string Name { get; }
    public List<string> Generics { get; }
    public List<Stmt> Body { get; }
}

class BoundClassStmt : Stmt
{
    public BoundClassStmt(
        string name,
        List<string> generics,
        List<BoundVarStmt> fields,
        List<BoundFunctionStmt> methods,
        Position position) : base(position)
    {
        Name = name;
        Generics = generics;
        Fields = fields;
        Methods = methods;
    }

    public string Name { get; }
    public List<string> Generics { get; }
    public List<BoundVarStmt> Fields { get; }
    public List<BoundFunctionStmt> Methods { get; }
}