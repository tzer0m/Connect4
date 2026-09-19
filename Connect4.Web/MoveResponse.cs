namespace Connect4.Web;

/// <summary>
/// The computer's reply to a request for its next move.
/// </summary>
/// <param name="Column">The zero-based column the computer played, or null if the game was already over.</param>
/// <param name="Milliseconds">How long the computer took to choose its move.</param>
/// <param name="Status">The state of the game after the move.</param>
public record MoveResponse(int? Column, long Milliseconds, GameStatus Status);