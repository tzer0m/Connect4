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

        // Collect the moves that don't lose immediately, and if every move loses, play any legal column.
        ulong safeMoves = position.PossibleNonLosingMoves();
        List<int> candidates = [];
        foreach (int col in ColumnOrder)
        {
            if ((safeMoves & Position.ColumnMask(col)) != 0)
                candidates.Add(col);
        }
        if (candidates.Count == 0)
            return ColumnOrder.First(position.CanPlay);
        if (candidates.Count == 1)
            return candidates[0];

        // Use the fast win/loss search to find the winning moves, and only consider those if there are any.
        List<int> winningColumns = candidates.Where(col => ScoreMove(position, col, true) > 0).ToList();
        if (winningColumns.Count == 1)
            return winningColumns[0];
        if (winningColumns.Count > 1)
            candidates = winningColumns;

        // Score the remaining candidates exactly and pick the best, which is the fastest win.
        int bestColumn = candidates[0];
        int bestScore = int.MinValue;
        foreach (int col in candidates)
        {
            int score = ScoreMove(position, col, false);
            if (score > bestScore)
            {
                bestScore = score;
                bestColumn = col;
            }
        }
        return bestColumn;
    }

    /// <summary>
    /// Scores playing a column for the player to move, using the fast win/loss search if weak is true.
    /// </summary>
    /// <param name="position">The position to move from.</param>
    /// <param name="col">The zero-based column to play.</param>
    /// <param name="weak">If true, only work out whether the move wins, draws or loses.</param>
    private int ScoreMove(Position position, int col, bool weak)
    {
        Position child = position.Clone();
        child.Play(col);
        return -Solver.Solve(child, weak);
    }
}