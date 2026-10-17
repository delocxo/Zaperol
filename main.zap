class Example
{
    var int number = 42;
}

class Box<T>
{
    var T value = default(T);
}

func T makeDefault<T>()
{
    return default(T);
}

var int integer = default(int);
var float decimal = default(float);
var bool flag = default(bool);
var string text = default(string);
var [int] array = default([int]);
var func(int, int) callback = default(func(int, int));
var Example instance = default(Example);

ioPrintln::<int>(integer);                 // 0
ioPrintln::<float>(decimal);               // 0
ioPrintln::<bool>(flag);                   // false
ioPrintln::<string>(text);                 // <null>
ioPrintln::<[int]>(array);                 // <null>
ioPrintln::<func(int, int)>(callback);     // <null>
ioPrintln::<Example>(instance);            // <null>

if (integer != 0)
{
    return -1;
}

if (decimal != 0.0)
{
    return -2;
}

if (flag != false)
{
    return -3;
}

// Generic substitution in default expressions.
if (makeDefault::<int>() != 0)
{
    return -4;
}

if (makeDefault::<bool>() != false)
{
    return -5;
}

// Default expressions inside generic field initializers.
var Box<int> integers = Box::<int>();
var Box<bool> flags = Box::<bool>();
var Box<Example> objects = Box::<Example>();

if (integers.value != 0)
{
    return -6;
}

if (flags.value != false)
{
    return -7;
}

ioPrintln::<Example>(objects.value); // <null>

// A null field can later receive a real instance.
objects.value = Example();

if (objects.value.number != 42)
{
    return -8;
}

ioPrintln::<string>("Default checks passed.");
return 0;