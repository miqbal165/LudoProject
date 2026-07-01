using LudoProjects.Interfaces;
using LudoProjects.Models;
using Spectre.Console;
using LudoColor = LudoProjects.Enums.Color;
using SpectreColor = Spectre.Console.Color;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static List<IPlayer> CreatePlayers()
    {
        AnsiConsole.Write(
            new Rule("[bold cyan]PENGATURAN PEMAIN[/]")
                .RuleStyle("grey")
                .LeftJustified());

        string selectedPlayerCount = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Berapa jumlah pemain?[/]")
                .PageSize(3)
                .HighlightStyle(
                    new Style(
                        SpectreColor.Black,
                        SpectreColor.Cyan1,
                        Decoration.Bold))
                .AddChoices(
                    "2 pemain",
                    "3 pemain",
                    "4 pemain"));

        int playerCount = int.Parse(selectedPlayerCount[..1]);
        List<IPlayer> players = [];
        List<LudoColor> availableColors =
            Enum.GetValues<LudoColor>().ToList();

        for (int index = 0; index < playerCount; index++)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Rule($"[bold]Pemain {index + 1}[/]")
                    .RuleStyle("grey")
                    .LeftJustified());

            string name = ReadPlayerName(players, index + 1);
            LudoColor selectedColor =
                ReadPlayerColor(availableColors);

            availableColors.Remove(selectedColor);
            players.Add(new Player(name, selectedColor));

            AnsiConsole.MarkupLine(
                $"[green]✓[/] {Markup.Escape(name)} memakai warna " +
                $"{GetColoredText(selectedColor, GetColorName(selectedColor))}.");
        }

        ShowPlayerSummary(players);

        AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold green]Semua pemain siap.[/] Pilih untuk melanjutkan.")
                .HighlightStyle(
                    new Style(
                        SpectreColor.Black,
                        SpectreColor.Green,
                        Decoration.Bold))
                .AddChoices("Mulai permainan"));

        return players;
    }

    private static string ReadPlayerName(
        IEnumerable<IPlayer> players,
        int playerNumber)
    {
        while (true)
        {
            string name = AnsiConsole.Prompt(
                    new TextPrompt<string>(
                            $"Nama [bold cyan]pemain {playerNumber}[/]:")
                        .PromptStyle("cyan"))
                .Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowInputError("Nama tidak boleh kosong.");
                continue;
            }

            bool nameAlreadyUsed = players.Any(player =>
                string.Equals(
                    player.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));

            if (nameAlreadyUsed)
            {
                ShowInputError("Nama tersebut sudah digunakan pemain lain.");
                continue;
            }

            return name;
        }
    }

    private static LudoColor ReadPlayerColor(
        IReadOnlyList<LudoColor> availableColors)
    {
        Dictionary<string, LudoColor> choices =
            availableColors.ToDictionary(
                color => GetColoredText(
                    color,
                    $"● {GetColorName(color)}"),
                color => color);

        string selectedChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Pilih [bold]warna pion[/]:")
                .PageSize(4)
                .MoreChoicesText(
                    "[grey](Gunakan tombol panah untuk melihat pilihan lain)[/]")
                .HighlightStyle(
                    new Style(
                        SpectreColor.Black,
                        SpectreColor.White,
                        Decoration.Bold))
                .AddChoices(choices.Keys));

        return choices[selectedChoice];
    }

    private static void ShowPlayerSummary(
        IEnumerable<IPlayer> players)
    {
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(new Style(SpectreColor.Grey))
            .AddColumn(new TableColumn("[bold]No.[/]").Centered())
            .AddColumn(new TableColumn("[bold]Nama[/]"))
            .AddColumn(new TableColumn("[bold]Warna[/]").Centered());

        int number = 1;

        foreach (IPlayer player in players)
        {
            table.AddRow(
                number.ToString(),
                Markup.Escape(player.Name),
                GetColoredText(
                    player.Color,
                    $"● {GetColorName(player.Color)}"));

            number++;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(table)
                .Header("[bold cyan]Daftar Pemain[/]")
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(SpectreColor.Cyan1))
                .Padding(1, 0, 1, 0));
    }

    private static void ShowInputError(string message)
    {
        AnsiConsole.MarkupLine(
            $"[bold red]✗ {Markup.Escape(message)}[/]");
    }

    private static string GetColorName(LudoColor color)
    {
        return color switch
        {
            LudoColor.Red => "Merah",
            LudoColor.Blue => "Biru",
            LudoColor.Green => "Hijau",
            LudoColor.Yellow => "Kuning",
            _ => color.ToString()
        };
    }
}
