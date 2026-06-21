using LudoProjects.Enums;

namespace LudoProjects.Interfaces;

public interface IPlayer
{
    string Name { get; }
    Color Color { get; }
    bool IsFinished { get; set; }
}