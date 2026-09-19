namespace Connect4.Web;

/// <summary>
/// The state of a game after the computer's move.
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// The game is still going and it's the player's turn.
    /// </summary>
    InProgress,

    /// <summary>
    /// The computer has won.
    /// </summary>
    ComputerWins,

    /// <summary>
    /// The player has won.
    /// </summary>
    PlayerWins,

    /// <summary>
    /// The board is full with no winner.
    /// </summary>
    Draw
}