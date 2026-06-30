using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public class Cell : ICell
{
    public Position Position { get; }
    public CellType Type { get; }
    public Color? Color { get; }
    public IReadOnlyList<IPawn> OccupyingPawns { get; }

    public Cell(
        Position position,
        CellType type,
        Color? color,
        IReadOnlyList<IPawn> occupyingPawns)
    {
        Position = position;
        Type = type;
        Color = color;
        OccupyingPawns = occupyingPawns.ToList().AsReadOnly();
    }
}