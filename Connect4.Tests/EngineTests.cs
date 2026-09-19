using Connect4.Engine;

namespace Connect4.Tests;

/// <summary>
/// Tests for the engine's choice of move.
/// </summary>
public class EngineTests
{
    /// <summary>
    /// The engine being tested, shared between tests so its table gets reused.
    /// </summary>
    private readonly Connect4Engine Engine = new(1000003);

    /// <summary>
    /// The engine opens in the centre column.
    /// </summary>
    [Test]
    public void GetBestMoveEmptyBoardPlaysTheCentreColumn()
    {
        Assert.That(Engine.GetBestMove(new Position()), Is.EqualTo(3));
    }

    /// <summary>
    /// The engine takes a win when it has one.
    /// </summary>
    [Test]
    public void GetBestMoveImmediateWinTakesIt()
    {
        Assert.That(Engine.GetBestMove(TestPositions.FromMoves("121212")), Is.EqualTo(0));
    }

    /// <summary>
    /// The engine blocks the opponent's threat when it can't win itself.
    /// </summary>
    [Test]
    public void GetBestMoveOpponentThreatensToWinBlocksIt()
    {
        Assert.That(Engine.GetBestMove(TestPositions.FromMoves("17273")), Is.EqualTo(3));
    }

    /// <summary>
    /// Whatever the position, the engine only ever chooses a column that can be played.
    /// </summary>
    [Test]
    public void GetBestMoveOnRandomPositionsReturnsAPlayableColumn()
    {
        Random random = new(7);
        for (int i = 0; i < 40; i++)
        {
            string? moves = TestPositions.RandomMoves(random, 26);
            if (moves == null)
                continue;
            Position position = TestPositions.FromMoves(moves);
            int col = Engine.GetBestMove(position);
            Assert.That(position.CanPlay(col), Is.True, $"Unplayable column {col + 1} for moves {moves}");
        }
    }
}