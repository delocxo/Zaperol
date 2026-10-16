func int subtract(int a, int b)
{
    return a - b;
}

func T identity<T>(T value)
{
    var T copy = value;
    return copy;
}

func T forward<T>(T value)
{
    var func(T, T) other = identity<T>;
    return other(value);
}

func int factorial(int n)
{
    if (n <= 1)
    {
        return 1;
    }

    return n * factorial(n - 1);
}

var int difference = subtract(20, 3);
var int nested = subtract(subtract(30, 5), subtract(10, 2));

var func(int, int) saved = identity<int>;
var int savedResult = saved(42);

var float decimalResult = identity<float>(2.5);
var string textResult = forward<string>("hello");
var [int] arrayResult = identity<[int]>([1, 2, 3]);

var int recursiveResult = factorial(5);

if (difference != 17)
{
    return -1;
}

if (nested != 17)
{
    return -2;
}

if (savedResult != 42)
{
    return -3;
}

if (decimalResult != 2.5)
{
    return -4;
}

if (textResult != "hello")
{
    return -5;
}

if (recursiveResult != 120)
{
    return -6;
}

// Expected result: 120
return recursiveResult; 