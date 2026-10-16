enum OpCode : byte
{
    LoadLocal,
    StoreLocal,
    LoadConst,

    MakeArray,

    AddIntInt,
    AddNumbers,

    SubIntInt,
    SubNumbers,

    MulIntInt,
    MulNumbers,

    DivIntInt,
    DivNumbers,

    ModIntInt,
    ModNumbers,

    LessIntInt,
    LessNumbers,

    GreaterIntInt,
    GreaterNumbers,

    LessEqualIntInt,
    LessEqualNumbers,

    GreaterEqualIntInt,
    GreaterEqaulNumbers,

    EqualsIntInt,
    EqaulsNumbers,

    NegateInt,
    NegateFloat,

    EqualsStrings,
    EqualsBools,
    EqualsArrays,

    NotEqualIntInt,
    NotEqualNumbers,

    NotEqualStrings,
    NotEqualBools,
    NotEqualArrays,

    Concat,
    FlipBool,

    JumpIfFalse,
    JumpIfTrue,
    JumpIfFalsePop,
    JumpIfTruePop,
    Jump,
    Pop,
    Call,
    Ret
}