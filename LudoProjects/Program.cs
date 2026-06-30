using LudoProjects.Controllers;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Views;

namespace LudoProjects;

internal abstract class Program
{
    public static void Main()
    {
        IBoard board = new Board();
        IDice dice = new Dice();
        Random randomDiceNumberGenerator = new Random();

        List<IPlayer> players = LudoUi.CreatePlayers();
        Dictionary<IPlayer, List<IPawn>> playerPawns = LudoUi.CreatePlayerPawns(players, board);
        
        GameController controller = new (players, playerPawns, 
            board, dice, randomDiceNumberGenerator);

        LudoUi.ShowTitle();
        LudoUi.RunGame(controller, board);
    }
}