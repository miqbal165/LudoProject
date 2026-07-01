using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using LudoColor = LudoProjects.Enums.Color;
using SpectreColor = Spectre.Console.Color;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    private const int BoardSize = 15;
    private const int CellWidth = 5;

    private static readonly SpectreColor RedBase = new(255, 37, 54);
    private static readonly SpectreColor BlueBase = new(30, 144, 255);
    private static readonly SpectreColor GreenBase = new(28, 200, 18);
    private static readonly SpectreColor YellowBase = new(255, 181, 17);
    private static readonly SpectreColor TrackBackground = new(247, 247, 247);
    private static readonly SpectreColor TrackForeground = new(45, 45, 45);
    private static readonly SpectreColor SlotBackground = new(255, 255, 255);
    private static readonly SpectreColor ProtectedBackground = new(250, 250, 250);
    private static readonly SpectreColor WhiteCellFrame = new(236, 120, 170); 
    private static readonly SpectreColor CenterFinish = new(110, 52, 170);

    public static void ShowTitle()
    {
        Panel title = new Panel(
                Align.Center(
                    new Markup(
                        "[bold cyan]L U D O[/]\n" +
                        "[grey]Spectre.Console Edition[/]")))
            .Border(BoxBorder.Double)
            .BorderStyle(new Style(SpectreColor.Cyan1))
            .Padding(2, 0, 2, 0);

        AnsiConsole.Write(title);
        AnsiConsole.WriteLine();
    }

    public static void DrawBoard(GameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        HashSet<Position> baseSlots = Enum
            .GetValues<LudoColor>()
            .SelectMany(controller.GetBasePositions)
            .ToHashSet();

        Table board = new Table()
            .Border(TableBorder.None)
            .HideHeaders();

        for (int column = 0; column < BoardSize; column++)
        {
            board.AddColumn(
                new TableColumn(string.Empty)
                    .Width(CellWidth + 2)
                    .NoWrap()
                    .Centered()
                    .PadLeft(0)
                    .PadRight(0));
        }

        for (int row = 0; row < BoardSize; row++)
        {
            IRenderable[] renderedCells =
                new IRenderable[BoardSize];

            for (int column = 0; column < BoardSize; column++)
            {
                Position position = new(row, column);
                ICell cell = controller.GetCell(position);

                renderedCells[column] = CreateBoardCell(
                    cell,
                    position,
                    baseSlots.Contains(position));
            }

            board.AddRow(renderedCells);
        }

        Panel boardPanel = new Panel(
                Align.Center(board))
            .Header("[bold white] PAPAN LUDO [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(SpectreColor.Cyan1))
            .Padding(1, 1, 1, 1);

        AnsiConsole.Write(boardPanel);
        DrawBoardLegend();
    }

    public static void DrawGameState(GameState state)
    {
        string currentPlayerName = Markup.Escape(state.CurrentPlayer.Name);

        string currentPlayer = GetColoredText(
                state.CurrentPlayer.Color,
                $"● {currentPlayerName}");

        string dice = state.LastDiceValue == 0
            ? "[grey]-[/]"
            : $"[bold yellow]{GetDiceFace(state.LastDiceValue)}  " +
              $"{state.LastDiceValue}[/]";

        string extraRoll = state.ExtraRollPending
            ? "[bold green]Ya[/]"
            : "[grey]Tidak[/]";

        Table statusTable = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(SpectreColor.Grey))
            .AddColumn(new TableColumn("[bold]Giliran[/]"))
            .AddColumn(new TableColumn("[bold]Fase[/]").Centered())
            .AddColumn(new TableColumn("[bold]Dadu[/]").Centered())
            .AddColumn(new TableColumn("[bold]Roll Lagi[/]").Centered())
            .AddRow(currentPlayer, GetPhaseMarkup(state.Phase), dice, extraRoll);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(statusTable)
                .Header("[bold yellow] STATUS [/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(
                    new Style(SpectreColor.Yellow))
                .Padding(1, 0, 1, 0));

        Table playerTable = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(SpectreColor.Grey))
            .AddColumn(new TableColumn("[bold]Pemain[/]"))
            .AddColumn(new TableColumn("[bold]1[/]").Centered())
            .AddColumn(new TableColumn("[bold]2[/]").Centered())
            .AddColumn(new TableColumn("[bold]3[/]").Centered())
            .AddColumn(new TableColumn("[bold]4[/]").Centered());

        foreach (IPlayer player in state.Players)
        {
            List<string> pawnDescriptions = state.PlayerPawns[player]
                    .OrderBy(pawn => pawn.Id)
                    .Select(DescribePawnStatus)
                    .ToList();

            while (pawnDescriptions.Count < 4)
            {
                pawnDescriptions.Add("[grey]-[/]");
            }

            string activeMarker = ReferenceEquals(
                    player,
                    state.CurrentPlayer)
                    ? "▶ "
                    : "  ";

            playerTable.AddRow(
                activeMarker +
                GetColoredText(
                    player.Color,
                    Markup.Escape(player.Name)),
                pawnDescriptions[0],
                pawnDescriptions[1],
                pawnDescriptions[2],
                pawnDescriptions[3]);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(playerTable)
                .Header("[bold green] POSISI PION [/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(
                    new Style(SpectreColor.Green))
                .Padding(1, 0, 1, 0));
    }


    private static IRenderable CreateBoardCell(
        ICell cell,
        Position position,
        bool isBaseSlot)
    {
        bool useWhiteBorder = cell.Type == CellType.Normal ||
                              cell.Type == CellType.Protected ||
                              (cell.Type == CellType.Base && isBaseSlot);

        if (cell.OccupyingPawns.Count > 0)
        {
            return CreateOccupiedCell(
                cell,
                isBaseSlot,
                useWhiteBorder);
        }

        return cell.Type switch
        {
            CellType.Base when isBaseSlot => CreateBorderedWhiteCell("○",GetPlayerColor(cell.Color)),
            CellType.Base => CreateUnborderedColoredCell(string.Empty,GetPlayerColor(cell.Color)),
            CellType.HomeColumn => CreateUnborderedColoredCell(string.Empty,GetPlayerColor(cell.Color)),
            CellType.Start => CreateUnborderedColoredCell("▶", GetPlayerColor(cell.Color)),
            CellType.Protected => CreateBorderedWhiteCell("★", TrackForeground),
            CellType.Center => CreateUnborderedColoredCell(
                    position.Row == 7 &&
                    position.Column == 7
                        ? "FIN"
                        : string.Empty,
                    GetCenterColor(position)),

            _ => CreateBorderedWhiteCell(string.Empty, TrackForeground)
        };
    }

    private static IRenderable CreateOccupiedCell(
        ICell cell,
        bool isBaseSlot,
        bool useWhiteBorder)
    {
        List<IPawn> pawns = cell.OccupyingPawns
            .OrderBy(pawn => pawn.Color)
            .ThenBy(pawn => pawn.Id)
            .ToList();

        string label = CreatePawnCellLabel(pawns);

        if (useWhiteBorder || isBaseSlot)
        {
            SpectreColor foreground = pawns.Select(pawn => pawn.Color)
                    .Distinct()
                    .Count() == 1
                    ? GetPlayerColor(pawns[0].Color)
                    : TrackForeground;

            return CreateBorderedWhiteCell(
                label,
                foreground);
        }

        SpectreColor background = pawns.Select(pawn => pawn.Color)
                .Distinct()
                .Count() == 1
                ? GetPlayerColor(pawns[0].Color)
                : ProtectedBackground;

        return CreateUnborderedColoredCell(
            label,
            background);
    }

    private static string CreatePawnCellLabel(
        IReadOnlyList<IPawn> pawns)
    {
        if (pawns.Count == 0)
        {
            return string.Empty;
        }

        if (pawns.Count == 1)
        {
            return GetPawnLabel(pawns[0]);
        }

        string firstTwoLabels = string.Concat(
            pawns
                .Take(2)
                .Select(GetPawnLabel));

        if (pawns.Count == 2 &&
            firstTwoLabels.Length <= CellWidth)
        {
            return firstTwoLabels;
        }

        bool sameColor = pawns
            .Select(pawn => pawn.Color)
            .Distinct()
            .Count() == 1;

        if (sameColor)
        {
            return
                $"{GetColorLetter(pawns[0].Color)}x{pawns.Count}";
        }

        return pawns.Count <= 2
            ? firstTwoLabels
            : $"Mix{pawns.Count}";
    }

    private static IRenderable CreateBorderedWhiteCell(
        string content,
        SpectreColor foreground)
    {
        Table cell = new Table()
            .Border(TableBorder.Square)
            .BorderStyle(
                new Style(
                    WhiteCellFrame,
                    WhiteCellFrame))
            .HideHeaders();

        cell.AddColumn(
            new TableColumn(string.Empty)
                .Width(CellWidth)
                .NoWrap()
                .Centered()
                .PadLeft(0)
                .PadRight(0));

        cell.AddRow(
            new Text(
                CenterCellText(content),
                new Style(
                    foreground,
                    TrackBackground,
                    Decoration.Bold)));

        return cell;
    }

    private static IRenderable CreateUnborderedColoredCell(string content, SpectreColor background)
    {
        SpectreColor foreground = IsLightBackground(background)
                ? SpectreColor.Black
                : SpectreColor.White;

        int fullWidth = CellWidth + 2;

        string topAndBottom = new string(' ', fullWidth);

        string middle = CenterText(content, fullWidth);

        return new Text($"{topAndBottom}\n{middle}\n{topAndBottom}",
            new Style(
                foreground,
                background,
                Decoration.Bold));
    }

    private static string CenterText(string content, int width)
    {
        content ??= string.Empty;

        if (content.Length > width)
        {
            content = content[..width];
        }

        int totalPadding =
            width - content.Length;

        int leftPadding =
            totalPadding / 2;

        int rightPadding =
            totalPadding - leftPadding;

        return
            new string(' ', leftPadding) +
            content +
            new string(' ', rightPadding);
    }

    private static SpectreColor GetCenterColor(
        Position position)
    {
        if (position.Row == 7 &&
            position.Column == 7)
        {
            return CenterFinish;
        }

        if (position.Row == 6)
        {
            return BlueBase;
        }

        if (position.Row == 8)
        {
            return YellowBase;
        }

        if (position.Column == 6)
        {
            return RedBase;
        }

        if (position.Column == 8)
        {
            return GreenBase;
        }

        return CenterFinish;
    }

    private static SpectreColor GetPlayerColor(
        LudoColor? color)
    {
        return color switch
        {
            LudoColor.Red => RedBase,
            LudoColor.Blue => BlueBase,
            LudoColor.Green => GreenBase,
            LudoColor.Yellow => YellowBase,
            _ => TrackBackground
        };
    }

    private static string CenterCellText(
        string content)
    {
        content ??= string.Empty;

        if (content.Length > CellWidth)
        {
            content = content[..CellWidth];
        }

        int totalPadding =
            CellWidth - content.Length;

        int leftPadding =
            totalPadding / 2;

        int rightPadding =
            totalPadding - leftPadding;

        return
            new string(' ', leftPadding) +
            content +
            new string(' ', rightPadding);
    }

    private static bool IsLightBackground(
        SpectreColor color)
    {
        double brightness =
            (color.R * 299 +
             color.G * 587 +
             color.B * 114) / 1000.0;

        return brightness >= 150;
    }

    private static void DrawBoardLegend()
    {
        Grid legend = new Grid();

        for (int index = 0; index < 5; index++)
        {
            legend.AddColumn();
        }

        legend.AddRow(
            CreateLegendItem(RedBase, "Base / Home"),
            CreateLegendItem(TrackBackground, "Jalur"),
            CreateLegendItem(ProtectedBackground, "★ Aman"),
            CreateLegendItem(CenterFinish, "Finish"),
            new Markup("[bold]R1[/] = pion merah nomor 1"));

        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            new Panel(legend)
                .Header("[grey] KETERANGAN [/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(SpectreColor.Grey))
                .Padding(1, 0, 1, 0));
    }

    private static IRenderable CreateLegendItem(
        SpectreColor background,
        string label)
    {
        return new Text(
            $"   {label}",
            new Style(
                SpectreColor.Black,
                background));
    }

    private static string GetColoredText(
        LudoColor color,
        string text)
    {
        string colorTag = color switch
        {
            LudoColor.Red => "red",
            LudoColor.Blue => "blue",
            LudoColor.Green => "green",
            LudoColor.Yellow => "yellow",
            _ => "white"
        };

        return $"[bold {colorTag}]{text}[/]";
    }

    private static string DescribePawnStatus(
        IPawn pawn)
    {
        string content = pawn.Status switch
        {
            PawnStatus.InBase =>
                "Base",

            PawnStatus.OnBoard =>
                $"Jalur {pawn.StepIndex + 1}",

            PawnStatus.InHomeColumn =>
                $"Home {Math.Max(1, pawn.StepIndex - 50)}",

            PawnStatus.Finished =>
                "Selesai",

            _ =>
                "-"
        };

        string style = pawn.Status switch
        {
            PawnStatus.InBase =>
                "grey",

            PawnStatus.OnBoard =>
                "white",

            PawnStatus.InHomeColumn =>
                "yellow",

            PawnStatus.Finished =>
                "bold green",

            _ =>
                "white"
        };

        return
            $"[{style}]{Markup.Escape(content)}[/]";
    }

    private static string GetPawnLabel(
        IPawn pawn)
    {
        return
            $"{GetColorLetter(pawn.Color)}" +
            $"{pawn.Id + 1}";
    }

    private static string GetColorLetter(
        LudoColor? color)
    {
        return color switch
        {
            LudoColor.Red => "R",
            LudoColor.Blue => "B",
            LudoColor.Green => "G",
            LudoColor.Yellow => "Y",
            _ => "-"
        };
    }

    private static string GetPhaseMarkup(
        TurnPhase phase)
    {
        return phase switch
        {
            TurnPhase.WaitingToStart =>
                "[grey]Menunggu[/]",

            TurnPhase.Rolling =>
                "[cyan]Kocok dadu[/]",

            TurnPhase.SelectingPawn =>
                "[yellow]Pilih pion[/]",

            TurnPhase.GameOver =>
                "[bold green]Selesai[/]",

            _ =>
                Markup.Escape(
                    phase.ToString())
        };
    }

    private static string GetDiceFace(int value)
    {
        return value switch
        {
            1 => "⚀",
            2 => "⚁",
            3 => "⚂",
            4 => "⚃",
            5 => "⚄",
            6 => "⚅",
            _ => "-"
        };
    }

    private static void ShowMessage(
        string message,
        string color = "cyan")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(
                    new Markup(
                        Markup.Escape(message)))
                .Header(
                    $"[bold {color}] INFORMASI [/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(
                    Style.Parse(color))
                .Padding(1, 0, 1, 0));
    }

    private static void TryClearConsole()
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch (IOException)
        {
            // Terminal tanpa dukungan clear tetap bisa digunakan.
        }
    }
}
