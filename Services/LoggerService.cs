using System.IO;

namespace RedOsDeployer.Services;

public static class LoggerService
{
    private static readonly string LogFilePath = Path.Combine(AppPaths.Logs, "deploy_log.txt");

    /// <summary>
    /// Записывает сообщение в файл лога с указанием времени.
    /// </summary>
    public static void LogInfo(string message)
    {
        WriteToFile($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    /// <summary>
    /// Записывает ошибку в файл лога.
    /// </summary>
    public static void LogError(string message)
    {
        WriteToFile($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    private static void WriteToFile(string logLine)
    {
        try
        {
            // AppendAllText создает файл, если его нет, и дописывает в конец, если есть
            File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
        }
        catch
        {
            // Если не удалось записать (например, нет прав), просто игнорируем, 
            // чтобы программа не упала посреди установки.
        }
    }
}