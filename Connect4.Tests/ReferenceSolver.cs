namespace Connect4.Tests;

/// <summary>
/// A simple array-based solver used to check the real solver, favouring clarity over speed.
/// </summary>
public sealed class ReferenceSolver
{
    /// <summary>
    /// The board, indexed by column then row, holding 0 for empty, 1 or 2.
    /// </summary>
    private readonly int[,] Board = new int[7, 6];

    /// <summary>
    /// The number of stones in each column.
    /// </summary>
    private readonly int[] Heights = new int[7];

    /// <summary>
    /// The result of every position solved so far, kept between calls.
    /// </summary>
    private readonly Dictionary<ulong, int> Results = [];

    /// <summary>
    /// The number of stones on the board.
    /// </summary>
    private int StonesPlayed;

    /// <summary>
    /// Returns 1 if the player to move wins, 0 for a draw and -1 for a loss, given the moves played so far as 1-based columns.
    /// </summary>
    /// <param name="moves">The moves played so far.</param>
    public int Solve(string moves)
    {
        Array.Clear(Board);
        Array.Clear(Heights);
        StonesPlayed = 0;
        int player = 1;
        foreach (char digit in moves)
        {
            Drop(digit - '1', player);
            player = 3 - player;
        }
        return Search(player);
    }

    /// <summary>
    /// Returns the result for the player to move by trying every move, stopping early once a win is found.
    /// </summary>
    /// <param name="player">The player to move, 1 or 2.</param>
    private int Search(int player)
    {
        if (StonesPlayed == 42)
            return 0;
        ulong key = Key();
        if (Results.TryGetValue(key, out int known))
            return known;
        int best = -1;
        for (int col = 0; col < 7 && best < 1; col++)
        {
            if (Heights[col] == 6)
                continue;
            int row = Heights[col];
            Drop(col, player);
            int score = Wins(col, row, player) ? 1 : -Search(3 - player);
            Undo(col);
            if (score > best)
                best = score;
        }
        Results[key] = best;
        return best;
    }

    /// <summary>
    /// Puts a stone on top of a column.
    /// </summary>
    /// <param name="col">The zero-based column.</param>
    /// <param name="player">The player, 1 or 2.</param>
    private void Drop(int col, int player)
    {
        Board[col, Heights[col]] = player;
        Heights[col]++;
        StonesPlayed++;
    }

    /// <summary>
    /// Removes the top stone from a column.
    /// </summary>
    /// <param name="col">The zero-based column.</param>
    private void Undo(int col)
    {
        Heights[col]--;
        Board[col, Heights[col]] = 0;
        StonesPlayed--;
    }

    /// <summary>
    /// Returns true if the stone at the cell is part of four in a row.
    /// </summary>
    /// <param name="col">The zero-based column.</param>
    /// <param name="row">The zero-based row.</param>
    /// <param name="player">The player who owns the stone.</param>
    private bool Wins(int col, int row, int player)
    {
        int[,] directions = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };
        for (int d = 0; d < 4; d++)
        {
            int count = 1;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int x = col + sign * directions[d, 0];
                int y = row + sign * directions[d, 1];
                while (x >= 0 && x < 7 && y >= 0 && y < 6 && Board[x, y] == player)
                {
                    count++;
                    x += sign * directions[d, 0];
                    y += sign * directions[d, 1];
                }
            }
            if (count >= 4)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a number identifying the stones on the board, unique for each position.
    /// </summary>
    private ulong Key()
    {
        ulong key = 0;
        for (int col = 0; col < 7; col++)
        {
            ulong code = 1;
            for (int row = 0; row < Heights[col]; row++)
            {
                code = (code << 1) | (uint)(Board[col, row] - 1);
            }
            key |= code << (7 * col);
        }
        return key;
    }
}