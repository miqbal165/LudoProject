using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Cell : ICell
{
    private readonly List<IPawn> _occupyingPawns = new();

    public Position Position { get; }
    public CellType Type { get; }
    public Color? Color { get; }
    public IReadOnlyList<IPawn> OccupyingPawns => _occupyingPawns.AsReadOnly();

    public Cell(Position position, CellType type, Color? color)
    {
        Position = position;
        Type = type;
        Color = color;
    }

    internal void AddPawn(IPawn pawn)
    {
        ArgumentNullException.ThrowIfNull(pawn);
        if (!_occupyingPawns.Contains(pawn))
            _occupyingPawns.Add(pawn);
    }

    internal void RemovePawn(IPawn pawn)
    {
        ArgumentNullException.ThrowIfNull(pawn);
        _occupyingPawns.Remove(pawn);
    }
}