namespace DataverseProxyGenerator.Core.Generation.Utilities;

internal class Counter(int initialValue = 0)
{
    private readonly int initialValue = initialValue;

    public int Count { get; private set; } = initialValue;

    public int Increment(int byValue = 1)
    {
        Count += byValue;
        return Count;
    }

    public void Reset() => Count = initialValue;
}
