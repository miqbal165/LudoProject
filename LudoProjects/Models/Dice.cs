using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Dice : IDice
{
    public int CurrentValue { get; set; }

    public Dice()
    {
        CurrentValue = 0;
    }
}
