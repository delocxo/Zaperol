class List<T>
{
    var [T] items = arrayCreate::<T>(4);
    var int count = 0;

    func int Count(List<T> this)
    {
        return this.count;
    }

    func int Capacity(List<T> this)
    {
        return arrayLength::<T>(this.items);
    }

    func int EnsureCapacity(List<T> this, int minimum)
    {
        var int capacity = this.Capacity();

        if (capacity >= minimum)
        {
            return capacity;
        }

        var int newCapacity = capacity * 2;

        if (newCapacity < 4)
        {
            newCapacity = 4;
        }

        if (newCapacity < minimum)
        {
            newCapacity = minimum;
        }

        var [T] larger = arrayCreate::<T>(newCapacity);
        arrayCopy::<T>(this.items, 0, larger, 0, this.count);
        this.items = larger;

        return newCapacity;
    }

    // Invalid logical indices trigger the array bounds check.
    func int CheckedIndex(List<T> this, int index)
    {
        if (index < 0 || index >= this.count)
        {
            return -1;
        }

        return index;
    }

    func int Add(List<T> this, T value)
    {
        this.EnsureCapacity(this.count + 1);
        this.items[this.count] = value;
        this.count = this.count + 1;

        return 0;
    }

    func T Get(List<T> this, int index)
    {
        return this.items[this.CheckedIndex(index)];
    }

    func T Set(List<T> this, int index, T value)
    {
        return this.items[this.CheckedIndex(index)] = value;
    }

    func T RemoveAt(List<T> this, int index)
    {
        var T removed = this.Get(index);

        arrayCopy::<T>(
            this.items,
            index + 1,
            this.items,
            index,
            this.count - index - 1
        );

        this.count = this.count - 1;
        this.items[this.count] = default(T);

        return removed;
    }

    func T Pop(List<T> this)
    {
        return this.RemoveAt(this.count - 1);
    }

    func int Clear(List<T> this)
    {
        arrayClear::<T>(this.items);
        this.count = 0;

        return 0;
    }

    func [T] ToArray(List<T> this)
    {
        var [T] result = arrayCreate::<T>(this.count);
        arrayCopy::<T>(this.items, 0, result, 0, this.count);

        return result;
    }
}

var List<int> numbers = List::<int>();

var int i = 1;
while (i <= 6)
{
    numbers.Add(i * 10);
    i = i + 1;
}

ioPrintln::<[int]>(numbers.ToArray());
// [10, 20, 30, 40, 50, 60]

ioPrintln::<int>(numbers.Count());    // 6
ioPrintln::<int>(numbers.Capacity()); // 8

numbers.Set(0, 99);
ioPrintln::<int>(numbers.Get(0));     // 99

ioPrintln::<int>(numbers.RemoveAt(1)); // 20
ioPrintln::<int>(numbers.Pop());       // 60

ioPrintln::<[int]>(numbers.ToArray());
// [99, 30, 40, 50]

var [int] copy = numbers.ToArray();
copy[0] = -1;

if (numbers.Get(0) != 99)
{
    return -1;
}

numbers.Clear();
ioPrintln::<[int]>(numbers.ToArray()); // []

numbers.Add(42);

if (numbers.Get(0) != 42)
{
    return -2;
}

var List<string> words = List::<string>();
words.Add("hello");
words.Add("world");

ioPrintln::<[string]>(words.ToArray());
// ['hello', 'world']

words.RemoveAt(0);

if (words.Get(0) != "world")
{
    return -3;
}

var List<bool> flags = List::<bool>();
flags.Add(true);
flags.Clear();
flags.Add(false);

if (flags.Get(0) != false)
{
    return -4;
}

ioPrintln::<string>("List method checks passed.");
return 0;