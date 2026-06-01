using RedOsDeployer.Services;
using Spectre.Console;
using System.Linq;
using System.IO;
using System;

// Очищаем консоль при старте
AnsiConsole.Clear();

// --- ЖЕСТКАЯ ПРОВЕРКА ПРАВ СУПЕРПОЛЬЗОВАТЕЛЯ ---
if (Environment.UserName != "root")
{
    AnsiConsole.MarkupLine("\n[red]КРИТИЧЕСКАЯ ОШИБКА: Программу необходимо запускать с правами суперпользователя (root)![/]");
    AnsiConsole.MarkupLine("[yellow]Пожалуйста, запустите утилиту командой:[/] [bold cyan]sudo ./RedOsDeployer[/]\n");
    Environment.Exit(1);
}
// ----------------------------------------------

// 1. Инициализация файловой структуры
AppPaths.EnsureDirectoriesExist();
LoggerService.LogInfo("=== Запуск RedOS Deployer ===");

// 2. Загрузка конфигурации
ConfigManager.LoadConfig();

// --- ПЕРЕХВАТ ЗАКРЫТИЯ ПРОГРАММЫ (Ctrl+C) ---
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[red]ВНИМАНИЕ: Получен сигнал прерывания (Ctrl+C)![/]");
    LoggerService.LogError("Работа программы экстренно прервана пользователем.");
    AnsiConsole.MarkupLine("[yellow]Выполняется безопасное завершение...[/]");
    Environment.Exit(0);
};
// --------------------------------------------

// Рисуем красивый ASCII-заголовок
AnsiConsole.Write(
    new FigletText("RedOS Deployer")
        .LeftJustified()
        .Color(Color.Red));

AnsiConsole.MarkupLine("[grey]Универсальная утилита для настройки РЕД ОС в образовательных учреждениях[/]");
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("[grey]Версия:[/] [bold white]2.0.0[/]");
AnsiConsole.MarkupLine("[grey]Автор:[/] [bold cyan]Spy[/]");
AnsiConsole.MarkupLine("[grey]GitHub:[/] [link=https://github.com/Spy39/RedOsDeployer]https://github.com/Spy39/RedOsDeployer[/]");
AnsiConsole.WriteLine();

// Бесконечный цикл меню
while (true)
{
    // Выводим дашборд
    await SystemChecker.ShowDashboardAsync();

    var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Выберите необходимое действие:[/]")
                .PageSize(18)
                .HighlightStyle(new Style(foreground: Color.Cyan1))
                .AddChoices(new[] {
                "[grey]=== БАЗОВОЕ РАЗВЕРТЫВАНИЕ ===[/]",
                "1. Офлайн обновление ОС (dnf update с USB)",
                "2. Онлайн обновление ОС (докачивание из сети)",
                " ",

                "[grey]=== УСТАНОВКА ПО И ПЕРИФЕРИИ ===[/]",
                "3. Установка целевого ПО (Р7, Яндекс, MAX...)",
                "4. Установка КриптоПро CSP",
                "5. Настройка принтеров",
                "  ",

                "[grey]=== СИСТЕМНЫЕ НАСТРОЙКИ ===[/]",
                "6. Базовые твики (Время Dual-Boot, Служба печати, Шрифты)",
                "7. Настройка GRUB (Dual-Boot с Windows)",
                "8. Удаление старых (фантомных) ядер Linux",
                "   ",

                "[grey]=== ОБСЛУЖИВАНИЕ ФЛЕШКИ (Нужен интернет) ===[/]",
                "9. Синхронизация репозитория (скачать патчи ОС)",
                "10. Синхронизация пакетов ПО (докачать свежие .rpm)",
                "11. Быстрая проверка скачанных версий ПО (без интернета)",
                "    ",

                "[grey]=== ПРОВЕРКА ===[/]",
                "12. Финальная диагностика",
                "0. Выход"
                }));

    if (choice.StartsWith("[grey]") || string.IsNullOrWhiteSpace(choice))
    {
        AnsiConsole.Clear();
        continue;
    }

    switch (choice)
    {
        case "1. Офлайн обновление ОС (dnf update с USB)":
            if (!Directory.Exists(AppPaths.Repo) || !Directory.EnumerateFileSystemEntries(AppPaths.Repo).Any())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Папка 'repo' пуста или отсутствует. Офлайн обновление невозможно.[/]");
                break;
            }

            AnsiConsole.MarkupLine("\n[cyan]Запуск локального обновления. Пожалуйста, подождите...[/]");
            int resultUpdate = await BashRunner.RunInteractiveAsync("1-auto_install.sh", $"\"{AppPaths.Repo}\"");

            var panelUpdate = new Panel(resultUpdate == 0
                ? "[green]Базовая система успешно обновлена с USB-накопителя.[/]"
                : $"[red]Произошла ошибка (Код: {resultUpdate}). Проверьте лог или вывод терминала выше.[/]")
            { Header = new PanelHeader(resultUpdate == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelUpdate);
            break;

        case "2. Онлайн обновление ОС (докачивание из сети)":
            if (!await NetworkService.IsOnlineAsync())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Нет подключения к интернету![/]");
                break;
            }
            AnsiConsole.MarkupLine("\n[yellow]Запуск онлайн обновления системы...[/]");

            int resultOnline = await BashRunner.RunInteractiveAsync("bash", "-c \"dnf update -y\"");

            var panelOnline = new Panel(resultOnline == 0
                ? "[green]Система успешно обновлена из сети Интернет.[/]"
                : $"[red]Произошла ошибка (Код: {resultOnline}).[/]")
            { Header = new PanelHeader(resultOnline == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelOnline);
            break;

        case "3. Установка целевого ПО (Р7, Яндекс, MAX...)":
            if (!Directory.Exists(AppPaths.Apps))
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Папка 'apps' отсутствует![/]");
                break;
            }

            // --- АВТООПРЕДЕЛЕНИЕ РЕДАКЦИИ ОС ---
            AnsiConsole.MarkupLine("\n[cyan]Автоопределение редакции РЕД ОС...[/]");
            var (osCode, osOutput) = await BashRunner.ExecuteCommandAsync("cat /etc/os-release /etc/red-release 2>/dev/null");
            bool isCert = osOutput.Contains("cert", StringComparison.OrdinalIgnoreCase) ||
                          osOutput.Contains("серт", StringComparison.OrdinalIgnoreCase);

            string osTypeArgApps = isCert ? "cert" : "std";

            if (isCert)
                AnsiConsole.MarkupLine("[green]Обнаружена: Сертифицированная (8.0c). Пакет redoswelcome будет исключен.[/]");
            else
                AnsiConsole.MarkupLine("[green]Обнаружена: Образовательная/Стандартная (8.0).[/]");
            // -----------------------------------

            // Вопрос: Логика Р7 Офис
            var r7Choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\n[cyan]Устанавливать Р7-Офис? (Установка Р7 удалит встроенный LibreOffice)[/]")
                    .AddChoices("1. Да, установить Р7-Офис", "2. Нет, оставить LibreOffice")
            );

            var packagesList = ConfigManager.Config.TargetPackages.ToList();
            if (r7Choice.Contains("2."))
            {
                packagesList.RemoveAll(p => p.Contains("r7", StringComparison.OrdinalIgnoreCase));
            }

            string packagesArgsInstall = string.Join(" ", packagesList);

            AnsiConsole.MarkupLine("\n[yellow]Установка целевого ПО (Этап 1: Офлайн -> Этап 2: Онлайн)...[/]");

            int resultApps = await BashRunner.RunInteractiveAsync(
                "3-install_apps.sh",
                $"\"{AppPaths.Apps}\" {osTypeArgApps} {packagesArgsInstall}"
            );

            var panelApps = new Panel(resultApps == 0
                ? "[green]Целевое ПО успешно установлено.[/]"
                : $"[red]Произошла ошибка (Код: {resultApps}). Проверьте вывод терминала.[/]")
            { Header = new PanelHeader(resultApps == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelApps);
            break;

        case "4. Установка КриптоПро CSP":
            string cryptoDir = Path.Combine(AppPaths.Apps, "CryptoPro");
            if (!Directory.Exists(cryptoDir)) Directory.CreateDirectory(cryptoDir);

            AnsiConsole.MarkupLine("\n[cyan]Поиск архива linux-amd64.tgz в папке apps/CryptoPro...[/]");
            int resultCrypto = await BashRunner.RunWithSpinnerAsync(
                            "Распаковка и установка КриптоПро CSP 5.0...",
                            "install_crypto.sh",
                            $"\"{cryptoDir}\""
                        );

            var panelCrypto = new Panel(resultCrypto == 0
                ? "[green]КриптоПро CSP, GUI-утилиты и драйверы токенов установлены.[/]"
                : $"[red]Ошибка установки. Убедитесь, что архив .tgz лежит в папке {cryptoDir}[/]")
            { Header = new PanelHeader(resultCrypto == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelCrypto);
            break;

        case "5. Настройка принтеров":
            var printerOptions = ConfigManager.Config.Printers.Select(p => p.Name).ToList();
            printerOptions.Add("0. Вернуться назад");

            var printerChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\n[cyan]Выберите модель принтера:[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(foreground: Color.Green))
                    .AddChoices(printerOptions)
            );

            if (printerChoice == "0. Вернуться назад") break;

            var selectedPrinter = ConfigManager.Config.Printers.First(p => p.Name == printerChoice);
            string printerPath = Path.Combine(AppPaths.Apps, selectedPrinter.FolderName);

            AnsiConsole.MarkupLine($"\n[yellow]Подготовка к установке: {selectedPrinter.Name}[/]");
            string cmd = $"cd '{printerPath}' && bash {selectedPrinter.InstallScript}";

            int resultPrinter = await BashRunner.RunWithSpinnerAsync(
                $"Установка драйвера {selectedPrinter.Name}...",
                "bash",
                $"-c \"{cmd}\""
            );

            var panelPrinter = new Panel(resultPrinter == 0
                ? $"[green]Драйвер для {selectedPrinter.Name} установлен.[/]"
                : $"[red]Ошибка установки. Код: {resultPrinter}[/]")
            { Header = new PanelHeader(resultPrinter == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelPrinter);
            break;

        case "6. Базовые твики (Время Dual-Boot, Служба печати, Шрифты)":
            AnsiConsole.MarkupLine("\n[yellow]Применение системных настроек...[/]");

            int resultTweaks = await BashRunner.RunWithSpinnerAsync("Настройка служб ОС...", "system_tweaks.sh");

            var panelTweaks = new Panel(resultTweaks == 0
                ? "[green]Службы печати, шрифты и Dual-Boot время успешно настроены.[/]"
                : $"[red]Произошла ошибка (Код: {resultTweaks}). Проверьте файл логов.[/]")
            { Header = new PanelHeader(resultTweaks == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded };
            AnsiConsole.Write(panelTweaks);
            break;

        case "7. Настройка GRUB (Dual-Boot с Windows)":
            AnsiConsole.MarkupLine("\n[yellow]Анализ меню загрузчика GRUB...[/]");

            bool isUefi = Directory.Exists("/sys/firmware/efi");
            if (isUefi)
            {
                string getMenuCmd = "cat $(find /boot/efi/EFI -name 'grub.cfg' | grep -i 'red' | head -n 1) | awk -F\\' '/menuentry / {print $2}'";
                var (exitCode, output) = await BashRunner.ExecuteCommandAsync(getMenuCmd);

                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var osList = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    osList.Add("0. Отмена");

                    var selectedOs = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("\n[cyan]Выберите систему для загрузки по умолчанию:[/]")
                            .PageSize(10)
                            .HighlightStyle(new Style(foreground: Color.Green))
                            .AddChoices(osList)
                    );

                    if (selectedOs == "0. Отмена") break;

                    await BashRunner.RunWithSpinnerAsync("Установка приоритета...", "bash", $"-c \"grub2-set-default '{selectedOs}' && grub2-mkconfig -o $(find /boot/efi/EFI -name 'grub.cfg' | grep -i 'red' | head -n 1)\"");
                    AnsiConsole.MarkupLine($"\n[green]Готово! Система '[cyan]{selectedOs}[/]' будет загружаться первой.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Не удалось прочитать меню GRUB. Убедитесь, что система использует UEFI.[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Обнаружен Legacy BIOS. Установка Windows по индексу 2...[/]");
                await BashRunner.RunWithSpinnerAsync("Настройка Legacy GRUB...", "bash", "-c \"grub2-set-default 2 && grub2-mkconfig -o /boot/grub2/grub.cfg\"");
                AnsiConsole.MarkupLine("[green]Готово! Приоритет изменен на индекс 2.[/]");
            }
            break;

        case "8. Удаление старых (фантомных) ядер Linux":
            AnsiConsole.MarkupLine("\n[cyan]Поиск старых версий ядра ОС...[/]");
            int resultKernels = await BashRunner.RunWithSpinnerAsync("Очистка фантомных ядер...", "clean_kernels.sh");

            AnsiConsole.Write(new Panel(resultKernels == 0 ? "[green]Старые ядра успешно удалены, место освобождено.[/]" : "[red]Ошибка при удалении ядер (возможно, старых версий нет).[/]")
            { Header = new PanelHeader(resultKernels == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded });
            break;

        case "9. Синхронизация репозитория (скачать патчи ОС)":
            if (!await NetworkService.IsOnlineAsync()) { AnsiConsole.MarkupLine("\n[red]ОШИБКА: Для синхронизации нужен интернет![/]"); break; }

            // --- АВТООПРЕДЕЛЕНИЕ РЕДАКЦИИ ОС ---
            AnsiConsole.MarkupLine("\n[cyan]Автоопределение редакции РЕД ОС...[/]");

            // Читаем системные файлы, чтобы понять, на какой версии ОС мы сейчас запущены
            var (osCodeSync, osOutputSync) = await BashRunner.ExecuteCommandAsync("cat /etc/os-release /etc/red-release 2>/dev/null");
            bool isCertSync = osOutputSync.Contains("cert", StringComparison.OrdinalIgnoreCase) ||
                              osOutputSync.Contains("серт", StringComparison.OrdinalIgnoreCase);

            // Назначаем аргумент для Bash-скрипта (cert или std)
            string osTypeArg = isCertSync ? "cert" : "std";

            if (isCertSync)
                AnsiConsole.MarkupLine("[green]Обнаружена: Сертифицированная (8.0c). Будут загружены базы 'redos-cert' и 'updates-cert'.[/]");
            else
                AnsiConsole.MarkupLine("[green]Обнаружена: Образовательная/Стандартная (8.0). Будут загружены стандартные базы ОС.[/]");
            // -----------------------------------

            AnsiConsole.MarkupLine("\n[yellow]Начинаю загрузку обновлений ОС...[/]");

            // Вызов скрипта 1-sync_os_repo.sh с автоматическим аргументом
            int resultSyncRepo = await BashRunner.RunInteractiveAsync("1-sync_os_repo.sh", $"\"{AppPaths.Repo}\" {osTypeArg}");

            AnsiConsole.Write(new Panel(resultSyncRepo == 0 ? "[green]Офлайн-репозиторий успешно обновлен.[/]" : "[red]Сбой синхронизации. Возможно, прервано пользователем.[/]")
            { Header = new PanelHeader(resultSyncRepo == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded });
            break;

        case "10. Синхронизация пакетов ПО (докачать свежие .rpm)":
            {
                if (!await NetworkService.IsOnlineAsync()) { AnsiConsole.MarkupLine("\n[red]ОШИБКА: Для скачивания программ нужен интернет![/]"); break; }

                string packagesArgsSync = string.Join(" ", ConfigManager.Config.TargetPackages);
                AnsiConsole.MarkupLine("\n[yellow]Загрузка свежих версий ПО...[/]");

                int resultSyncApps = await BashRunner.RunInteractiveAsync("2-sync_apps.sh", $"\"{AppPaths.Apps}\" {packagesArgsSync}");

                if (resultSyncApps == 0)
                {
                    string vFile = Path.Combine(AppPaths.Apps, "versions.txt"); // Переименовали переменную
                    if (File.Exists(vFile))
                    {
                        var table = new Table().Border(TableBorder.Rounded).Title("[cyan]Содержимое локального репозитория apps[/]");
                        table.AddColumn("Программа").AddColumn("Версия");

                        foreach (var line in File.ReadAllLines(vFile))
                        {
                            var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2) table.AddRow($"[green]{parts[0].Trim()}[/]", $"[yellow]{parts[1].Trim()}[/]");
                        }
                        AnsiConsole.Write(table);
                    }
                }

                AnsiConsole.Write(new Panel(resultSyncApps == 0 ? "[green]Пакеты обновлены, локальный индекс пересоздан.[/]" : "[red]Ошибка при скачивании.[/]")
            { Header = new PanelHeader(resultSyncApps == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"), Border = BoxBorder.Rounded });
            }
            break;

        case "11. Быстрая проверка скачанных версий ПО (без интернета)":
            string versionsFile = Path.Combine(AppPaths.Apps, "versions.txt");

            if (File.Exists(versionsFile))
            {
                AnsiConsole.MarkupLine("\n[cyan]Чтение локальной базы данных носителя...[/]");

                var tableVersions = new Table().Border(TableBorder.Rounded).Title("[cyan]Целевое ПО на флешке[/]");
                tableVersions.AddColumn("Программа").AddColumn("Скачанная версия");

                // Читаем наш краткий лог, который создал скрипт 2-sync_apps.sh
                foreach (var line in File.ReadAllLines(versionsFile))
                {
                    var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string statusColor = parts[1].Contains("ОТСУТСТВУЕТ") ? "red" : "yellow";
                        tableVersions.AddRow($"[green]{parts[0].Trim()}[/]", $"[{statusColor}]{parts[1].Trim()}[/]");
                    }
                }
                AnsiConsole.Write(tableVersions);

                // Показываем дату создания файла (дату последней синхронизации)
                DateTime lastModified = File.GetLastWriteTime(versionsFile);
                AnsiConsole.MarkupLine($"\n[grey]Дата последнего обновления папки apps:[/] [bold white]{lastModified:dd.MM.yyyy HH:mm}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[red]Файл отчета не найден![/] [yellow]Сначала выполните пункт 10 (Синхронизация пакетов), чтобы создать базу.[/]");
            }
            break;

        case "12. Финальная диагностика":
            AnsiConsole.MarkupLine("\n[yellow]Сбор сведений о системе...[/]");

            var diagTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[cyan]Отчет о корпоративном ПО[/]")
                .AddColumn(new TableColumn("Программа пакета").Centered())
                .AddColumn(new TableColumn("Статус").Centered());

            var tweaksTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[cyan]Отчет о системных настройках[/]")
                .AddColumn(new TableColumn("Настройка").Centered())
                .AddColumn(new TableColumn("Статус").Centered());

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("[yellow]Выполнение проверок...[/]", async ctx =>
                {
                    foreach (var pkg in ConfigManager.Config.TargetPackages)
                    {
                        var (exitCode, _) = await BashRunner.ExecuteCommandAsync($"rpm -q {pkg}");
                        if (exitCode == 0) diagTable.AddRow(pkg, "[green]УСТАНОВЛЕНО[/]");
                        else diagTable.AddRow(pkg, "[red]ОШИБКА / НЕТ[/]");
                    }

                    var (cupsCode, _) = await BashRunner.ExecuteCommandAsync("systemctl is-active cups");
                    tweaksTable.AddRow("Служба печати (CUPS)", cupsCode == 0 ? "[green]АКТИВНА[/]" : "[red]ОТКЛЮЧЕНА[/]");

                    var (timeCode, _) = await BashRunner.ExecuteCommandAsync("timedatectl | grep -q 'RTC in local TZ: yes'");
                    tweaksTable.AddRow("Время Dual-Boot (Local RTC)", timeCode == 0 ? "[green]ВКЛЮЧЕНО[/]" : "[red]СТАНДАРТ (UTC)[/]");

                    var (kernelCode, kernelOut) = await BashRunner.ExecuteCommandAsync("dnf repoquery --installonly -q | wc -l");
                    tweaksTable.AddRow("Установлено ядер ОС", $"[cyan]{kernelOut.Trim()} шт.[/]");

                    var (grubCode, grubOut) = await BashRunner.ExecuteCommandAsync("grubby --default-title");
                    tweaksTable.AddRow("Загрузчик по умолчанию", $"[cyan]{grubOut.Trim()}[/]");
                });

            AnsiConsole.Write(diagTable);
            AnsiConsole.WriteLine();
            AnsiConsole.Write(tweaksTable);
            AnsiConsole.MarkupLine("\n[green]=== ДИАГНОСТИКА ЗАВЕРШЕНА ===[/]");
            break;

        case "0. Выход":
            if (AnsiConsole.Confirm("\n[yellow]Очистить историю логов (deploy_log.txt) перед выходом?[/]"))
            {
                if (Directory.Exists(AppPaths.Logs))
                {
                    foreach (var file in Directory.GetFiles(AppPaths.Logs))
                    {
                        File.Delete(file);
                    }
                    AnsiConsole.MarkupLine("[green]Папка логов очищена.[/]");
                }
            }
            AnsiConsole.MarkupLine("\n[red]Завершение работы...[/]");
            return;

        default:
            AnsiConsole.MarkupLine($"\n[cyan]Вы выбрали: {choice}[/]");
            break;
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Нажмите любую клавишу для возврата в меню...[/]");
    Console.ReadKey(true);
    AnsiConsole.Clear();
}