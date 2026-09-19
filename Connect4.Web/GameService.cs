using Connect4.Engine;
using System.Diagnostics;

namespace Connect4.Web;

/// <summary>
/// Replays a game from its moves and chooses the computer's reply, sharing one engine so its cache lasts between requests.
/// </summary>
public sealed class GameService
{
    /// <summary>
    /// The engine that chooses the computer's moves.
    /// </summary>
    private readonly Connect4Engine Engine = new();

    /// <summary>
    /// Makes sure only one request uses the engine at a time, as it isn't thread safe.
    /// </summary>
    private readonly Lock EngineLock = new();

    /// <summary>
    /// Replays the moves and returns the computer's reply, or the result of the game if it has already finished.
    /// </summary>
    /// <param name="moves">The moves played so far as 1-based column digits, with the computer moving first.</param>
    public MoveResponse PlayComputerMove(string moves)
    {
        // Replay the moves, rejecting anything that isn't legal.
        Position position = new();
        bool gameWon = false;
        bool computerMovedLast = false;
        foreach (char digit in moves)
        {
            if (gameWon)
                throw new BadHttpRequestException("Moves continue after the game has ended.");
            int col = digit - '1';
            if (col < 0 || col >= Position.Width || !position.CanPlay(col))
                throw new BadHttpRequestException($"Illegal move '{digit}' at move {position.Moves + 1}.");
            gameWon = position.IsWinningMove(col);
            computerMovedLast = position.Moves % 2 == 0;
            position.Play(col);
        }

        // Report the result if the game has already finished.
        if (gameWon)
            return new MoveResponse(null, 0, computerMovedLast ? GameStatus.ComputerWins : GameStatus.PlayerWins);
        if (position.Moves == Position.Width * Position.Height)
            return new MoveResponse(null, 0, GameStatus.Draw);

        // The computer moves first, so it's only the computer's turn after an even number of moves.
        if (position.Moves % 2 != 0)
            throw new BadHttpRequestException("It isn't the computer's turn.");

        // Choose the computer's move, timing it.
        Stopwatch stopwatch = Stopwatch.StartNew();
        int column;
        lock (EngineLock)
        {
            column = Engine.GetBestMove(position);
        }
        long milliseconds = stopwatch.ElapsedMilliseconds;

        // Work out the state of the game after the move.
        bool computerWins = position.IsWinningMove(column);
        position.Play(column);
        GameStatus status = computerWins ? GameStatus.ComputerWins : position.Moves == Position.Width * Position.Height ? GameStatus.Draw : GameStatus.InProgress;
        return new MoveResponse(column, milliseconds, status);
    }
}