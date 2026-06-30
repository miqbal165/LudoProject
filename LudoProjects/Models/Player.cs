using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Player : IPlayer
{
    public string Name { get; }
    public Color Color { get; }
    public bool IsFinished { get; set; }

    public Player(string name, Color color)
    {
        Name = name.Trim();
        Color = color;
        IsFinished = false;
    }

    public override string ToString() => $"{Name} ({Color})";
}