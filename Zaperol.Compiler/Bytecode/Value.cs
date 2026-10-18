using System.Globalization;

enum ValueKind : byte
{
    Int,
    Float,
    String,
    Bool,
    Array,
    Function,
    Object
}

readonly struct Value
{
    public Value(long @int)
    {
        Kind = ValueKind.Int;
        Int = @int;
    }

    public Value(double @double)
    {
        Kind = ValueKind.Float;
        Float = @double;
    }

    public Value(string @string)
    {
        Kind = ValueKind.String;
        Object = @string;
    }

    public Value(bool @bool)
    {
        Kind = ValueKind.Bool;
        Bool = @bool;
    }

    public Value(Value[] array)
    {
        Kind = ValueKind.Array;
        Object = array;
    }

    public Value(Chunk chunk)
    {
        Kind = ValueKind.Function;
        Object = chunk;
    }

    public Value(Native native)
    {
        Kind = ValueKind.Function;
        Object = native;
    }

    public Value(object? @object)
    {
        Kind = ValueKind.Object;
        Object = @object;
    }

    public Value(object? @object, ValueKind kind)
    {
        Kind = ValueKind.Object;
        Object = @object;
    }

    public ValueKind Kind { get; }
    public long Int { get; }
    public double Float { get; }
    public bool Bool { get; }
    public object? Object { get; }
    public string String => (string)Object!;
    public Value[] Array => (Value[])Object!;
    public Chunk Function => (Chunk)Object!;
    public Native Native => (Native)Object!;

    public override string ToString()
    {
        return Kind switch
        {
            ValueKind.Int => Int.ToString(CultureInfo.InvariantCulture),
            ValueKind.Float => Float.ToString(CultureInfo.InvariantCulture),
            ValueKind.String => String,
            ValueKind.Bool => Bool ? "true" : "false",
            ValueKind.Array => ArrayToString(),
            ValueKind.Function => "<function()>",
            ValueKind.Object => ObjectToString(),
            _ => "<unknown>"
        };
    }

    public string ToStringQuotes()
    {
        if (Kind == ValueKind.String)
            return $"'{String}'";
        return ToString();
    }

    string ArrayToString()
    {
        var array = Array
            .Select(x => x.ToStringQuotes());
        string joined = string.Join(", ", array);
        return $"[{joined}]";
    }

    string ClassToString()
    {
        var array = Array
            .Select(x => x.ToStringQuotes());
        string joined = string.Join(", ", array);
        return $"<class({joined})>";
    }

    string ObjectToString()
    {
        if (Object == null)
            return "<null>";
        return Object.ToString() ?? "<object>";
    }

    public int AsInt32() => (int)Int;

    public static Value DefaultValue(TypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            IntSymbol => new Value(0L),
            FloatSymbol => new Value(0.0),
            BoolSymbol => new Value(false),

            StringSymbol or
            ArraySymbol or
            FunctionSymbol or
            EnumSymbol or
            ClassSymbol =>
                new Value((object?)null),

            _ => throw new InvalidOperationException(
                $"Cannot create a default value for {typeSymbol}"
            )
        };
    }
}