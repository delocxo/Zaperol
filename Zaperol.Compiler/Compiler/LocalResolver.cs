class LocalResolver
{
    Stack<Dictionary<string, Local>> _scopes = [];

    public LocalResolver()
    {
        _scopes.Push(new());
    }

    public bool TryResolve(string name, out Local? local)
    {
        foreach (var scope in _scopes)
            if (scope.TryGetValue(name, out local))
                return true;
        local = null;
        return false;
    }

    public Local Resolve(string name, Position position)
    {
        if (!TryResolve(name, out var local))
            throw new Error($"'{name}' does not exist in this scope", position);
        return local!;
    }

    public Local Declare(string name, ref int slot, TypeSymbol typeSymbol, Position position)
    {
        var scope = _scopes.Peek();

        if (scope.ContainsKey(name))
            throw new Error($"'{name}' has already been declared in this scope", position);

        int nextSlot = slot++;
        Local local = new Local(name, nextSlot, typeSymbol);
        scope[name] = local;

        return local;
    }

    public Local Assign(string name, TypeSymbol typeSymbol, Position position)
    {
        Local local = Resolve(name, position);
        local.CanAssign(typeSymbol, position);
        return local;
    }

    public void BeginScope() => _scopes.Push(new());
    public void EndScope() => _scopes.Pop();
}