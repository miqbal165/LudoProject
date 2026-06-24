using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static void ShowTitle()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("           WELCOME TO LUDO GAME");
        Console.WriteLine("                  CONSOLE");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }
    
    public static void DrawBoard(IBoard board)
    {
        Console.WriteLine("BOARD");
        Console.WriteLine("-----");

        for (var row = 0; row < 15; row++)
        {
            for (var column = 0; column < 15; column++)
            {
                var cell = board.GetCell(new Position(row, column));
                var token = GetCellToken(cell);
                var consoleColor = GetCellConsoleColor(cell);
                
                Console.Write("[");
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = consoleColor;
                Console.Write(token.PadRight(3)[..3]);
                Console.ForegroundColor = originalColor;
                Console.Write("]");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Explanation: R/B/G/Y = pawn color, S = start, * = protected, H = home");
        Console.WriteLine("            R#3/B#3/G#3/Y#3 = BLOCK (3+ pawns of the same color)");
    }

    private static string GetCellToken(ICell cell)
    {
        
        if (cell.OccupyingPawns.Count > 0)
        {
            var groups = cell.OccupyingPawns.GroupBy(pawn => pawn.Color).ToList();
            if (groups.Count == 1)
            {
                var group = groups[0].OrderBy(pawn => pawn.Id).ToList();
                var letter = GetColorLetter(group[0].Color);

                if (group.Count >= 3)
                    return $"{letter}#{group.Count}";
                if (group.Count == 2)
                    return $"{letter}{group[0].Id + 1}{group[1].Id + 1}";

                return $"{letter}{group[0].Id + 1}";
            }

            var mixedLetters = string.Concat(groups.Select(group => GetColorLetter(group.Key)));
            return mixedLetters.Length <= 3 ? mixedLetters : "MIX";
        }
        
        return cell.Type switch
        {
            CellType.Start => $"S{GetColorLetter(cell.Color)}",
            CellType.Protected => " * ",
            CellType.HomeColumn => $"H{GetColorLetter(cell.Color)}",
            CellType.Center => " C ",
            CellType.Base => "<^>",
            _ => "  "
        };
    }

    private static string GetColorLetter(Color? color)
    {
        var colorLetter = color switch
        {
            Color.Red => "R",
            Color.Blue => "B",
            Color.Green => "G",
            Color.Yellow => "Y",
            _ => "-"
        };

        return colorLetter;
    }
    
    private static ConsoleColor GetCellConsoleColor(ICell cell)
    {
        var color = cell.OccupyingPawns.FirstOrDefault()?.Color ?? cell.Color;
        return color switch
        {
            Color.Red => ConsoleColor.Red,
            Color.Blue => ConsoleColor.Blue,
            Color.Green => ConsoleColor.Green,
            Color.Yellow => ConsoleColor.Yellow,
            _ => ConsoleColor.Gray
        };
    }
    
    public static void DrawGameState(GameState state)
    {
        Console.WriteLine();
        Console.WriteLine("GAME STATUS");
        Console.WriteLine("-----------");
        Console.WriteLine($"Turn         : {state.CurrentPlayer.Name} ({GetColorName(state.CurrentPlayer.Color)})");
        Console.WriteLine($"Phase        : {state.Phase}");
        Console.WriteLine($"Dice Value   : {(state.LastDiceValue == 0 ? "-" : state.LastDiceValue)}");
        Console.WriteLine($"Extra Roll   : {(state.ExtraRollPending ? "Yes" : "No")}");
        Console.WriteLine();

        foreach (var player in state.Players)
        {
            var pawnDescriptions = state.PlayerPawns[player]
                .OrderBy(pawn => pawn.Id)
                .Select(DescribePawnStatus);
            Console.WriteLine($"{player.Name,-12} ({GetColorLetter(player.Color)}) : {string.Join(" | ", pawnDescriptions)}");
        }
    }

    private static string DescribePawnStatus(IPawn pawn)
    {
        var status = pawn.Status switch
        {
            PawnStatus.InBase => $"{GetPawnLabel(pawn)}=Base",
            PawnStatus.OnBoard => $"{GetPawnLabel(pawn)}=Track-{pawn.StepIndex}",
            PawnStatus.InHomeColumn => $"{GetPawnLabel(pawn)}=Home-{pawn.StepIndex - 51}",
            PawnStatus.Finished => $"{GetPawnLabel(pawn)}=Finish",
            _ => GetPawnLabel(pawn)
        };
        
        return status;
    }
    
    private static string GetPawnLabel(IPawn pawn) => $"{GetColorLetter(pawn.Color)}{pawn.Id + 1}";
    
    private static void TryClearConsole()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Consoles that don't support Clear can still run the game.
        }
    }
    
}