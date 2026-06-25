using System.Collections.ObjectModel;
using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Views;

namespace LudoProjects;

internal abstract class Program
{
    public static void Main()
    {
        // Testing UI
        
        IBoard board = new Board();
        IDice dice = new Dice();
        
        var listPlayers = LudoUi.CreatePlayers();
        GameController controller = new (listPlayers, board, dice, new Random());
        LudoUi.RunGame(controller, board);
    }
}