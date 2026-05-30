using System.IO;
using System.Text.Json;

namespace RedOsDeployer.Services;

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(AppPaths.BaseDirectory, "config.json");

    // Свойство, через которое вся программа будет иметь доступ к настройкам
    public static AppConfig Config { get; private set; } = new AppConfig();

    /// <summary>
    /// Загружает настройки из JSON или создает файл по умолчанию.
    /// </summary>
    public static void LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            // Если файла нет, генерируем дефолтные настройки
            Config = new AppConfig
            {
                TargetPackages = ["r7-office", "r7organizer", "R7Grafika", "yandex-browser-stable", "max", "redoswelcome"],
                Printers =
                [
                    new PrinterConfig { Name = "Brother (T220, L2500DR)", FolderName = "BROTHER", InstallScript = "install.sh" },
                    new PrinterConfig { Name = "Canon MF (3010, 264dw)", FolderName = "CANON_MF", InstallScript = "install.sh" },
                    new PrinterConfig { Name = "Canon LBP (L11121E / LBP2900)", FolderName = "CANON_LBP", InstallScript = "install.sh" },
                    new PrinterConfig { Name = "HP (M134fn, 179fnw, 1510)", FolderName = "HP", InstallScript = "install.sh" }
                ]
            };

            // Записываем в файл с красивым форматированием (с отступами)
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Config, options);
            File.WriteAllText(ConfigPath, json);

            LoggerService.LogInfo("Создан стандартный файл конфигурации config.json");
        }
        else
        {
            // Если файл есть, просто читаем его
            string json = File.ReadAllText(ConfigPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

            LoggerService.LogInfo("Конфигурация успешно загружена.");
        }
    }
}