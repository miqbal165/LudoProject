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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama pemain tidak boleh kosong.", nameof(name));

        Name = name.Trim();
        Color = color;
        IsFinished = false;
    }

    public override string ToString() => $"{Name} ({Color})";
}