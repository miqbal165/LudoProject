using LudoProjects.Enums;
using LudoProjects.Models;

namespace LudoProjects.Interfaces;

public interface IBoard
{
    ICell GetCell(Position position);
    IReadOnlyList<ICell> GetCellsByType(CellType type);
    Position GetStartPosition(Color color);
    IReadOnlyList<Position> GetBasePositions(Color color);
    IReadOnlyList<Position> GetHomeColumnPositions(Color color);
    Position GetCenterPosition();
    IReadOnlyList<Position> GetFullPath(Color color);
}