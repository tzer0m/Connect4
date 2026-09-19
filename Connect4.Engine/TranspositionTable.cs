namespace Connect4.Engine;

/// <summary>
/// A fixed-size cache that remembers a value for each position the solver has already seen.
/// </summary>
/// <param name="size">The number of slots in the table.</param>
public sealed class TranspositionTable(int size)
{
    /// <summary>
    /// The full key stored in each slot, used to detect when two positions share a slot.
    /// </summary>
    private readonly ulong[] Keys = new ulong[size];

    /// <summary>
    /// The value stored in each slot, where 0 means nothing has been stored.
    /// </summary>
    private readonly byte[] Values = new byte[size];

    /// <summary>
    /// Stores a value for a position, replacing whatever was in its slot.
    /// </summary>
    /// <param name="key">The position's unique key.</param>
    /// <param name="value">The value to store, which must not be 0.</param>
    public void Put(ulong key, byte value)
    {
        int index = (int)(key % (ulong)Keys.Length);
        Keys[index] = key;
        Values[index] = value;
    }

    /// <summary>
    /// Returns the value stored for a position, or 0 if there isn't one.
    /// </summary>
    /// <param name="key">The position's unique key.</param>
    public byte Get(ulong key)
    {
        int index = (int)(key % (ulong)Keys.Length);
        return Keys[index] == key ? Values[index] : (byte)0;
    }
}