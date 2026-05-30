using System.Collections.Generic;

namespace RedOsDeployer.Services;

// Главный класс настроек
public class AppConfig
{
    // Список пакетов для установки и проверки
    public List<string> TargetPackages { get; set; } = new();

    // Список принтеров
    public List<PrinterConfig> Printers { get; set; } = new();
}

// Настройки для конкретного принтера
public class PrinterConfig
{
    public string Name { get; set; } = ""; // Как это будет выглядеть в меню (напр. "HP M134fn")
    public string FolderName { get; set; } = ""; // Имя папки на флешке (напр. "HP")
    public string InstallScript { get; set; } = "install.sh"; // Какой скрипт запускать внутри
}