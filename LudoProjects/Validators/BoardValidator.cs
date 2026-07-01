using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Validators;

public static class BoardValidator
{
    public static ICell GetRequiredCell(IBoard board, Position position)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (position.Row < 0 || position.Row >= board.Cells.GetLength(0) ||
            position.Column < 0 ||
            position.Column >= board.Cells.GetLength(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position ({position.Row}, {position.Column}) " +
                "is outside the board.");
        }

        Cell? cell = board.Cells[
                position.Row,
                position.Column];

        if (cell is null)
        {
            throw new InvalidOperationException(
                $"Cell ({position.Row}, {position.Column}) " +
                "has not been initialized.");
        }

        return cell;
    }
}