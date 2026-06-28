using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static void RunGame(GameController controller, IBoard board)
    {
        string message = "Permainan dimulai.";

        GameState? currentState = null;
        IPlayer? winner = null;
        bool winnerDisplayed = false;

        void DisplayWinnerIfReady()
        {
            if (winnerDisplayed ||
                winner is null ||
                currentState?.Phase != TurnPhase.GameOver)
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
            DrawBoard(board);
            DrawGameState(newState);

            DisplayWinnerIfReady();
        }

        void HandlePlayerWon(IPlayer winningPlayer)
        {
            winner = winningPlayer;
            DisplayWinnerIfReady();
        }

        controller.OnStateChange += HandleStateChange;
        controller.OnPlayerWon += HandlePlayerWon;

        try
        {
            controller.StartGame();

            if (currentState is null)
            {
                throw new InvalidOperationException(
                    "StartGame() tidak mengirim state. " +
                    "Pastikan StartGame() memanggil BroadcastState()."
                );
            }

            while (currentState.Phase != TurnPhase.GameOver)
            {
                GameState state = currentState;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine();
                    Console.WriteLine($"> {message}");
                    message = string.Empty;
                }

                if (state.Phase == TurnPhase.Rolling)
                {
                    IPlayer playerBeforeRoll = state.CurrentPlayer;

                    Dictionary<IPawn,(PawnStatus Status, int StepIndex)> pawnStatesBeforeRoll = state.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .ToDictionary(
                            pawn => pawn,
                            pawn => (pawn.Status, pawn.StepIndex)
                        );

                    Console.WriteLine();
                    Console.Write(
                        $"{playerBeforeRoll.Name.ToUpper()} " +
                        "Tekan ENTER untuk mengocok dadu..."
                    );

                    Console.ReadLine();

                    controller.RollDice();

                    GameState afterRoll = currentState
                                    ?? throw new InvalidOperationException(
                                        "State tidak tersedia setelah dadu dikocok."
                                    );

                    IPawn? automaticallyMovedPawn =
                        afterRoll.PlayerPawns[playerBeforeRoll]
                            .FirstOrDefault(pawn =>
                                pawnStatesBeforeRoll.TryGetValue(
                                    pawn,
                                    out (PawnStatus Status, int StepIndex) oldState
                                )
                                &&
                                (
                                    oldState.Status != pawn.Status ||
                                    oldState.StepIndex != pawn.StepIndex
                                )
                            );

                    List<string> automaticallyCaptured = afterRoll.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .Where(pawn =>
                            pawnStatesBeforeRoll.TryGetValue(
                                pawn,
                                out (PawnStatus Status, int StepIndex) oldState
                            )
                            &&
                            oldState.Status != PawnStatus.InBase
                            &&
                            pawn.Status == PawnStatus.InBase
                        )
                        .Select(GetPawnLabel)
                        .ToList();

                    if (automaticallyMovedPawn is not null)
                    {
                        string captureText =
                            automaticallyCaptured.Count > 0
                                ? $" dan menendang " +
                                  $"{string.Join(", ", automaticallyCaptured)} " +
                                  "ke base"
                                : string.Empty;

                        string extraText =
                            ReferenceEquals(
                                playerBeforeRoll,
                                afterRoll.CurrentPlayer
                            )
                            &&
                            afterRoll.Phase == TurnPhase.Rolling
                                ? "Pemain mendapatkan kesempatan untuk mengocok dadu lagi."
                                : string.Empty;

                        message =
                            $"Hasil dadu {afterRoll.LastDiceValue}. " +
                            $"{GetPawnLabel(automaticallyMovedPawn)} " +
                            $"otomatis pawn yang dimainkan {captureText}.{extraText}";
                    }
                    else if (
                        !ReferenceEquals(
                            playerBeforeRoll,
                            afterRoll.CurrentPlayer
                        )
                        &&
                        afterRoll.LastDiceValue == 6
                    )
                    {
                        message =
                            $"{playerBeforeRoll.Name} mendapatkan angka 6 " +
                            "tiga kali berturut-turut. Giliran dibatalkan.";
                    }
                    else if (afterRoll.Phase == TurnPhase.SelectingPawn)
                    {
                        message =
                            $"Hasil dadu {playerBeforeRoll.Name}: " +
                            $"{afterRoll.LastDiceValue}. " +
                            "Pilih pawn yang mau dimainkan.";
                    }
                    else if (
                        ReferenceEquals(
                            playerBeforeRoll,
                            afterRoll.CurrentPlayer
                        )
                        &&
                        afterRoll.LastDiceValue == 6
                    )
                    {
                        message =
                            "Hasil kocok dadu adalah 6, tetapi tidak ada pawn yang bisa digerakan. " +
                            "Pemain mendapatkan kesempatan untuk mengocok dadu lagi.";
                    }
                    else
                    {
                        message =
                            $"Hasil dadu {afterRoll.LastDiceValue}. " +
                            "Tidak ada pawn yang bisa digerakan; melewati giliran.";
                    }

                    continue;
                }

                if (state.Phase == TurnPhase.SelectingPawn)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"Hasil dadu: {state.LastDiceValue}"
                    );
                    Console.WriteLine("Pawn yang bisa dimainkan:");

                    foreach (IPawn pawn in state.MovablePawns
                                 .OrderBy(pawn => pawn.Id))
                    {
                        Console.WriteLine(
                            $"  {pawn.Id + 1}. " +
                            $"{DescribeMove(board, pawn, state.LastDiceValue)}"
                        );
                    }

                    int selectedPawnId;

                    while (true)
                    {
                        Console.Write("Pilih pawn yang mau dimainkan: ");

                        if (int.TryParse(
                                Console.ReadLine(),
                                out int selectedNumber))
                        {
                            selectedPawnId = selectedNumber - 1;

                            if (state.MovablePawns.Any(pawn => pawn.Id == selectedPawnId))
                            {
                                break;
                            }
                        }

                        Console.WriteLine(
                            "Pawn tidak dapat di pilih."
                        );
                    }

                    Dictionary<IPawn, (PawnStatus Status, int StepIndex)> beforeMove = state.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .ToDictionary(
                            pawn => pawn,
                            pawn => (pawn.Status, pawn.StepIndex)
                        );

                    IPawn selectedPawn = state.MovablePawns.Single(pawn => pawn.Id == selectedPawnId
                    );

                    string selectedLabel = GetPawnLabel(selectedPawn);

                    string moveDescription = DescribeMove(
                        board,
                        selectedPawn,
                        state.LastDiceValue
                    );
                    
                    controller.SelectPawn(selectedPawnId);

                    GameState afterMove = currentState
                                          ?? throw new InvalidOperationException(
                                              "State tidak tersedia setelah pawn bergerak."
                                          );

                    List<string> captured = afterMove.PlayerPawns
                        .SelectMany(pair => pair.Value)
                        .Where(pawn =>
                            beforeMove.TryGetValue(
                                pawn,
                                out (PawnStatus Status, int StepIndex) oldState
                            )
                            &&
                            oldState.Status != PawnStatus.InBase
                            &&
                            pawn.Status == PawnStatus.InBase
                        )
                        .Select(GetPawnLabel)
                        .ToList();

                    message = captured.Count > 0
                        ? $"{moveDescription}; {selectedLabel} menendang " +
                          $"{string.Join(", ", captured)} kembali ke base."
                        : $"{moveDescription}.";
                }
            }

            DisplayWinnerIfReady();
        }
        finally
        {
            controller.OnStateChange -= HandleStateChange;
            controller.OnPlayerWon -= HandlePlayerWon;
        }
    }

    private static string DescribeMove(IBoard board, IPawn pawn, int diceValue)
    {
        if (pawn.Status == PawnStatus.InBase)
            return $"{GetPawnLabel(pawn)} keluar dari BASE ke START";

        IReadOnlyList<Position> path = board.GetFullPath(pawn.Color);
        int targetIndex = pawn.StepIndex + diceValue;
        Position target = path[targetIndex];

        if (targetIndex == path.Count - 1)
            return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke CENTER/FINISH";

        if (targetIndex >= 52)
            return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke Home kolom ({target.Row},{target.Column})";

        return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke kolom ({target.Row},{target.Column})";
    }

    private static void ShowWinner(IPlayer player)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"Pemenang: {player.Name.ToUpper()} " +
                          $"({GetColorName(player.Color)})");
        Console.WriteLine("Semua pawn pemain telah mencapai CENTER.");
        Console.WriteLine("========================================");
        Console.WriteLine("Tekan ENTER untuk keluar...");
        Console.ReadLine();
    }
}