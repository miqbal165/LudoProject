using LudoProjects.Controllers;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Views;

namespace LudoProjects;

internal abstract class Program
{
    public static void Main()
    {
        LudoUi.ShowTitle();
        List<IPlayer> players = LudoUi.CreatePlayers();
        IBoard board = new Board();
        IDice dice = new Dice();
        var controller = new GameController(players, board, dice, new Random());

        LudoUi.RunGame(controller, board);
    }
}