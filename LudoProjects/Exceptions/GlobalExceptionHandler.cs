namespace LudoProjects.Exceptions;

public static class GlobalExceptionHandler
{
    public static void Run(Action? application)
    {
        if (application is null)
        {
            ShowError("Aplikasi tidak dapat dijalankan karena action tidak tersedia.");
            return;
        }

        try
        {
            application();
        }
        catch (ArgumentNullException exception)
        {
            ShowError("Data yang dibutuhkan belum tersedia.", exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ShowError("Nilai yang diberikan berada di luar batas yang diperbolehkan.", exception);
        }
        catch (ArgumentException exception)
        {
            ShowError(exception.Message, exception);
        }
        catch (InvalidOperationException exception)
        {
            ShowError(exception.Message, exception);
        }
        catch (Exception exception)
        {
            ShowError("Terjadi kesalahan yang tidak terduga pada permainan.", exception);
        }
    }

    private static void ShowError(string userMessage, Exception? exception = null)
    {
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine();
        Console.WriteLine("=== LUDO ERROR ===");
        Console.WriteLine(userMessage);

        if (exception is not null)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Jenis error: {exception.GetType().Name}");

            Console.WriteLine(
                $"Detail: {exception.Message}");
        }

        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine(
            "Tekan tombol apa saja untuk menutup aplikasi.");

        Console.ReadKey(true);
    }
}