namespace Connect4.Web;

/// <summary>
/// A request for the computer's next move.
/// </summary>
/// <param name="Moves">The moves played so far as 1-based column digits, with the computer moving first, such as "4453".</param>
public record MoveRequest(string Moves);