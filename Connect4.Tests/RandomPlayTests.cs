using Connect4.Engine;
using System.Diagnostics;

namespace Connect4.Tests;

/// <summary>
/// Plays the engine against a random opponent to check it always wins and to measure how long it takes to move.
/// </summary>
public class RandomPlayTests
{
    /// <summary>
    /// The longest a single engine move may take, in milliseconds, before the test fails.
    /// </summary>
    private const long MaxMoveMilliseconds = 30000;

    /// <summary>
    /// The engine moves first against an opponent playing random columns, and should win every game, with timings reported.
    /// </summary>
    /// <param name="games">The number of games to play.</param>
    [TestCase(50)]
    [Repeat(5)]
    [Category("Slow")]
    public void EngineMovingFirstBeatsARandomOpponent(int games)
    {
        // Pick a new seed each run and report it, so a failing run can be replayed by using that seed here.
        int seed = Random.Shared.Next();
        Random random = new(seed);
        Connect4Engine engine = new();
        List<long> moveTimes = [];
        List<string> lostGames = [];
        int totalMoves = 0;
        for (int game = 0; game < games; game++)
        {
            // Play one game, timing each engine move.
            Position position = new();
            string moves = "";
            bool engineWon = false;
            while (position.Moves < Position.Width * Position.Height)
            {
                bool engineToMove = position.Moves % 2 == 0;
                int col;
                if (engineToMove)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    col = engine.GetBestMove(position);
                    moveTimes.Add(stopwatch.ElapsedMilliseconds);
                    Assert.That(position.CanPlay(col), Is.True, $"Unplayable column {col + 1} with seed {seed} in game {game + 1}: {moves}");
                }
                else
                {
                    col = RandomColumn(position, random);
                }
                bool wins = position.IsWinningMove(col);
                position.Play(col);
                moves += col + 1;
                if (wins)
                {
                    engineWon = engineToMove;
                    break;
                }
            }
            totalMoves += moves.Length;
            if (!engineWon)
                lostGames.Add(moves);
        }

        // Report the results and timings.
        long[] sorted = [.. moveTimes.Order()];
        long ninetyFifth = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * 0.95))];
        TestContext.Out.WriteLine($"Seed: {seed}");
        TestContext.Out.WriteLine($"Games: {games}, engine wins: {games - lostGames.Count}, average game length: {(double)totalMoves / games:F1} moves");
        TestContext.Out.WriteLine($"Engine moves: {sorted.Length}, average {sorted.Average():F1} ms, median {sorted[sorted.Length / 2]} ms, 95th percentile {ninetyFifth} ms, slowest {sorted[^1]} ms");
        Assert.Multiple(() =>
        {
            Assert.That(lostGames, Is.Empty, $"The engine failed to win with seed {seed}: {string.Join(", ", lostGames)}");
            Assert.That(sorted[^1], Is.LessThan(MaxMoveMilliseconds), "An engine move took too long");
        });
    }

    /// <summary>
    /// Returns a random column that can be played.
    /// </summary>
    /// <param name="position">The current position.</param>
    /// <param name="random">The source of randomness.</param>
    private static int RandomColumn(Position position, Random random)
    {
        List<int> options = [];
        for (int col = 0; col < Position.Width; col++)
        {
            if (position.CanPlay(col))
                options.Add(col);
        }
        return options[random.Next(options.Count)];
    }
}