class IfContext
{
    public List<int> Leaves { get; } = [];
}

class LoopContext
{
    public List<int> Breaks { get; } = [];
    public List<int> Continues { get; } = [];
}