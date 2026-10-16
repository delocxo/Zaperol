abstract record TypeSymbol(string Name)
{
    public static bool TypeCheck(TypeSymbol left, TypeSymbol right)
    {
        if (left is FunctionSymbol a && right is FunctionSymbol b)
        {
            return a.ParameterSymbols.Count == b.ParameterSymbols.Count &&
                a.ParameterSymbols
                    .Zip(b.ParameterSymbols)
                    .All(pair => TypeCheck(pair.First, pair.Second))
                && TypeCheck(a.ReturnType, b.ReturnType);
        }
        else if (left is ArraySymbol leftArray && right is ArraySymbol rightArray)
            return TypeCheck(leftArray.ElementType, rightArray.ElementType);
        return left == right;
    }

    public static BoolSymbol BoolSymbol = new();
    public static InvalidSymbol InvalidSymbol = new();

    public TypeSymbol Expect(TypeSymbol right, string errorMessage, Position position)
    {
        if (!TypeCheck(this, right))
            throw new Error(errorMessage, position);
        return this;
    }

    public TypeSymbol Expect(TypeSymbol right, Position position)
    {
        return Expect(right, $"Expected type {right}, got type {this}", position);
    }

    public bool IsNumeric() => this is FloatSymbol || this is IntSymbol;

    public sealed override string ToString()
    {
        if (this is ArraySymbol arraySymbol)
        {
            return $"[{arraySymbol.ElementType}]";
        }
        else if (this is EmptyArraySymbol emptyArray)
        {
            return $"[]";
        }
        else if (this is FunctionSymbol function)
        {
            if (function.ParameterSymbols.Count == 0)
                return $"function({function.ReturnType})";
            return $"function({string.Join(", ", function.ParameterSymbols)}, {function.ReturnType})";
        }
        return Name;
    }
}
record IntSymbol() : TypeSymbol("int");
record FloatSymbol() : TypeSymbol("float");
record StringSymbol() : TypeSymbol("string");
record BoolSymbol() : TypeSymbol("bool");
record ArraySymbol(TypeSymbol ElementType) : TypeSymbol("array");
record EmptyArraySymbol() : TypeSymbol("array");
record InvalidSymbol() : TypeSymbol("invalid");
record TypeParameterSymbol(string Name) : TypeSymbol(Name);
record FunctionSymbol(List<TypeSymbol> ParameterSymbols, TypeSymbol ReturnType) : TypeSymbol("function");