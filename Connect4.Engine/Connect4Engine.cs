namespace Connect4.Engine;

/// <summary>
/// Chooses the computer's moves, using the opening book where it can and searching to the end of the game otherwise.
/// </summary>
/// <param name="tableSize">The number of slots in the solver's transposition table.</param>
public sealed class Connect4Engine(int tableSize = 8388593)
{
    /// <summary>
    /// The order columns are tried in, centre first.
    /// </summary>
    private readonly int[] ColumnOrder = [3, 2, 4, 1, 5, 0, 6];

    /// <summary>
    /// The solver used to score positions, kept between moves so its table is reused.
    /// </summary>
    private readonly Solver Solver = new(tableSize);

    /// <summary>
    /// Returns the best column for the player to move.
    /// </summary>
    /// <param name="position">The position to choose a move for.</param>
    public int GetBestMove(Position position)
    {
        // Use the opening book if the position is in it.
        if (OpeningBook.Entries.TryGetValue(position.Key(), out int bookColumn))
            return bookColumn;

        // Win straight away if possible.
        foreach (int col in ColumnOrder)
        {
            if (position.CanPlay(col) && position.IsWinningMove(col))
                return col;
        }

        // Otherwise score each move that doesn't lose immediately, and pick the one that is best for us.
        ulong safeMoves = position.PossibleNonLosingMoves();
        int bestColumn = -1;
        int bestScore = int.MinValue;
        foreach (int col in ColumnOrder)
        {
            if ((safeMoves & Position.ColumnMask(col)) == 0)
                continue;
            Position child = position.Clone();
            child.Play(col);
            int score = -Solver.Solve(child);
            if (score > bestScore)
            {
                bestScore = score;
                bestColumn = col;
            }
        }

        // If every move loses, play any legal column.
        return bestColumn >= 0 ? bestColumn : ColumnOrder.First(position.CanPlay);
    }
}