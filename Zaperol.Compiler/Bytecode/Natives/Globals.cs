static class Globals
{
    public static Dictionary<string, Value> Values { get; } = [];
    public static Dictionary<string, BoundClassStmt> Classes { get; } = [];

    static List<IBuiltin> _toBeLoaded = [
        new ArrayBuiltins(),
        new IoBuiltins(),
        new CastBuiltins(),
        new ErrorBuiltins(),
        new Result()
    ];

    public static void Init()
    {
        foreach (var builtin in _toBeLoaded)
        {
            builtin.Register(Values);
            if (builtin is IClass @class)
            {
                foreach (var unbound in @class.Unbound)
                {
                    Semantics.AddName(unbound.Name, "class", null);
                    Semantics.Classes[unbound.Name] = unbound;
                }
                foreach (var bound in @class.Bound)
                    Classes[bound.Name] = bound;
            }
        }
    }
}