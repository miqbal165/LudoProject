using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Exceptions;
using LudoProjects.Helper;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Views;

namespace LudoProjects;

internal static class Program
{
    public static void Main()
    {
        GlobalExceptionHandler.Run(RunApplication);
    }

    private static void RunApplication()
    {
        LudoUi.ShowTitle();

        List<IPlayer> players = LudoUi.CreatePlayers();

        Dictionary<IPlayer, List<IPawn>> playerPawns = CreatePlayerPawns(players);

        Cell[,] cells = new Cell[15, 15];
        IBoard board = new Board(cells);

        Dictionary<Color, IReadOnlyList<Position>> pathCache = new();

        IDice dice = new Dice();
        Random randomDiceNumberGenerator = new();

        GameController controller = new(
            players,
            playerPawns,
            pathCache,
            board,
            dice,
            randomDiceNumberGenerator);

        BoardFactory.InitializeCells(
            controller,
            cells,
            playerPawns);

        LudoUi.RunGame(controller);
    }

    private static Dictionary<IPlayer, List<IPawn>> CreatePlayerPawns(IEnumerable<IPlayer> players)
    {
        Dictionary<IPlayer, List<IPawn>> playerPawns = new();

        foreach (IPlayer player in players)
        {
            List<IPawn> pawns = [];

            for (int pawnId = 0; pawnId < 4; pawnId++)
            {
                pawns.Add(
                    new Pawn(
                        pawnId,
                        player.Color));
            }

            playerPawns[player] = pawns;
        }

        return playerPawns;
    }
}