using System.Globalization;

class CastBuiltins : IBuiltin
{
    public void Register(Dictionary<string, Value> globals)
    {
        globals["cast"] = new Native(
            new(
                [new TypeParameterSymbol("TSource")],
                new TypeParameterSymbol("TTarget")
            ),
            ["TSource", "TTarget"],
            (typeArgs, args) =>
            {
                Value source = args[0];
                var sourceSymbol = typeArgs[0];
                var targetSymbol = typeArgs[1];

                switch (targetSymbol)
                {
                    case StringSymbol:
                        return new(args[0].ToString());

                    case IntSymbol:
                        switch (sourceSymbol)
                        {
                            case IntSymbol intSymbol:
                                return source;

                            case FloatSymbol floatSymbol:
                                return new((long)source.Float);

                            case BoolSymbol boolSymbol:
                                return new(source.Bool ? 1 : 0);

                            case StringSymbol stringSymbol:
                                return new(long.Parse(source.String, CultureInfo.InvariantCulture));

                            case EnumSymbol enumSymbol:
                                if (!enumSymbol.Values.Any(value => value.Value == source.Int))
                                    throw new InvalidOperationException($"{source.Int} is not a defined value of {enumSymbol}");
                                return source;
                        }
                        break;

                    case FloatSymbol:
                        switch (sourceSymbol)
                        {
                            case IntSymbol intSymbol:
                                return new((double)source.Int);

                            case FloatSymbol floatSymbol:
                                return source;

                            case BoolSymbol boolSymbol:
                                return new(source.Bool ? 1D : 0);

                            case StringSymbol stringSymbol:
                                return new(double.Parse(source.String, CultureInfo.InvariantCulture));
                        }
                        break;

                    case BoolSymbol:
                        switch (sourceSymbol)
                        {
                            case IntSymbol:
                                return new(source.Int != 0);

                            case FloatSymbol:
                                return new(source.Float != 0.0);

                            case BoolSymbol:
                                return source;

                            case StringSymbol:
                                return new(bool.Parse(source.String));
                        }
                        break;

                    case EnumSymbol enumSymbol:
                        switch (sourceSymbol)
                        {
                            case IntSymbol:
                                if (!enumSymbol.Values.Any(member => member.Value == source.Int))
                                {
                                    throw new InvalidCastException(
                                        $"{source.Int} is not a defined value of {enumSymbol.Name}"
                                    );
                                }

                                return source;
                        }
                        break;
                }

                throw new InvalidCastException($"Type {sourceSymbol} cannot be casted to type {targetSymbol}");
            },
            []
        ).ToValue();

        globals["tryCast"] = new Native(
        new(
            [new TypeParameterSymbol("TSource")],
            Result.ResultSymbolWithTParameter("TTarget")
        ),
        ["TSource", "TTarget"],
        (typeArgs, args) =>
        {
            Value source = args[0];
            var sourceSymbol = typeArgs[0];
            var targetSymbol = typeArgs[1];

            switch (targetSymbol)
            {
                case StringSymbol:
                    return Result.MakeResult(new Value(source.ToString()), true);

                case IntSymbol:
                    switch (sourceSymbol)
                    {
                        case IntSymbol:
                            return Result.MakeResult(source, true);

                        case FloatSymbol:
                            return Result.MakeResult(new Value((long)source.Float), true);

                        case BoolSymbol:
                            return Result.MakeResult(new Value(source.Bool ? 1L : 0L), true);

                        case StringSymbol:
                            if (long.TryParse(source.String, CultureInfo.InvariantCulture, out long result))
                            {
                                return Result.MakeResult(new Value(result), true);
                            }
                            break;

                        case EnumSymbol enumSymbol:
                            return Result.MakeResult(source, true);
                    }
                    break;

                case FloatSymbol:
                    switch (sourceSymbol)
                    {
                        case IntSymbol:
                            return Result.MakeResult(new Value((double)source.Int), true);

                        case FloatSymbol:
                            return Result.MakeResult(source, true);

                        case BoolSymbol:
                            return Result.MakeResult(new Value(source.Bool ? 1D : 0D), true);

                        case StringSymbol:
                            if (double.TryParse(source.String, CultureInfo.InvariantCulture, out double result))
                            {
                                return Result.MakeResult(new Value(result), true);
                            }
                            break;
                    }
                    break;

                case BoolSymbol:
                    switch (sourceSymbol)
                    {
                        case IntSymbol:
                            return Result.MakeResult(new Value(source.Int != 0), true);

                        case FloatSymbol:
                            return Result.MakeResult(new Value(source.Float != 0.0), true);

                        case BoolSymbol:
                            return Result.MakeResult(source, true);

                        case StringSymbol:
                            if (bool.TryParse(source.String, out bool result))
                            {
                                return Result.MakeResult(new Value(result), true);
                            }
                            break;
                    }
                    break;

                case EnumSymbol enumSymbol:
                    switch (sourceSymbol)
                    {
                        case IntSymbol:
                            if (enumSymbol.Values.Any(member => member.Value == source.Int))
                            {
                                return Result.MakeResult(source, true);
                            }
                            break;
                    }
                    break;
            }

            return Result.MakeResult(Value.DefaultValue(targetSymbol), false);
        },
        []
    ).ToValue();
    }
}