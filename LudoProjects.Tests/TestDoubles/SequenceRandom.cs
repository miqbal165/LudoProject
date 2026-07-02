namespace LudoProjects.Tests.TestDoubles;

internal sealed class SequenceRandom : Random
{
    private readonly Queue<int> _values;

    public SequenceRandom(params int[] values)
    {
        _values = new Queue<int>(values);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (_values.Count == 0)
        {
            throw new InvalidOperationException(
                "No deterministic dice value remains for this test.");
        }

        int value = _values.Dequeue();

        if (value < minValue || value >= maxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Dice value must be between {minValue} and {maxValue - 1}.");
        }

        return value;
    }
}