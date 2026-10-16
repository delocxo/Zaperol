var [int] source = [10, 20, 30, 40];
var [int] destination = [1, 2, 3, 4, 5];

arrayCopy::<int>(source, 1, destination, 2, 2);
ioPrintln::<[int]>(destination); // [1, 2, 20, 30, 5]

if (arrayGet::<int>(destination, 0) != 1)
{
    return -1;
}

if (arrayGet::<int>(destination, 2) != 20)
{
    return -2;
}

if (arrayGet::<int>(destination, 3) != 30)
{
    return -3;
}

if (arrayGet::<int>(destination, 4) != 5)
{
    return -4;
}

// Overlapping copy toward the right.
var [int] overlap = [1, 2, 3, 4, 5];
arrayCopy::<int>(overlap, 0, overlap, 1, 4);
ioPrintln::<[int]>(overlap); // [1, 1, 2, 3, 4]

if (arrayGet::<int>(overlap, 4) != 4)
{
    return -5;
}

// Overlapping copy toward the left.
arrayCopy::<int>(overlap, 1, overlap, 0, 4);
ioPrintln::<[int]>(overlap); // [1, 2, 3, 4, 4]

if (arrayGet::<int>(overlap, 2) != 3)
{
    return -6;
}

arrayClear::<int>(destination);
ioPrintln::<[int]>(destination); // [0, 0, 0, 0, 0]

var int i = 0;
while (i < arrayLength::<int>(destination))
{
    if (arrayGet::<int>(destination, i) != 0)
    {
        return -7;
    }

    i = i + 1;
}

var [bool] flags = [true, true];
arrayClear::<bool>(flags);
ioPrintln::<[bool]>(flags); // [false, false]

if (arrayGet::<bool>(flags, 0) != false)
{
    return -8;
}

// Cleared elements must remain usable.
arraySet::<int>(destination, 0, 99);

if (arrayGet::<int>(destination, 0) != 99)
{
    return -9;
}

ioPrintln::<string>("Copy and clear checks passed.");
return 0;