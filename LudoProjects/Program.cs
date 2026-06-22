using System.Collections.ObjectModel;
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
        List<IPlayer> listPlayers = new();
        List<IPawn> movablePawns = new();

        IPlayer firstPlayer = new Player("Budi", Color.Red);
        IPlayer secondPlayer = new Player("Iqbal", Color.Green);

        IPawn firstPlayerPawnRed = new Pawn(0, Color.Red);
        IPawn secondPlayerPawnRed = new Pawn(1, Color.Red);
        IPawn thirdPlayerPawnRed = new Pawn(2, Color.Red);
        IPawn fourthPlayerPawnRed = new Pawn(3, Color.Red);
        
        IPawn firstPlayerPawnGreen = new Pawn(0, Color.Green);
        IPawn secondPlayerPawnGreen = new Pawn(1, Color.Green);
        IPawn thirdPlayerPawnGreen = new Pawn(2, Color.Green);
        IPawn fourthPlayerPawnGreen = new Pawn(3, Color.Green);
        
        movablePawns.Add(firstPlayerPawnRed);
        movablePawns.Add(secondPlayerPawnRed);
        movablePawns.Add(thirdPlayerPawnRed);
        movablePawns.Add(fourthPlayerPawnRed);
        
        IReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>> playersPawns =
            new ReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>>(
                new Dictionary<IPlayer, IReadOnlyList<IPawn>>
                {
                    [firstPlayer] =
                    [
                        firstPlayerPawnRed,
                        secondPlayerPawnRed,
                        thirdPlayerPawnRed,
                        fourthPlayerPawnRed
                    ],
                    
                    [secondPlayer] =
                    [
                        firstPlayerPawnGreen,
                        secondPlayerPawnGreen,
                        thirdPlayerPawnGreen,
                        fourthPlayerPawnGreen
                    ]
                }
            );
        
        listPlayers.Add(firstPlayer);
        listPlayers.Add(secondPlayer);
        
        GameState state = new GameState(
            TurnPhase.Rolling,
            listPlayers[0],
            listPlayers,
            playersPawns,
            3,
            true,
            movablePawns,
            null
        );
        
        LudoUi.ShowTitle();
        LudoUi.DrawBoard(board);
        LudoUi.DrawGameState(state);
    }
}