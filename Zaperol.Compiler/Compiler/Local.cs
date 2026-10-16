class Local
{
    public Local(string name, int slot, TypeSymbol typeSymbol)
    {
        Name = name;
        Slot = slot;
        TypeSymbol = typeSymbol;
    }

    public string Name { get; }
    public int Slot { get; }
    public TypeSymbol TypeSymbol { get; }

    public void CanAssign(TypeSymbol typeSymbol, Position position)
    {
        if (typeSymbol is EmptyArraySymbol && TypeSymbol is ArraySymbol)
            return;

        if (!TypeSymbol.TypeCheck(TypeSymbol, typeSymbol))
            throw new Error($"Cannot assign type {typeSymbol} to '{Name}'", position);
    }
}