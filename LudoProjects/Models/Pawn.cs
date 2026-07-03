using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Pawn(int id, Color color) : IPawn
{
    public int Id { get; } = id;
    public Color Color { get; } = color;
    public PawnStatus Status { get; set; } = PawnStatus.InBase;
    public int StepIndex { get; set; } = -1;
}