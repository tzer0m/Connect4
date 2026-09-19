using Connect4.Cli;

// Generate the opening book if requested, e.g. -GenerateOpeningBook 3.
int bookIndex = Array.FindIndex(args, arg => string.Equals(arg, "-GenerateOpeningBook", StringComparison.OrdinalIgnoreCase));
if (bookIndex >= 0)
{
    int depth = bookIndex + 1 < args.Length && int.TryParse(args[bookIndex + 1], out int parsedDepth) ? parsedDepth : 3;
    OpeningBookGenerator generator = new();
    generator.Generate(depth);
    return;
}

// Otherwise play a game against the computer.
ConsoleGame game = new();
game.Play();