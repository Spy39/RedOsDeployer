using System.Diagnostics;
using Spectre.Console;

namespace RedOsDeployer.Services;

public static class BashRunner
{
    /// <summary>
    /// Запускает Bash-скрипт с красивым спиннером в UI и пишет подробности в лог-файл.
    /// </summary>
    public static async Task<int> RunWithSpinnerAsync(string taskTitle, string scriptName, string arguments = "")
    {
        // Автоматически ищем скрипт в нашей сгенерированной папке scripts
        string scriptPath = Path.Combine(AppPaths.Scripts, scriptName);

        if (!File.Exists(scriptPath))
        {
            LoggerService.LogError($"Скрипт не найден: {scriptPath}");
            AnsiConsole.MarkupLine($"[red]Ошибка: Скрипт '{scriptName}' не найден в папке scripts![/]");
            return -1;
        }

        // Запускаем UI-спиннер Spectre.Console
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots) // Стиль анимации (точки)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync($"[yellow]{taskTitle}[/]", async ctx =>
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"{scriptPath} {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };

                // Перехват обычного вывода (stdout)
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        // 1. Пишем полный лог в файл (для истории)
                        LoggerService.LogInfo(e.Data);

                        // 2. Обновляем текст под спиннером в интерфейсе
                        // Обрезаем слишком длинные строки, чтобы UI не дергался
                        string safeText = e.Data.Length > 60 ? e.Data.Substring(0, 57) + "..." : e.Data;
                        ctx.Status($"[grey]{safeText.EscapeMarkup()}[/]");
                    }
                };

                // Перехват ошибок (stderr)
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        LoggerService.LogError(e.Data);
                        ctx.Status($"[red]Ошибка: {e.Data.EscapeMarkup()}[/]");
                    }
                };

                LoggerService.LogInfo($"--- Запуск скрипта: {scriptName} ---");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                LoggerService.LogInfo($"--- Завершение скрипта: {scriptName} (Код: {process.ExitCode}) ---");

                return process.ExitCode;
            });
    }
}