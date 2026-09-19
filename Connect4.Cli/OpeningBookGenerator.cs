using Connect4.Engine;
using System.Diagnostics;

namespace Connect4.Cli;

/// <summary>
/// Solves the computer's opening moves in parallel and writes them out as entries that can be pasted into the engine's opening book.
/// </summary>
public sealed class OpeningBookGenerator
{
    /// <summary>
    /// The file the generated entries are written to.
    /// </summary>
    private const string OutputFile = "opening-book.txt";

    /// <summary>
    /// The number of slots in the transposition table given to each thread's solver.
    /// </summary>
    private const int TableSize = 8388593;

    /// <summary>
    /// The order columns are tried in, centre first.
    /// </summary>
    private readonly int[] ColumnOrder = [3, 2, 4, 1, 5, 0, 6];

    /// <summary>
    /// The keys of positions that already have an entry, so a position reached by two move orders is only solved once.
    /// </summary>
    private readonly HashSet<ulong> Done = [];

    /// <summary>
    /// Times how long the generation takes.
    /// </summary>
    private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// The number of entries solved so far.
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
        int threads = Environment.ProcessorCount;
        Console.WriteLine($"Generating up to {TotalEntries} entries, covering the computer's first {depth} moves, using {threads} threads.");
        File.WriteAllText(OutputFile, "// Opening book entries: position key = zero-based column for the computer to play.\n");
        ParallelOptions options = new() { MaxDegreeOfParallelism = threads };

        // Each level holds the positions where the computer is to move, starting with the empty board.
        List<(Position Position, string Moves)> level = [(new Position(), "")];
        Done.Add(level[0].Position.Key());
        for (int levelNumber = 1; levelNumber <= depth; levelNumber++)
        {
            // Solve every position in the level at the same time, giving each thread its own solver.
            List<(Position Position, string Moves)> positions = level;
            int[] columns = new int[positions.Count];
            Parallel.For(0, positions.Count, options, () => new Solver(TableSize), (int index, ParallelLoopState state, Solver solver) =>
            {
                columns[index] = FindWinningColumn(positions[index].Position, solver);
                int number = Interlocked.Increment(ref EntryCount);
                Console.WriteLine($"[{number}/{TotalEntries}] {Stopwatch.Elapsed:hh\\:mm\\:ss} {FormatEntry(positions[index].Position, positions[index].Moves, columns[index])}");
                return solver;
            }, (Solver solver) => { });

            // Write the level's entries in a fixed order, and work out the positions in the next level.
            List<string> lines = [];
            List<(Position Position, string Moves)> next = [];
            for (int i = 0; i < positions.Count; i++)
            {
                (Position position, string moves) = positions[i];
                int col = columns[i];
                lines.Add(FormatEntry(position, moves, col));
                if (levelNumber == depth)
                    continue;

                // Cover every reply the opponent could make.
                Position afterMove = position.Clone();
                afterMove.Play(col);
                for (int reply = 0; reply < Position.Width; reply++)
                {
                    if (!afterMove.CanPlay(reply))
                        continue;
                    Position child = afterMove.Clone();
                    child.Play(reply);

                    // Skip positions already covered.
                    if (Done.Add(child.Key()))
                        next.Add((child, moves + (col + 1) + (reply + 1)));
                }
            }
            File.AppendAllLines(OutputFile, lines);
            Console.WriteLine($"Level {levelNumber} of {depth} done at {Stopwatch.Elapsed:hh\\:mm\\:ss}.");
            level = next;
        }
        Console.WriteLine($"Done in {Stopwatch.Elapsed:hh\\:mm\\:ss}. Written to {Path.GetFullPath(OutputFile)}");
    }

    /// <summary>
    /// Formats an opening book entry as a line of code with a comment showing how the position was reached.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    /// <param name="moves">The moves played so far, as 1-based columns.</param>
    /// <param name="col">The zero-based column the computer should play.</param>
    private static string FormatEntry(Position position, string moves, int col)
    {
        return $"[{position.Key()}UL] = {col}, // after moves \"{moves}\" play {col + 1}";
    }

    /// <summary>
    /// Returns the first column, centre first, that wins for the player to move.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    /// <param name="solver">The solver to use, which must not be shared with another thread.</param>
    private int FindWinningColumn(Position position, Solver solver)
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
            if (solver.Solve(child, true) < 0)
                return col;
        }

        // The first player should always have a winning move, so warn and fall back to the centre-most column.
        Console.WriteLine("WARNING: no winning move found.");
        return ColumnOrder.First(position.CanPlay);
    }
}