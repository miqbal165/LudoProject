using LudoProjects.Enums;

namespace LudoProjects.Interfaces;

public interface IPawn
{
    int Id { get; }
    Color Color { get; }
    PawnStatus Status { get; set; }
    int StepIndex { get; set; }
}