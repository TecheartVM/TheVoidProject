public class StatModifier
{
    public readonly float value;
    public readonly StatModType type;
    public readonly int sortOrder;
    public readonly object source;
    public StatModifier(float value, StatModType type, int sortOrder, object source)
    {
        this.value = value;
        this.type = type;
        this.sortOrder = sortOrder;
        this.source = source;
    }

    public StatModifier(float value, StatModType type) : this(value, type, (int)type, null) { }
    public StatModifier(float value, StatModType type, int sortOrder) : this(value, type, sortOrder, null) { }
    public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }

    public static int CompareOrder(StatModifier a, StatModifier b)
    {
        return a.sortOrder < b.sortOrder ? -1 : (a.sortOrder > b.sortOrder ? 1 : 0);
    }
}

public enum StatModType
{
    Flat = 100,
    PercentAdd = 200,
    Mult = 300,
    PercentMult = 400
}
