using System.IO;

namespace RedOsDeployer.Services;

public static class AppPaths
{
    // Получаем путь к папке, где лежит наш .exe (или бинарник в Linux)
    public static string BaseDirectory => AppContext.BaseDirectory;
    
    // Автоматически генерируем пути к нужным нам папкам
    public static string Scripts => Path.Combine(BaseDirectory, "scripts");
    public static string Repo => Path.Combine(BaseDirectory, "repo");
    public static string Apps => Path.Combine(BaseDirectory, "apps");
    public static string Logs => Path.Combine(BaseDirectory, "logs");

    /// <summary>
    /// Проверяет базовую структуру папок и создает их, если они отсутствуют.
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(Scripts)) Directory.CreateDirectory(Scripts);
        if (!Directory.Exists(Repo)) Directory.CreateDirectory(Repo);
        if (!Directory.Exists(Apps)) Directory.CreateDirectory(Apps);
        if (!Directory.Exists(Logs)) Directory.CreateDirectory(Logs);
    }
}