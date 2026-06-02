using System.Diagnostics;
using Spectre.Console;

namespace RedOsDeployer.Services;

public static class BashRunner
{
    /// <summary>
    /// Запускает Bash-скрипт с красивым спиннером в UI и пишет подробности в лог-файл.
    /// </summary>
    /// <summary>
    /// Запускает Bash-скрипт с красивым спиннером в UI и пишет ПОЛНЫЙ вывод в лог-файл.
    /// </summary>
    public static async Task<int> RunWithSpinnerAsync(string spinnerText, string scriptName, string arguments = "")
    {
        string executableFile = scriptName;
        bool isScript = scriptName.EndsWith(".sh");

        if (isScript)
        {
            executableFile = Path.Combine(AppPaths.Scripts, scriptName);
            if (!File.Exists(executableFile))
            {
                LoggerService.LogError($"Скрипт не найден: {executableFile}");
                AnsiConsole.MarkupLine($"[red]Ошибка: Скрипт '{scriptName}' не найден в папке scripts![/]");
                return -1;
            }
        }

        LoggerService.LogInfo($"--- Запуск {(isScript ? "скрипта" : "команды")}: {scriptName} ---");

        int exitCode = -1;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"[yellow]{spinnerText}[/]", async ctx =>
            {
                string finalArguments = isScript ? $"\"{executableFile}\" {arguments}" : arguments;
                string processFileName = isScript ? "bash" : executableFile;

                var processInfo = new ProcessStartInfo
                {
                    FileName = processFileName,
                    Arguments = finalArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                // Читаем оба потока параллельно
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

                exitCode = process.ExitCode;
                string standardOutput = outputTask.Result;
                string errorOutput = errorTask.Result;

                // --- РАСШИРЕННОЕ ЛОГИРОВАНИЕ ---
                // Записываем весь стандартный вывод скрипта в файл логов (невидимо для пользователя)
                if (!string.IsNullOrWhiteSpace(standardOutput))
                {
                    LoggerService.LogInfo($"[ПОДРОБНЫЙ ВЫВОД {scriptName}]:\n{standardOutput.Trim()}");
                }

                // Записываем все ошибки
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    // Bash часто пишет предупреждения в поток ошибок, даже если exitCode == 0, поэтому пишем всё
                    string logPrefix = exitCode == 0 ? "ПРЕДУПРЕЖДЕНИЯ" : "КРИТИЧЕСКИЕ ОШИБКИ";
                    LoggerService.LogError($"[{logPrefix} {scriptName}]:\n{errorOutput.Trim()}");
                }
            });

        LoggerService.LogInfo($"--- Завершение {(isScript ? "скрипта" : "команды")}: {scriptName} (Код: {exitCode}) ---");
        return exitCode;
    }
    
         /// <summary>
         /// Выполняет сырую bash-команду "тихо" (без вывода на экран) и возвращает её текстовый результат и код.
         /// </summary>
    public static async Task<(int ExitCode, string Output)> ExecuteCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"", // Передаем сырую команду
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();

        // Читаем весь ответ от консоли Linux
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output.Trim());
    }

    /// <summary>
    /// Выполняет команду, выводя весь прогресс напрямую в консоль пользователя (без спиннера).
    /// Идеально для dnf update и reposync, где нужно видеть проценты скачивания.
    /// </summary>
    public static async Task<int> RunInteractiveAsync(string scriptName, string arguments = "")
    {
        if (File.Exists(Path.Combine(AppPaths.Apps, "install_report.txt")))
        {
            string report = File.ReadAllText(Path.Combine(AppPaths.Apps, "install_report.txt"));
            // Здесь рисуем таблицу через Spectre.Console.Table
        }

        string executableFile = scriptName;
        bool isScript = scriptName.EndsWith(".sh");

        if (isScript)
        {
            executableFile = Path.Combine(AppPaths.Scripts, scriptName);
            if (!File.Exists(executableFile)) return -1;
        }

        string finalArguments = isScript ? $"\"{executableFile}\" {arguments}" : arguments;
        string processFileName = isScript ? "bash" : executableFile;

        var processInfo = new ProcessStartInfo
        {
            FileName = processFileName,
            Arguments = finalArguments,
            // ВАЖНО: false означает, что мы не прячем вывод, а отдаем его прямо в терминал
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}