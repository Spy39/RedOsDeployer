using System.Diagnostics;
using Spectre.Console;

namespace RedOsDeployer.Services;

public static class BashRunner
{
    /// <summary>
    /// Запускает Bash-скрипт, выводит логи в реальном времени и возвращает код завершения.
    /// </summary>
    public static async Task<int> RunAsync(string scriptPath, string arguments = "")
    {
        if (!File.Exists(scriptPath))
        {
            AnsiConsole.MarkupLine($"[red]Ошибка: Скрипт по пути '{scriptPath}' не найден![/]");
            return -1; // -1 будет означать, что файл даже не запустился
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = "bash", // Запускаем сам интерпретатор bash
            Arguments = $"{scriptPath} {arguments}", // Передаем ему путь к нашему скрипту
            RedirectStandardOutput = true, // Перехватываем обычный вывод
            RedirectStandardError = true, // Перехватываем ошибки
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        // Событие: когда bash делает echo (обычный лог)
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                // EscapeMarkup защищает от падения, если в логе будут скобки [ или ]
                AnsiConsole.MarkupLine($"[grey] {e.Data.EscapeMarkup()}[/]");
            }
        };

        // Событие: когда bash плюется ошибками
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                AnsiConsole.MarkupLine($"[red] ОШИБКА: {e.Data.EscapeMarkup()}[/]");
            }
        };

        process.Start();

        // Начинаем асинхронное чтение потоков
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Ждем завершения скрипта, не блокируя основной поток программы
        await process.WaitForExitAsync();

        return process.ExitCode; // 0 - успех, всё остальное - ошибка
    }
}