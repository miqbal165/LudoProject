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
        List<IPawn> movablePawns = new();
        IDice dice = new Dice();

        IPawn firstPlayerPawnRed = new Pawn(0, Color.Red);
        IPawn secondPlayerPawnRed = new Pawn(1, Color.Red);
        IPawn thirdPlayerPawnRed = new Pawn(2, Color.Red);
        IPawn fourthPlayerPawnRed = new Pawn(3, Color.Red);
        
        
        movablePawns.Add(firstPlayerPawnRed);
        movablePawns.Add(secondPlayerPawnRed);
        movablePawns.Add(thirdPlayerPawnRed);
        movablePawns.Add(fourthPlayerPawnRed);
        

        var listPlayers = LudoUi.CreatePlayers();
        GameController controller = new GameController(listPlayers, board, dice, new Random());
        
        LudoUi.ShowTitle();
        LudoUi.DrawBoard(board);
        LudoUi.DrawGameState(controller.GetGameState());
        
    }
}