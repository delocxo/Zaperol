enum Color
{
    Red,
    Green,
    Blue
}

// Enum to integer.
var int number = cast::<Color, int>(Color.Green);
ioPrintln::<int>(number); // 1

if (number != 1)
{
    return -1;
}

// Integer to enum.
var Color color = cast::<int, Color>(2);
ioPrintln::<Color>(color); // 2

if (color != Color.Blue)
{
    return -2;
}

// Successful tryCast to enum.
var Result<Color> valid = tryCast::<int, Color>(1);

if (!valid.success || valid.data != Color.Green)
{
    return -3;
}

// Failed tryCast to enum.
var Result<Color> invalid = tryCast::<int, Color>(99);
ioPrintln::<bool>(invalid.success); // false

if (invalid.success)
{
    return -4;
}

// Successful tryCast from enum.
var Result<int> numeric = tryCast::<Color, int>(Color.Blue);

if (!numeric.success || numeric.data != 2)
{
    return -5;
}

// Check the other result-producing branches.
var Result<float> decimal = tryCast::<int, float>(42);

if (!decimal.success || decimal.data != 42.0)
{
    return -6;
}

var Result<bool> flag = tryCast::<int, bool>(1);

if (!flag.success || flag.data != true)
{
    return -7;
}

var Result<int> badText = tryCast::<string, int>("hello");

if (badText.success || badText.data != 0)
{
    return -8;
}

ioPrintln::<string>("Cast checks passed.");
return 0;