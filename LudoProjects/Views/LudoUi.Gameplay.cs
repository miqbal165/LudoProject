using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using Spectre.Console;
using SpectreColor = Spectre.Console.Color;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static void RunGame(GameController controller)
    {
        string message = "Permainan dimulai. Semoga beruntung!";
        GameState? currentState = null;
        IPlayer? winnerFromEvent = null;
        bool winnerDisplayed = false;

        void DisplayWinnerIfReady(GameState state)
        {
            IPlayer? winner =
                state.Winner ?? winnerFromEvent;

            if (winnerDisplayed ||
                state.Phase != TurnPhase.GameOver ||
                winner is null)
            {
                return;
            }

            ShowWinner(winner);
            winnerDisplayed = true;
        }

        void HandleStateChange(GameState newState)
        {
            currentState = newState;

            TryClearConsole();
            ShowTitle();
            DrawBoard(controller);
            DrawGameState(newState);

            DisplayWinnerIfReady(newState);
        }

        void HandlePlayerWon(IPlayer winningPlayer)
        {
            winnerFromEvent = winningPlayer;

            if (currentState is not null)
            {
                DisplayWinnerIfReady(currentState);
            }
        }

        controller.OnStateChanged += HandleStateChange;
        controller.OnPlayerWon += HandlePlayerWon;

        try
        {
            controller.StartGame();

            if (currentState is null)
            {
                ShowMessage(
                    "State permainan belum tersedia. Pastikan StartGame() " +
                    "memanggil BroadcastState().",
                    "red");

                return;
            }

            while (currentState.Phase != TurnPhase.GameOver)
            {
                GameState state = currentState;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    ShowMessage(message);
                    message = string.Empty;
                }

                if (state.Phase == TurnPhase.Rolling)
                {
                    IPlayer playerBeforeRoll = state.CurrentPlayer;

                    Dictionary<IPawn, (PawnStatus Status, int StepIndex)>
                        pawnStatesBeforeRoll = state.PlayerPawns
                            .SelectMany(pair => pair.Value)
                            .ToDictionary(
                                pawn => pawn,
                                pawn => (pawn.Status, pawn.StepIndex));

                    string playerName =
                        Markup.Escape(playerBeforeRoll.Name);

                    AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(
                                $"Giliran {GetColoredText(playerBeforeRoll.Color, playerName)}. " +
                                "Pilih aksi:")
                            .HighlightStyle(
                                new Style(
                                    SpectreColor.Black,
                                    SpectreColor.Cyan1,
                                    Decoration.Bold))
                            .AddChoices("🎲 Kocok dadu"));

                    controller.RollDice();

                    if (currentState is null)
                    {
                        ShowMessage(
                            "State tidak tersedia setelah dadu dikocok.",
                            "red");

                        return;
                    }

                    GameState afterRoll = currentState;

                    IPawn? automaticallyMovedPawn =
                        afterRoll.PlayerPawns[playerBeforeRoll]
                            .FirstOrDefault(pawn =>
                                pawnStatesBeforeRoll.TryGetValue(
                                    pawn,
                                    out (PawnStatus Status, int StepIndex) oldState)
                                &&
                                (oldState.Status != pawn.Status ||
                                 oldState.StepIndex != pawn.StepIndex));

                    List<string> automaticallyCaptured = afterRoll.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .Where(pawn =>
                            pawnStatesBeforeRoll.TryGetValue(
                                pawn,
                                out (PawnStatus Status, int StepIndex) oldState)
                            && oldState.Status != PawnStatus.InBase
                            && pawn.Status == PawnStatus.InBase)
                        .Select(GetPawnLabel)
                        .ToList();

                    if (automaticallyMovedPawn is not null)
                    {
                        string captureText = automaticallyCaptured.Count > 0
                            ? " dan menendang " +
                              $"{string.Join(", ", automaticallyCaptured)} ke base"
                            : string.Empty;

                        string extraText =
                            ReferenceEquals(
                                playerBeforeRoll,
                                afterRoll.CurrentPlayer)
                            && afterRoll.Phase == TurnPhase.Rolling
                                ? " Pemain mendapat kesempatan mengocok lagi."
                                : string.Empty;

                        message =
                            $"Hasil dadu {afterRoll.LastDiceValue}. " +
                            $"{GetPawnLabel(automaticallyMovedPawn)} " +
                            $"otomatis dimainkan{captureText}.{extraText}";
                    }
                    else if (!ReferenceEquals(
                                 playerBeforeRoll,
                                 afterRoll.CurrentPlayer)
                             && afterRoll.LastDiceValue == 6)
                    {
                        message =
                            $"{playerBeforeRoll.Name} mendapatkan angka 6 " +
                            "tiga kali berturut-turut. Giliran dibatalkan.";
                    }
                    else if (afterRoll.Phase == TurnPhase.SelectingPawn)
                    {
                        message =
                            $"Hasil dadu {playerBeforeRoll.Name}: " +
                            $"{afterRoll.LastDiceValue}. Pilih pion yang dimainkan.";
                    }
                    else if (ReferenceEquals(
                                 playerBeforeRoll,
                                 afterRoll.CurrentPlayer)
                             && afterRoll.LastDiceValue == 6)
                    {
                        message =
                            "Hasil dadu 6, tetapi tidak ada langkah valid. " +
                            "Pemain memperoleh roll tambahan.";
                    }
                    else
                    {
                        message =
                            $"Hasil dadu {afterRoll.LastDiceValue}. " +
                            "Tidak ada pion yang dapat digerakkan; giliran dilewati.";
                    }

                    continue;
                }

                if (state.Phase == TurnPhase.SelectingPawn)
                {
                    Dictionary<string, int> pawnChoices =
                        state.MovablePawns
                            .OrderBy(pawn => pawn.Id)
                            .ToDictionary(
                                pawn =>
                                    GetColoredText(
                                        pawn.Color,
                                        GetPawnLabel(pawn)) +
                                    " — " +
                                    Markup.Escape(
                                        DescribeMove(
                                            controller,
                                            pawn,
                                            state.LastDiceValue)),
                                pawn => pawn.Id);

                    string selectedChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(
                                $"Dadu menunjukkan [bold yellow]" +
                                $"{GetDiceFace(state.LastDiceValue)} " +
                                $"{state.LastDiceValue}[/]. " +
                                "Pilih pion:")
                            .PageSize(6)
                            .MoreChoicesText(
                                "[grey](Gunakan tombol panah untuk pilihan lain)[/]")
                            .HighlightStyle(
                                new Style(
                                    SpectreColor.Black,
                                    SpectreColor.Yellow,
                                    Decoration.Bold))
                            .AddChoices(pawnChoices.Keys));

                    int selectedPawnId = pawnChoices[selectedChoice];

                    Dictionary<IPawn, (PawnStatus Status, int StepIndex)> beforeMove =
                        state.PlayerPawns
                            .SelectMany(pair => pair.Value)
                            .ToDictionary(
                                pawn => pawn,
                                pawn => (pawn.Status, pawn.StepIndex));

                    IPawn selectedPawn = state.MovablePawns.Single(
                        pawn => pawn.Id == selectedPawnId);

                    string selectedLabel =
                        GetPawnLabel(selectedPawn);

                    string moveDescription = DescribeMove(
                        controller,
                        selectedPawn,
                        state.LastDiceValue);

                    controller.SelectPawn(selectedPawnId);

                    if (currentState is null)
                    {
                        ShowMessage(
                            "State tidak tersedia setelah pion bergerak.",
                            "red");

                        return;
                    }

                    GameState afterMove = currentState;

                    List<string> captured = afterMove.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .Where(pawn =>
                            beforeMove.TryGetValue(
                                pawn,
                                out (PawnStatus Status, int StepIndex) oldState)
                            && oldState.Status != PawnStatus.InBase
                            && pawn.Status == PawnStatus.InBase)
                        .Select(GetPawnLabel)
                        .ToList();

                    message = captured.Count > 0
                        ? $"{moveDescription}; {selectedLabel} menendang " +
                          $"{string.Join(", ", captured)} kembali ke base."
                        : $"{moveDescription}.";
                }
            }

            if (currentState is not null)
            {
                DisplayWinnerIfReady(currentState);
            }
        }
        finally
        {
            controller.OnStateChanged -= HandleStateChange;
            controller.OnPlayerWon -= HandlePlayerWon;
        }
    }

    private static string DescribeMove(
        GameController controller,
        IPawn pawn,
        int diceValue)
    {
        if (pawn.Status == PawnStatus.InBase)
        {
            return $"{GetPawnLabel(pawn)} keluar dari Base menuju Start";
        }

        IReadOnlyList<Position> path =
            controller.GetFullPath(pawn.Color);

        int targetIndex = pawn.StepIndex + diceValue;

        if (targetIndex < 0 || targetIndex >= path.Count)
        {
            return $"{GetPawnLabel(pawn)} tidak memiliki langkah valid";
        }

        Position target = path[targetIndex];
        int finishIndex = path.Count - 1;
        int homeColumnStartIndex =
            finishIndex -
            controller.GetHomeColumnPositions(pawn.Color).Count;

        if (targetIndex == finishIndex)
        {
            return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke Finish";
        }

        if (targetIndex >= homeColumnStartIndex)
        {
            return
                $"{GetPawnLabel(pawn)} maju {diceValue} langkah " +
                $"ke Home Column ({target.Row},{target.Column})";
        }

        return
            $"{GetPawnLabel(pawn)} maju {diceValue} langkah " +
            $"ke cell ({target.Row},{target.Column})";
    }

    private static void ShowWinner(IPlayer player)
    {
        TryClearConsole();
        ShowTitle();

        string playerName =
            Markup.Escape(player.Name.ToUpperInvariant());

        string winnerText =
            $"[bold green]🏆 PEMENANG 🏆[/]\n\n" +
            $"{GetColoredText(player.Color, playerName)}\n" +
            $"[grey]Semua pion telah mencapai Finish.[/]";

        AnsiConsole.Write(
            new Panel(new Markup(winnerText))
                .Header("[bold yellow]GAME OVER[/]")
                .Border(BoxBorder.Double)
                .BorderStyle(new Style(SpectreColor.Yellow))
                .Padding(4, 2, 4, 2));

        AnsiConsole.WriteLine();

        AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Permainan selesai.[/]")
                .HighlightStyle(
                    new Style(
                        SpectreColor.Black,
                        SpectreColor.Green,
                        Decoration.Bold))
                .AddChoices("Keluar"));
    }
}
