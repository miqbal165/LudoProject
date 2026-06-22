using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Pawn : IPawn
{
    public int Id { get; }
    public Color Color { get; }
    public PawnStatus Status { get; set; }
    public int StepIndex { get; set; }

    public Pawn(int id, Color color)
    {
        if (id < 0 || id > 3)
            throw new ArgumentOutOfRangeException(nameof(id), "Id pion harus 0 sampai 3.");

        Id = id;
        Color = color;
        Status = PawnStatus.InBase;
        StepIndex = -1;
    }

    public override string ToString() => $"{Color.ToString()[0]}{Id + 1}";
}