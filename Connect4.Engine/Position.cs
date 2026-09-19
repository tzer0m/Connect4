using System.Numerics;

namespace Connect4.Engine;

/// <summary>
/// A Connect 4 board stored as a pair of bitboards. Each column uses 7 bits (6 cells plus an empty sentinel bit on top), bit 0 of each column being its bottom cell.
/// </summary>
public sealed class Position
{
    /// <summary>
    /// The number of columns.
    /// </summary>
    public const int Width = 7;

    /// <summary>
    /// The number of rows.
    /// </summary>
    public const int Height = 6;

    /// <summary>
    /// A mask with a 1 in the bottom cell of every column, used to find the next empty cell in a column.
    /// </summary>
    private const ulong BottomMask = ((1UL << (Width * (Height + 1))) - 1) / ((1UL << (Height + 1)) - 1);

    /// <summary>
    /// A mask covering every playable cell on the board, used to remove anything that isnt a real cell
    /// </summary>
    private const ulong BoardMask = BottomMask * ((1UL << Height) - 1);

    /// <summary>
    /// The stones of the player to move.
    /// </summary>
    private ulong Current;

    /// <summary>
    /// The stones of both players.
    /// </summary>
    private ulong Mask;

    /// <summary>
    /// The number of moves played so far.
    /// </summary>
    public int Moves { get; private set; }

    /// <summary>
    /// Returns true if the column is on the board and not full. Checks the column is a real column, then checks the top cell of the column is empty.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    public bool CanPlay(int col)
    {
        return col >= 0 && col < Width && (Mask & TopMask(col)) == 0;
    }

    /// <summary>
    /// Drops a stone for the player to move into the column. The column must be playable.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    public void Play(int col)
    {
        // Swaps the current player.
        Current ^= Mask;

        // Adds a stone to the column and iterates the moves.
        Mask |= Mask + BottomOf(col);
        Moves++;
    }

    /// <summary>
    /// Returns true if playing the column wins the game for the player to move.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    public bool IsWinningMove(int col)
    {
        // Checks if the column is playable.
        ulong possible = (Mask + BottomMask) & BoardMask;

        // Returns true if the player to move wins after playing the column and playing in the column is valid, and false otherwise.
        return (ComputeWinningPositions(Current, Mask) & possible & ColumnMask(col)) != 0;
    }

    /// <summary>
    /// Returns the contents of a cell: 0 for empty, 1 for the first player, 2 for the second player.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    /// <param name="row">The zero-based row index, counted from the bottom.</param>
    public int GetCell(int col, int row)
    {
        // Find the bit corresponding to the cell.
        ulong bit = 1UL << (col * (Height + 1) + row);

        // Return 0 if the cell is empty.
        if ((Mask & bit) == 0)
            return 0;

        // Return 1 if the cell belongs to the first player, 2 if it belongs to the second player.
        bool isCurrentPlayer = (Current & bit) != 0;
        return isCurrentPlayer == (Moves % 2 == 0) ? 1 : 2;
    }

    /// <summary>
    /// Returns an independent copy of this position.
    /// </summary>
    public Position Clone()
    {
        return new Position { Current = Current, Mask = Mask, Moves = Moves };
    }

    /// <summary>
    /// Returns a number that uniquely identifies this position, used as the transposition table key.
    /// </summary>
    public ulong Key()
    {
        return Current + Mask;
    }

    /// <summary>
    /// Returns true if the player to move can win on their next move.
    /// </summary>
    public bool CanWinNext()
    {
        ulong possible = (Mask + BottomMask) & BoardMask;
        return (ComputeWinningPositions(Current, Mask) & possible) != 0;
    }

    /// <summary>
    /// Returns a mask of the cells the player to move can play without losing straight away, or 0 if every move loses.
    /// </summary>
    public ulong PossibleNonLosingMoves()
    {
        // The cell a stone would land on in each column.
        ulong possible = (Mask + BottomMask) & BoardMask;

        // Cells where the opponent would win if they played there.
        ulong opponentWins = ComputeWinningPositions(Current ^ Mask, Mask);

        // If the opponent threatens to win, we must block it. With two threats we can only block one, so every move loses.
        ulong forced = possible & opponentWins;
        if (forced != 0)
        {
            if ((forced & (forced - 1)) != 0)
                return 0;
            possible = forced;
        }

        // Never play directly underneath a cell where the opponent would win, as it lets them play there.
        return possible & ~(opponentWins >> 1);
    }

    /// <summary>
    /// Returns the number of winning cells the player to move would have after playing the move, used to try the most promising moves first.
    /// </summary>
    /// <param name="move">A mask with a single bit set at the cell to play.</param>
    public int MoveScore(ulong move)
    {
        return BitOperations.PopCount(ComputeWinningPositions(Current | move, Mask | move));
    }

    /// <summary>
    /// Finds every empty cell that would complete four in a row for the given stones.
    /// </summary>
    /// <param name="position">The stones to check.</param>
    /// <param name="occupied">The stones of both players.</param>
    private static ulong ComputeWinningPositions(ulong position, ulong occupied)
    {
        const int horizontalShift = Height + 1;
        const int downDiagonalShift = Height;
        const int upDiagonalShift = Height + 2;

        // Vertical: three stones directly below.
        ulong verticalWins = (position << 1) & (position << 2) & (position << 3);

        // Horizontal: stones on either side of the cell.
        ulong horizontalTwoLower = (position << horizontalShift) & (position << (2 * horizontalShift));
        ulong horizontalTwoHigher = (position >> horizontalShift) & (position >> (2 * horizontalShift));
        ulong horizontalWins = (horizontalTwoLower & (position << (3 * horizontalShift))) | (horizontalTwoLower & (position >> horizontalShift)) | (horizontalTwoHigher & (position << horizontalShift)) | (horizontalTwoHigher & (position >> (3 * horizontalShift)));

        // Diagonal down-right.
        ulong downDiagonalTwoLower = (position << downDiagonalShift) & (position << (2 * downDiagonalShift));
        ulong downDiagonalTwoHigher = (position >> downDiagonalShift) & (position >> (2 * downDiagonalShift));
        ulong downDiagonalWins = (downDiagonalTwoLower & (position << (3 * downDiagonalShift))) | (downDiagonalTwoLower & (position >> downDiagonalShift)) | (downDiagonalTwoHigher & (position << downDiagonalShift)) | (downDiagonalTwoHigher & (position >> (3 * downDiagonalShift)));

        // Diagonal up-right.
        ulong upDiagonalTwoLower = (position << upDiagonalShift) & (position << (2 * upDiagonalShift));
        ulong upDiagonalTwoHigher = (position >> upDiagonalShift) & (position >> (2 * upDiagonalShift));
        ulong upDiagonalWins = (upDiagonalTwoLower & (position << (3 * upDiagonalShift))) | (upDiagonalTwoLower & (position >> upDiagonalShift)) | (upDiagonalTwoHigher & (position << upDiagonalShift)) | (upDiagonalTwoHigher & (position >> (3 * upDiagonalShift)));

        // Combine every direction and keep only empty cells on the board.
        ulong allWins = verticalWins | horizontalWins | downDiagonalWins | upDiagonalWins;
        return allWins & (BoardMask ^ occupied);
    }

    /// <summary>
    /// Returns a mask of the top cell of a column.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    private static ulong TopMask(int col)
    {
        return 1UL << (Height - 1 + col * (Height + 1));
    }

    /// <summary>
    /// Returns a mask of the bottom cell of a column.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    private static ulong BottomOf(int col)
    {
        return 1UL << (col * (Height + 1));
    }

    /// <summary>
    /// Returns a mask of every cell in a column.
    /// </summary>
    /// <param name="col">The zero-based column index.</param>
    public static ulong ColumnMask(int col)
    {
        return ((1UL << Height) - 1) << (col * (Height + 1));
    }
}