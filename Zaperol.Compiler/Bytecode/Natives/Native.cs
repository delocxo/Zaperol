delegate Value NativeDelegate(TypeSymbol[] TypeArguments, Value[] args);

class Native
{
    public Native(
        FunctionSymbol functionSymbol,
        string[] genericParameters,
        NativeDelegate nativeDelegate,
        TypeSymbol[] typeSymbols)
    {
        FunctionSymbol = functionSymbol;
        GenericParameters = genericParameters;
        NativeDelegate = nativeDelegate;
        TypeSymbols = typeSymbols;
    }

    public FunctionSymbol FunctionSymbol { get; }
    public string[] GenericParameters { get; }
    public NativeDelegate NativeDelegate { get; }
    public TypeSymbol[] TypeSymbols { get; }

    public Native Bound(
        FunctionSymbol functionSymbol,
        TypeSymbol[] typeArguments)
        => new Native(functionSymbol, GenericParameters, NativeDelegate, typeArguments);

    public Value ToValue() =>
        new Value(this);
}