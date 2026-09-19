using Connect4.Engine;

namespace Connect4.Tests;

/// <summary>
/// Tests that the opening book is complete and only contains winning moves.
/// </summary>
public class OpeningBookTests
{
    /// <summary>
    /// The number of computer moves the book covers.
    /// </summary>
    private const int Levels = 4;

    /// <summary>
    /// Every position the computer can face in the covered moves has an entry, and there are no spare entries.
    /// </summary>
    [Test]
    public void BookCoversEveryPositionInTheOpening()
    {
        List<Position> positions = BookPositions();
        Assert.That(positions, Has.Count.EqualTo(OpeningBook.Entries.Count));
    }

    /// <summary>
    /// Every entry is a column that can be played.
    /// </summary>
    [Test]
    public void BookEveryEntryIsPlayable()
    {
        foreach (Position position in BookPositions())
        {
            int col = OpeningBook.Entries[position.Key()];
            Assert.That(position.CanPlay(col), Is.True, $"Unplayable column {col + 1} after {position.Moves} moves");
        }
    }

    /// <summary>
    /// Every entry from six moves on wins, either straight away or by leaving the opponent in a lost position.
    /// </summary>
    [Test]
    [Category("Slow")]
    public void BookEntriesFromSixMovesOnAreWinningMoves()
    {
        Solver solver = new(8388593);
        foreach (Position position in BookPositions().Where(p => p.Moves >= 6))
        {
            AssertWins(position, solver);
        }
    }

    /// <summary>
    /// Every entry for the earliest moves wins, which is slow because these are the hardest positions to solve.
    /// </summary>
    [Test]
    public void BookEarlyEntriesAreWinningMoves()
    {
        Solver solver = new(8388593);
        foreach (Position position in BookPositions().Where(p => p.Moves > 0 && p.Moves < 6))
        {
            AssertWins(position, solver);
        }
    }

    /// <summary>
    /// Checks that the book's move for a position wins.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    /// <param name="solver">The solver to check with.</param>
    private static void AssertWins(Position position, Solver solver)
    {
        int col = OpeningBook.Entries[position.Key()];
        bool wins = position.IsWinningMove(col);
        if (!wins)
        {
            Position child = position.Clone();
            child.Play(col);
            wins = solver.Solve(child, true) < 0;
        }
        Assert.That(wins, Is.True, $"The entry after {position.Moves} moves doesn't win");
    }

    /// <summary>
    /// Returns every position the book should cover, found by following the book's moves and every possible reply.
    /// </summary>
    private static List<Position> BookPositions()
    {
        List<Position> positions = [];
        Collect(new Position(), Levels, [], positions);
        return positions;
    }

    /// <summary>
    /// Adds a position to the list, then does the same for every position that can follow it.
    /// </summary>
    /// <param name="position">The position with the computer to move.</param>
    /// <param name="levels">The number of computer moves still to cover, including this one.</param>
    /// <param name="seen">The keys of the positions already collected.</param>
    /// <param name="positions">The list being filled.</param>
    private static void Collect(Position position, int levels, HashSet<ulong> seen, List<Position> positions)
    {
        if (!seen.Add(position.Key()))
            return;
        positions.Add(position);
        Assert.That(OpeningBook.Entries.TryGetValue(position.Key(), out int col), Is.True, $"Missing entry after {position.Moves} moves");
        if (levels == 1)
            return;
        Position afterMove = position.Clone();
        afterMove.Play(col);
        for (int reply = 0; reply < Position.Width; reply++)
        {
            if (!afterMove.CanPlay(reply))
                continue;
            Position next = afterMove.Clone();
            next.Play(reply);
            Collect(next, levels - 1, seen, positions);
        }
    }
}