using LudoProjects.Controllers;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Tests.TestSupport;

public class GameContext
{
    public GameController Controller { get; }

    public List<IPlayer> Players { get; }

    public Dictionary<IPlayer, List<IPawn>> PlayerPawns { get; }

    public Cell[,] Cells { get; }
    
    // wadah untuk menyimpan semua object yang dibutuhkan oleh unit test.
    public GameContext(
        GameController controller,
        List<IPlayer> players,
        Dictionary<IPlayer, List<IPawn>> playerPawns,
        Cell[,] cells)
    {
        Controller = controller;
        Players = players;
        PlayerPawns = playerPawns;
        Cells = cells;
    }
}