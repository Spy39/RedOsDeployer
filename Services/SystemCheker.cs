using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;

namespace RedOsDeployer.Services;

public static class SystemChecker
{
    public static async Task ShowDashboardAsync()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[yellow]Сводка о системе и примененных настройках[/]")
            .AddColumn(new TableColumn("[cyan]Системный параметр[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Текущее состояние[/]").Centered());

        // --- АВТООПРЕДЕЛЕНИЕ РЕДАКЦИИ ОС ---
        var (osCode, osOutput) = await BashRunner.ExecuteCommandAsync("cat /etc/os-release /etc/red-release 2>/dev/null");
        bool isCert = osOutput.Contains("cert", StringComparison.OrdinalIgnoreCase) ||
                      osOutput.Contains("серт", StringComparison.OrdinalIgnoreCase);

        table.AddRow("Редакция РЕД ОС", isCert ? "[green]Сертифицированная (8.0c)[/]" : "[green]Образовательная/Стандартная (8.0)[/]");

        // --- БАЗОВЫЕ ПРОВЕРКИ ---
        bool isOnline = await NetworkService.IsOnlineAsync();
        table.AddRow("Статус сети", isOnline ? "[green]Онлайн[/]" : "[red]Офлайн[/]");

        bool isUefi = Directory.Exists("/sys/firmware/efi");
        table.AddRow("Режим загрузки", isUefi ? "[green]UEFI[/]" : "[yellow]Legacy BIOS[/]");

        bool repoExists = Directory.Exists(Path.Combine(AppPaths.Repo, "std", "base")) ||
                          Directory.Exists(Path.Combine(AppPaths.Repo, "cert", "base"));
        table.AddRow("Локальный Репозиторий (repo)", repoExists ? "[green]Доступен[/]" : "[red]Не скачан[/]");


        // --- ПРОВЕРКИ ПРИМЕНЕННЫХ ТВИКОВ ---
        var (cupsCode, _) = await BashRunner.ExecuteCommandAsync("systemctl is-active cups");
        table.AddRow("Служба печати (CUPS)", cupsCode == 0 ? "[green]Включена[/]" : "[red]Отключена[/]");

        var (rtcCode, _) = await BashRunner.ExecuteCommandAsync("timedatectl | grep -q 'RTC in local TZ: yes'");
        table.AddRow("Время для Dual-Boot", rtcCode == 0 ? "[green]Адаптировано (Local)[/]" : "[yellow]Стандарт (UTC)[/]");

        var (grubCode, grubOut) = await BashRunner.ExecuteCommandAsync("grubby --default-title 2>/dev/null");
        string defaultOs = string.IsNullOrWhiteSpace(grubOut) ? "Неизвестно" : grubOut;
        string osColor = defaultOs.Contains("Windows") ? "yellow" : "green";
        table.AddRow("ОС по умолчанию (GRUB)", $"[{osColor}]{defaultOs}[/]");

        var (kernelCode, kernelOut) = await BashRunner.ExecuteCommandAsync("dnf repoquery --installonly -q 2>/dev/null | wc -l");
        table.AddRow("Количество ядер ОС", int.TryParse(kernelOut, out int kCount) && kCount > 2 ? $"[red]{kCount} шт. (Нужна очистка)[/]" : $"[green]{kCount} шт. (Норма)[/]");

        // --- ПРОВЕРКА КРИПТОПРО (ТОЛЬКО ДЛЯ СЕРТИФИЦИРОВАННОЙ ОС) ---
        if (isCert)
        {
            var (cryptoCode, _) = await BashRunner.ExecuteCommandAsync("rpm -q lsb-cprocsp-base");
            var (pcscdCode, _) = await BashRunner.ExecuteCommandAsync("systemctl is-active pcscd");

            if (cryptoCode == 0 && pcscdCode == 0)
            {
                table.AddRow("КриптоПро CSP", "[green]Установлен (Служба токенов активна)[/]");
            }
            else if (cryptoCode == 0)
            {
                table.AddRow("КриптоПро CSP", "[yellow]Установлен (Служба токенов отключена)[/]");
            }
            else
            {
                table.AddRow("КриптоПро CSP", "[red]Не установлен[/]");
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}