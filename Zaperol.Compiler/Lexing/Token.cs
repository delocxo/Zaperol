internal enum TokenType
{
    String, Number, Identifier,

    True, False, Var, Infer, Typeof, Return,
    If, Else, ElseIf, Leave, While, Break,
    Continue, Func, Class, Default,

    Add, Sub, Mul, Div, Mod,
    IsEqual, NotEqual, Less, Greater,
    LessEq, GreaterEq, And, Or, Bang,
    BitwiseNot, BitwiseLeftShift, BitwiseRightShift,
    BitwiseAnd, BitwiseOr, BitwiseXor,

    Equal, Semicolon, LeftBracket, RightBracket,
    LeftBrace, RightBrace, LeftParen, RightParen,
    Comma, Period, Hash, Arrow, At, DoubleColon,

    Eof,
}

internal record Token(TokenType TokenType, string Lexeme, Position Position);