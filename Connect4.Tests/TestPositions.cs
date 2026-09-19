using Connect4.Engine;

namespace Connect4.Tests;

/// <summary>
/// Helpers for building positions in tests.
/// </summary>
public static class TestPositions
{
    /// <summary>
    /// Builds a position by playing the moves, given as 1-based column digits such as "4453".
    /// </summary>
    /// <param name="moves">The moves to play.</param>
    public static Position FromMoves(string moves)
    {
        Position position = new();
        foreach (char digit in moves)
        {
            position.Play(digit - '1');
        }
        return position;
    }

    /// <summary>
    /// Returns the left-right mirror image of a string of moves.
    /// </summary>
    /// <param name="moves">The moves to mirror, as 1-based column digits.</param>
    public static string Mirror(string moves)
    {
        return string.Concat(moves.Select(digit => (char)('8' - digit + '0')));
    }

    /// <summary>
    /// Plays random moves that don't end the game, returning them as 1-based column digits, or null if the game got stuck.
    /// </summary>
    /// <param name="random">The source of randomness.</param>
    /// <param name="count">The number of moves to play.</param>
    public static string? RandomMoves(Random random, int count)
    {
        Position position = new();
        string moves = "";
        for (int i = 0; i < count; i++)
        {
            List<int> options = [];
            for (int col = 0; col < Position.Width; col++)
            {
                if (position.CanPlay(col) && !position.IsWinningMove(col))
                    options.Add(col);
            }
            if (options.Count == 0)
                return null;
            int chosen = options[random.Next(options.Count)];
            position.Play(chosen);
            moves += chosen + 1;
        }
        return moves;
    }
}