using Connect4.Engine;

namespace Connect4.Tests;

/// <summary>
/// Tests that the solver scores positions correctly.
/// </summary>
public class SolverTests
{
    /// <summary>
    /// The solver being tested, shared between tests so its table gets reused.
    /// </summary>
    private readonly Solver Solver = new(1000003);

    /// <summary>
    /// The simple solver the real solver is checked against.
    /// </summary>
    private readonly ReferenceSolver Reference = new();

    /// <summary>
    /// A player who can win immediately gets the best possible score.
    /// </summary>
    [Test]
    public void SolveImmediateWinReturnsTheBestScore()
    {
        Position position = TestPositions.FromMoves("121212");
        Assert.That(Solver.Solve(position), Is.EqualTo(18));
    }

    /// <summary>
    /// A player facing two winning threats has lost, so the score is negative.
    /// </summary>
    [Test]
    public void SolveFacingTwoThreatsReturnsALoss()
    {
        Position position = TestPositions.FromMoves("27374");
        Assert.That(Solver.Solve(position), Is.LessThan(0));
    }

    /// <summary>
    /// Whether the player to move wins, draws or loses matches the reference solver on random late-game positions.
    /// </summary>
    [Test]
    public void SolveAgreesWithTheReferenceOnRandomPositions()
    {
        Random random = new(1234);
        int checkedCount = 0;
        for (int i = 0; i < 150; i++)
        {
            string? moves = TestPositions.RandomMoves(random, 30);
            if (moves == null)
                continue;
            Position position = TestPositions.FromMoves(moves);
            int expected = Reference.Solve(moves);
            Assert.Multiple(() =>
            {
                Assert.That(Math.Sign(Solver.Solve(position)), Is.EqualTo(expected), $"Full search disagrees for moves {moves}");
                Assert.That(Math.Sign(Solver.Solve(position, true)), Is.EqualTo(expected), $"Win/loss search disagrees for moves {moves}");
            });
            checkedCount++;
        }
        Assert.That(checkedCount, Is.GreaterThan(100));
    }

    /// <summary>
    /// A position and its left-right mirror image have the same score.
    /// </summary>
    [Test]
    public void SolveMirroredPositionScoresTheSame()
    {
        Random random = new(99);
        for (int i = 0; i < 40; i++)
        {
            string? moves = TestPositions.RandomMoves(random, 28);
            if (moves == null)
                continue;
            int score = Solver.Solve(TestPositions.FromMoves(moves));
            int mirroredScore = Solver.Solve(TestPositions.FromMoves(TestPositions.Mirror(moves)));
            Assert.That(mirroredScore, Is.EqualTo(score), $"Mirror differs for moves {moves}");
        }
    }
}