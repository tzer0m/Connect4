using Connect4.Engine;
using System.Numerics;

namespace Connect4.Tests;

/// <summary>
/// Tests for the board rules in the position class.
/// </summary>
public class PositionTests
{
    /// <summary>
    /// A new board is empty and every column can be played.
    /// </summary>
    [Test]
    public void NewPositionIsEmpty()
    {
        Position position = new();
        Assert.That(position.Moves, Is.EqualTo(0));
        for (int col = 0; col < Position.Width; col++)
        {
            Assert.That(position.CanPlay(col), Is.True);
            for (int row = 0; row < Position.Height; row++)
            {
                Assert.That(position.GetCell(col, row), Is.EqualTo(0));
            }
        }
    }

    /// <summary>
    /// Columns outside the board can't be played.
    /// </summary>
    /// <param name="col">The column to check.</param>
    [TestCase(-1)]
    [TestCase(7)]
    [TestCase(100)]
    public void CanPlayColumnOutsideTheBoardReturnsFalse(int col)
    {
        Position position = new();
        Assert.That(position.CanPlay(col), Is.False);
    }

    /// <summary>
    /// A column with six stones is full, and the other columns are unaffected.
    /// </summary>
    [Test]
    public void CanPlayFullColumnReturnsFalse()
    {
        Position position = TestPositions.FromMoves("111111");
        Assert.Multiple(() =>
        {
            Assert.That(position.CanPlay(0), Is.False);
            Assert.That(position.CanPlay(1), Is.True);
        });
    }

    /// <summary>
    /// Stones fall to the bottom and the players alternate, with player 1 first.
    /// </summary>
    [Test]
    public void PlayPlacesStonesFromTheBottomAlternatingPlayers()
    {
        Position position = TestPositions.FromMoves("443");
        Assert.Multiple(() =>
        {
            Assert.That(position.Moves, Is.EqualTo(3));
            Assert.That(position.GetCell(3, 0), Is.EqualTo(1));
            Assert.That(position.GetCell(3, 1), Is.EqualTo(2));
            Assert.That(position.GetCell(2, 0), Is.EqualTo(1));
            Assert.That(position.GetCell(2, 1), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// A move that completes four in a row wins, in every direction.
    /// </summary>
    /// <param name="moves">The moves played so far, as 1-based columns.</param>
    /// <param name="col">The zero-based column to try.</param>
    /// <param name="expected">Whether playing the column should win.</param>
    [TestCase("121212", 0, true)]
    [TestCase("121212", 3, false)]
    [TestCase("172737", 3, true)]
    [TestCase("172737", 6, false)]
    [TestCase("1223433447", 3, true)]
    [TestCase("7665455441", 3, true)]
    [TestCase("17273", 6, false)]
    public void IsWinningMoveDetectsFourInARow(string moves, int col, bool expected)
    {
        Position position = TestPositions.FromMoves(moves);
        Assert.That(position.IsWinningMove(col), Is.EqualTo(expected));
    }

    /// <summary>
    /// Checking for a winning move doesn't change the position.
    /// </summary>
    [Test]
    public void IsWinningMoveDoesNotChangeThePosition()
    {
        Position position = TestPositions.FromMoves("121212");
        ulong key = position.Key();
        position.IsWinningMove(0);
        Assert.Multiple(() =>
        {
            Assert.That(position.Key(), Is.EqualTo(key));
            Assert.That(position.Moves, Is.EqualTo(6));
        });
    }

    /// <summary>
    /// The same stones reached in a different move order give the same key.
    /// </summary>
    [Test]
    public void KeySameStonesInADifferentOrderIsTheSame()
    {
        Assert.That(TestPositions.FromMoves("3412").Key(), Is.EqualTo(TestPositions.FromMoves("1234").Key()));
    }

    /// <summary>
    /// The same cells filled by different players give different keys.
    /// </summary>
    [Test]
    public void KeyDifferentOwnersIsDifferent()
    {
        Assert.That(TestPositions.FromMoves("21").Key(), Is.Not.EqualTo(TestPositions.FromMoves("12").Key()));
    }

    /// <summary>
    /// Playing on a clone leaves the original unchanged.
    /// </summary>
    [Test]
    public void CloneIsIndependentOfTheOriginal()
    {
        Position original = TestPositions.FromMoves("44");
        Position copy = original.Clone();
        copy.Play(3);
        Assert.Multiple(() =>
        {
            Assert.That(original.Moves, Is.EqualTo(2));
            Assert.That(copy.Moves, Is.EqualTo(3));
            Assert.That(original.Key(), Is.Not.EqualTo(copy.Key()));
        });
    }

    /// <summary>
    /// The player to move can win next only when they have three in a row with an open end.
    /// </summary>
    /// <param name="moves">The moves played so far, as 1-based columns.</param>
    /// <param name="expected">Whether the player to move can win next.</param>
    [TestCase("", false)]
    [TestCase("172737", true)]
    [TestCase("17273", false)]
    public void CanWinNextDetectsAnImmediateWin(string moves, bool expected)
    {
        Assert.That(TestPositions.FromMoves(moves).CanWinNext(), Is.EqualTo(expected));
    }

    /// <summary>
    /// With no threats on the board, every column is a safe move.
    /// </summary>
    [Test]
    public void PossibleNonLosingMovesEmptyBoardAllSevenColumns()
    {
        Position position = new();
        Assert.That(BitOperations.PopCount(position.PossibleNonLosingMoves()), Is.EqualTo(7));
    }

    /// <summary>
    /// When the opponent threatens to win, the only safe move is to block it.
    /// </summary>
    [Test]
    public void PossibleNonLosingMovesSingleThreatOnlyTheBlock()
    {
        Position position = TestPositions.FromMoves("17273");
        Assert.That(position.PossibleNonLosingMoves(), Is.EqualTo(1UL << 21));
    }

    /// <summary>
    /// When the opponent threatens to win in two places, no move is safe.
    /// </summary>
    [Test]
    public void PossibleNonLosingMovesTwoThreatsNoSafeMove()
    {
        Position position = TestPositions.FromMoves("27374");
        Assert.That(position.PossibleNonLosingMoves(), Is.EqualTo(0UL));
    }
}