using System.IO;
using System.Linq;
using Spectre.Console;

namespace RedOsDeployer.Services;

public static class SystemChecker
{
    public static async Task ShowDashboardAsync()
    {
        bool isOnline = await NetworkService.IsOnlineAsync();
        string networkStatus = isOnline ? "[green]Онлайн[/]" : "[red]Офлайн[/]";

        bool isUefi = Directory.Exists("/sys/firmware/efi");
        string bootMode = isUefi ? "[cyan]UEFI (Современный)[/]" : "[yellow]Legacy BIOS (Старый)[/]";

        // Проверка прав Администратора (Root в Linux)
        // В Linux у пользователя root имя всегда "root". В Windows это проверка не сработает, но для Linux идеально.
        bool isRoot = Environment.UserName == "root";
        string rightsStatus = isRoot ? "[green]Root (Суперпользователь)[/]" : "[red]Обычный (Требуется sudo!)[/]";

        // Проверка наполнения флешки
        // Проверка наполнения флешки и получение даты обновления
        bool hasRepo = Directory.Exists(AppPaths.Repo) && Directory.EnumerateFileSystemEntries(AppPaths.Repo).Any();
        bool hasApps = Directory.Exists(AppPaths.Apps) && Directory.EnumerateFileSystemEntries(AppPaths.Apps).Any();

        string repoDate = hasRepo ? Directory.GetLastWriteTime(AppPaths.Repo).ToString("dd.MM.yyyy HH:mm") : "";
        string appsDate = hasApps ? Directory.GetLastWriteTime(AppPaths.Apps).ToString("dd.MM.yyyy HH:mm") : "";

        string repoStatus = hasRepo ? $"[green]Доступен[/] [grey](Обновлен: {repoDate})[/]" : "[red]Отсутствует[/]";
        string appsStatus = hasApps ? $"[green]Доступны[/] [grey](Обновлен: {appsDate})[/]" : "[red]Отсутствуют[/]";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]Сводка о системе и носителях[/]")
            .AddColumn(new TableColumn("Параметр").Centered())
            .AddColumn(new TableColumn("Значение").Centered());

        table.AddRow("Статус сети", networkStatus);
        table.AddRow("Режим загрузки", bootMode);
        table.AddRow("Права доступа", rightsStatus);
        table.AddRow("Локальный Репозиторий (repo)", repoStatus);
        table.AddRow("Целевые программы (apps)", appsStatus);

        AnsiConsole.Write(table);

        // Красное предупреждение, если забыли sudo
        if (!isRoot && Environment.OSVersion.Platform == PlatformID.Unix)
        {
            AnsiConsole.MarkupLine("[bold red]ВНИМАНИЕ:[/] Программа запущена без прав суперпользователя. Установка пакетов завершится ошибкой! Перезапустите через [cyan]sudo ./RedOsDeployer[/]");
        }
        AnsiConsole.WriteLine();
    }
}