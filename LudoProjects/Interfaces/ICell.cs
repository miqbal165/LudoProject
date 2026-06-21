using LudoProjects.Enums;
using LudoProjects.Models;

namespace LudoProjects.Interfaces;

public interface ICell
{
    Position Position { get; }
    CellType Type { get; }
    Color? Color { get; }
    IReadOnlyList<IPawn> OccupyingPawns { get; }
}