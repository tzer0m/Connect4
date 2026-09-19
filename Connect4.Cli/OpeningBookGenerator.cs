using Connect4.Engine;
using System.Diagnostics;

namespace Connect4.Cli;

/// <summary>
/// Solves the computer's opening moves and writes them out as entries that can be pasted into the engine's opening book.
/// </summary>
public sealed class OpeningBookGenerator
{
    /// <summary>
    /// The file the generated entries are written to.
    /// </summary>
    private const string OutputFile = "opening-book.txt";

    /// <summary>
    /// The order columns are tried in, centre first.
    /// </summary>
    private readonly int[] ColumnOrder = [3, 2, 4, 1, 5, 0, 6];

    /// <summary>
    /// The solver used to find winning moves, kept between positions so its table is reused.
    /// </summary>
    private readonly Solver Solver = new(8388593);

    /// <summary>
    /// The keys of positions that already have an entry, so a position reached by two move orders is only solved once.
    /// </summary>
    private readonly HashSet<ulong> Done = [];

    /// <summary>
    /// Times how long the generation takes.
    /// </summary>
    private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// The number of entries written so far.
    /// </summary>
    private int EntryCount;

    /// <summary>
    /// The number of entries expected, used for progress messages.
    /// </summary>
    private int TotalEntries;

    /// <summary>
    /// Generates an entry for every position the computer can face in its first moves, as the first player.
    /// </summary>
    /// <param name="depth">The number of computer moves to cover, where 1 is just the first move.</param>
    public void Generate(int depth)
    {
        // Each computer move has 7 possible replies, so 1 + 7 + 49 + ... entries.
        TotalEntries = ((int)Math.Pow(7, depth) - 1) / 6;
        Console.WriteLine($"Generating up to {TotalEntries} entries, covering the computer's first {depth} moves.");
        File.WriteAllText(OutputFile, "// Opening book entries: position key = zero-based column for the computer to play.\n");
        Visit(new Position(), "", depth);
        Console.WriteLine($"Done in {Stopwatch.Elapsed:hh\\:mm\\:ss}. Written to {Path.GetFullPath(OutputFile)}");
    }

    /// <summary>
    /// Writes the entry for a position, then does the same for every position that can follow it.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    /// <param name="moves">The moves played so far, as 1-based columns, for the comment on the entry.</param>
    /// <param name="movesRemaining">The number of computer moves still to cover, including this one.</param>
    private void Visit(Position position, string moves, int movesRemaining)
    {
        // Skip positions already covered, as everything after them is covered too.
        if (!Done.Add(position.Key()))
            return;

        // Find the move and record it.
        int col = FindWinningColumn(position);
        EntryCount++;
        string line = $"[{position.Key()}UL] = {col}, // after moves \"{moves}\" play {col + 1}";
        File.AppendAllText(OutputFile, line + "\n");
        Console.WriteLine($"[{EntryCount}/{TotalEntries}] {Stopwatch.Elapsed:hh\\:mm\\:ss} {line}");
        if (movesRemaining <= 1)
            return;

        // Cover every reply the opponent could make.
        Position afterMove = position.Clone();
        afterMove.Play(col);
        for (int reply = 0; reply < Position.Width; reply++)
        {
            if (!afterMove.CanPlay(reply))
                continue;
            Position next = afterMove.Clone();
            next.Play(reply);
            Visit(next, moves + (col + 1) + (reply + 1), movesRemaining - 1);
        }
    }

    /// <summary>
    /// Returns the first column, centre first, that wins for the player to move.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    private int FindWinningColumn(Position position)
    {
        // The centre column is the known winning first move, so there's no need to solve the empty board.
        if (position.Moves == 0)
            return Position.Width / 2;

        // A column wins if it wins immediately, or leaves the opponent in a lost position.
        foreach (int col in ColumnOrder)
        {
            if (!position.CanPlay(col))
                continue;
            if (position.IsWinningMove(col))
                return col;
            Position child = position.Clone();
            child.Play(col);
            if (Solver.Solve(child, true) < 0)
                return col;
        }

        // The first player should always have a winning move, so warn and fall back to the centre-most column.
        Console.WriteLine("WARNING: no winning move found.");
        return ColumnOrder.First(position.CanPlay);
    }
}