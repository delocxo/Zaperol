static class Globals
{
    public static Dictionary<string, Value> Values { get; } = [];

    static List<IBuiltin> _toBeLoaded = [
        new ArrayBuiltins(),
        new IoBuiltins()
    ];

    public static void Init()
    {
        foreach (var builtin in _toBeLoaded)
        {
            builtin.Register(Values);
        }
    }
}