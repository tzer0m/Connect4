namespace Connect4.Engine;

/// <summary>
/// Scores Connect 4 positions exactly using negamax search with alpha-beta pruning.
/// </summary>
/// <param name="tableSize">The number of slots in the transposition table.</param>
public sealed class Solver(int tableSize)
{
    /// <summary>
    /// The lowest score a stored table entry can represent, used to squeeze scores into a byte.
    /// </summary>
    private const int MinScore = -(Position.Width * Position.Height) / 2 + 3;

    /// <summary>
    /// The order columns are tried in, centre first, because centre moves are usually the strongest.
    /// </summary>
    private readonly int[] ColumnOrder = [3, 2, 4, 1, 5, 0, 6];

    /// <summary>
    /// The cache of positions that have already been solved.
    /// </summary>
    private readonly TranspositionTable Table = new(tableSize);

    /// <summary>
    /// The number of positions searched so far.
    /// </summary>
    public long NodeCount { get; private set; }

    /// <summary>
    /// Scores a position for the player to move: positive is a win (bigger means sooner), 0 is a draw and negative is a loss (bigger magnitude means sooner).
    /// </summary>
    /// <param name="position">The position to score.</param>
    /// <param name="weak">If true, only work out whether the player to move wins, draws or loses, which is much faster.</param>
    public int Solve(Position position, bool weak = false)
    {
        // An immediate win is the best possible score.
        if (position.CanWinNext())
            return (Position.Width * Position.Height + 1 - position.Moves) / 2;

        // The score can't be lower than the fastest possible loss or higher than the fastest possible win.
        int min = weak ? -1 : -(Position.Width * Position.Height - position.Moves) / 2;
        int max = weak ? 1 : (Position.Width * Position.Height + 1 - position.Moves) / 2;

        // Narrow the range by repeatedly asking "is the score above this guess?".
        while (min < max)
        {
            int guess = min + (max - min) / 2;

            // Guess nearer to 0 first, as most searches are quicker there.
            if (guess <= 0 && min / 2 < guess)
                guess = min / 2;
            else if (guess >= 0 && max / 2 > guess)
                guess = max / 2;

            // Ask the negamax search to prove the score is above the guess, or return a bound.
            int result = Negamax(position, guess, guess + 1);
            if (result <= guess)
                max = result;
            else
                min = result;
        }
        return min;
    }

    /// <summary>
    /// Scores a position for the player to move, only exactly if the score is between alpha and beta, otherwise it returns a bound.
    /// </summary>
    /// <param name="position">The position to score.</param>
    /// <param name="alpha">The lowest score we care about.</param>
    /// <param name="beta">The highest score we care about.</param>
    private int Negamax(Position position, int alpha, int beta)
    {
        NodeCount++;

        // If every move loses, the opponent wins with their next stone.
        ulong next = position.PossibleNonLosingMoves();
        if (next == 0)
            return -(Position.Width * Position.Height - position.Moves) / 2;

        // Two cells left with no win possible is a draw.
        if (position.Moves >= Position.Width * Position.Height - 2)
            return 0;

        // We can't lose sooner than the opponent's next win, so raise the lower bound.
        int min = -(Position.Width * Position.Height - 2 - position.Moves) / 2;
        if (alpha < min)
        {
            alpha = min;
            if (alpha >= beta)
                return alpha;
        }

        // We can't win sooner than our next-but-one move, so lower the upper bound, or use the one from the table.
        int max = (Position.Width * Position.Height - 1 - position.Moves) / 2;
        byte stored = Table.Get(position.Key());
        if (stored != 0)
            max = stored + MinScore - 1;
        if (beta > max)
        {
            beta = max;
            if (alpha >= beta)
                return beta;
        }

        // Sort the possible columns so the most promising is last, with ties going to the centre.
        Span<int> columns = stackalloc int[Position.Width];
        Span<int> scores = stackalloc int[Position.Width];
        int count = 0;
        for (int i = Position.Width - 1; i >= 0; i--)
        {
            int col = ColumnOrder[i];
            ulong move = next & Position.ColumnMask(col);
            if (move == 0)
                continue;
            int score = position.MoveScore(move);
            int slot = count++;
            while (slot > 0 && scores[slot - 1] > score)
            {
                columns[slot] = columns[slot - 1];
                scores[slot] = scores[slot - 1];
                slot--;
            }
            columns[slot] = col;
            scores[slot] = score;
        }

        // Try each column, best first, assuming the opponent replies as well as they can.
        while (count > 0)
        {
            Position child = position.Clone();
            child.Play(columns[--count]);
            int score = -Negamax(child, -beta, -alpha);

            // The opponent would never let us reach this position, so stop searching it.
            if (score >= beta)
                return score;
            if (score > alpha)
                alpha = score;
        }

        // Remember the best score we found as an upper bound for this position.
        Table.Put(position.Key(), (byte)(alpha - MinScore + 1));
        return alpha;
    }
}