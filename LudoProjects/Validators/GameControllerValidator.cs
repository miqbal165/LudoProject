using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Validators;

public static class GameControllerValidator
{
    public static void ValidateConstructorArguments(
        List<IPlayer> players,
        Dictionary<IPlayer, List<IPawn>> playerPawns,
        Dictionary<Color, IReadOnlyList<Position>> pathCache,
        IBoard board,
        IDice dice,
        Random randomDiceNumberGenerator)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(playerPawns);
        ArgumentNullException.ThrowIfNull(pathCache);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(
            randomDiceNumberGenerator);

        if (players.Count is < 2 or > 4)
        {
            throw new ArgumentException(
                "Minimum players must be 2 and maximum is 4.",
                nameof(players));
        }

        int distinctColorCount =
            players
                .Select(player => player.Color)
                .Distinct()
                .Count();

        if (distinctColorCount != players.Count)
        {
            throw new ArgumentException(
                "Each player must choose a different color.",
                nameof(players));
        }

        foreach (IPlayer player in players)
        {
            if (!playerPawns.TryGetValue(
                    player,
                    out List<IPawn>? pawns))
            {
                throw new ArgumentException(
                    $"Pawn collection for player " +
                    $"{player.Name} was not found.",
                    nameof(playerPawns));
            }

            if (pawns.Count != 4)
            {
                throw new ArgumentException(
                    $"Player {player.Name} must have " +
                    $"exactly four pawns.",
                    nameof(playerPawns));
            }

            if (pawns.Any(
                    pawn => pawn.Color != player.Color))
            {
                throw new ArgumentException(
                    $"All pawns belonging to " +
                    $"{player.Name} must have color " +
                    $"{player.Color}.",
                    nameof(playerPawns));
            }

            int uniquePawnIdCount =
                pawns
                    .Select(pawn => pawn.Id)
                    .Distinct()
                    .Count();

            if (uniquePawnIdCount != pawns.Count)
            {
                throw new ArgumentException(
                    $"Pawn IDs belonging to " +
                    $"{player.Name} must be unique.",
                    nameof(playerPawns));
            }
        }

        if (board.Cells.GetLength(0) != 15 ||
            board.Cells.GetLength(1) != 15)
        {
            throw new ArgumentException(
                "The Ludo board must have a size of 15 x 15.",
                nameof(board));
        }
    }
}
