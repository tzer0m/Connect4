using Connect4.Engine;
using System.Diagnostics;

namespace Connect4.Cli;

/// <summary>
/// Plays a game of Connect 4 in the console, with the computer moving first.
/// </summary>
public sealed class ConsoleGame
{
    /// <summary>
    /// The engine that chooses the computer's moves, kept for the whole game so its table is reused.
    /// </summary>
    private readonly Connect4Engine Engine = new();

    /// <summary>
    /// Plays one game, alternating between the computer and the player until someone wins or the board is full.
    /// </summary>
    public void Play()
    {
        Position position = new();
        Console.WriteLine("Connect 4: the computer is X and moves first, you are O. Enter a column from 1 to 7.");
        Draw(position);
        while (position.Moves < Position.Width * Position.Height)
        {
            // Get the next move from the computer or the player.
            bool computerToMove = position.Moves % 2 == 0;
            int col;
            if (computerToMove)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                col = Engine.GetBestMove(position);
                Console.WriteLine($"Computer plays column {col + 1} ({stopwatch.ElapsedMilliseconds} ms).");
            }
            else
            {
                col = ReadColumn(position);
                if (col < 0)
                    return;
            }

            // Play it and check whether it wins.
            bool wins = position.IsWinningMove(col);
            position.Play(col);
            Draw(position);
            if (wins)
            {
                Console.WriteLine(computerToMove ? "The computer wins." : "You win!");
                return;
            }
        }
        Console.WriteLine("It's a draw.");
    }

    /// <summary>
    /// Asks the player for a column until they enter a playable one, and returns it zero-based, or -1 if the input has ended.
    /// </summary>
    /// <param name="position">The current position.</param>
    private static int ReadColumn(Position position)
    {
        while (true)
        {
            Console.Write("Your move (1-7): ");
            string? input = Console.ReadLine();
            if (input == null)
                return -1;
            if (int.TryParse(input, out int number) && position.CanPlay(number - 1))
                return number - 1;
            Console.WriteLine("That isn't a playable column.");
        }
    }

    /// <summary>
    /// Draws the board, with X for the computer and O for the player.
    /// </summary>
    /// <param name="position">The position to draw.</param>
    private static void Draw(Position position)
    {
        Console.WriteLine();
        for (int row = Position.Height - 1; row >= 0; row--)
        {
            string line = "";
            for (int col = 0; col < Position.Width; col++)
            {
                line += " " + ".XO"[position.GetCell(col, row)];
            }
            Console.WriteLine(line);
        }
        Console.WriteLine(" 1 2 3 4 5 6 7");
        Console.WriteLine();
    }
}