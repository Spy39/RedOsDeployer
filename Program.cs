using RedOsDeployer.Services;
using Spectre.Console;
using System.Linq;

// Очищаем консоль при старте
AnsiConsole.Clear();

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
    AnsiConsole.MarkupLine("[yellow]Выполняется откат сетевых настроек...[/]");
    AnsiConsole.MarkupLine("[green]Программа безопасно завершена.[/]");
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
AnsiConsole.MarkupLine("[grey]Версия:[/] [bold white]1.0.0[/]");
AnsiConsole.MarkupLine("[grey]Автор:[/] [bold cyan]Spy[/]");
// Используем тег link, чтобы в современных терминалах (в т.ч. Linux) можно было кликнуть по ссылке
AnsiConsole.MarkupLine("[grey]GitHub:[/] [link=https://github.com/Spy39/RedOsDeployer]https://github.com/Spy39/RedOsDeployer[/]");
AnsiConsole.WriteLine();

// Бесконечный цикл меню
while (true)
{
    // Выводим дашборд
    await SystemChecker.ShowDashboardAsync();

    // Создаем главное меню (обратите внимание на двойные скобки [[ ]])
    var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Выберите необходимое действие:[/]")
                .PageSize(18) // Увеличим размер страницы, чтобы влезли все категории
                .HighlightStyle(new Style(foreground: Color.Cyan1))
                .AddChoices(new[] {
                "[grey]=== БАЗОВОЕ РАЗВЕРТЫВАНИЕ ===[/]",
                "1. Офлайн обновление ОС (dnf update с USB)",
                "2. Онлайн обновление ОС (докачивание из сети)",
                " ", // Пустая строка для отступа

                "[grey]=== УСТАНОВКА ПО И ПЕРИФЕРИИ ===[/]",
                "3. Установка пакетов (Р7, Яндекс, MAX)",
                "4. Установка КриптоПро CSP",
                "5. Настройка принтеров",
                "  ", // Двойной пробел, чтобы строки не дублировались для C#

                "[grey]=== СИСТЕМНЫЕ НАСТРОЙКИ ===[/]",
                "6. Базовые твики (Время Dual-Boot, Служба печати, Шрифты)",
                "7. Настройка GRUB (Dual-Boot с Windows)",
                "8. Удаление старых (фантомных) ядер Linux",
                "   ", // Тройной пробел

                "[grey]=== ОБСЛУЖИВАНИЕ ФЛЕШКИ (Нужен интернет) ===[/]",
                "9. Синхронизация репозитория (скачать новые патчи ОС)",
                "10. Обновление пакетов ПО (докачать свежие .rpm в папку apps)",
                "    ",

                "[grey]=== ПРОВЕРКА ===[/]",
                "11. Финальная диагностика",
                "0. Выход"
                }));

    // Если пользователь случайно нажал Enter на заголовке или пустой строке — просто перерисовываем меню
    if (choice.StartsWith("[grey]") || string.IsNullOrWhiteSpace(choice))
    {
        AnsiConsole.Clear();
        continue; // Возвращаемся в начало цикла while
    }

    // Обрабатываем выбор пользователя (строки теперь совпадают 1 в 1)
    // Обрабатываем выбор пользователя (теперь строки совпадают ИДЕАЛЬНО)
    switch (choice)
    {
        case "1. Офлайн обновление ОС (dnf update с USB)":
            // Защита: проверяем наличие папки repo
            if (!Directory.Exists(AppPaths.Repo) || !Directory.EnumerateFileSystemEntries(AppPaths.Repo).Any())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Папка 'repo' пуста или отсутствует. Офлайн обновление невозможно.[/]");
                break;
            }

            AnsiConsole.MarkupLine("\n[cyan]Подготовка к офлайн обновлению...[/]");
            int resultUpdate = await BashRunner.RunWithSpinnerAsync(
                "Выполняется локальное обновление (может занять несколько минут)...",
                "1-auto_install.sh",
                AppPaths.Repo // Передаем путь к репо аргументом в баш
            );

            // Красивый вывод результата
            var panelUpdate = new Panel(resultUpdate == 0
                ? "[green]Базовая система успешно обновлена с USB-накопителя.[/]"
                : $"[red]Произошла ошибка (Код: {resultUpdate}). Проверьте файл logs/deploy_log.txt[/]")
            {
                Header = new PanelHeader(resultUpdate == 0 ? "[green]УСПЕХ[/]" : "[red]ОШИБКА[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panelUpdate);
            break;

        case "2. Онлайн обновление ОС (докачивание из сети)":
            if (!await NetworkService.IsOnlineAsync())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Нет подключения к интернету![/]");
                break;
            }
            AnsiConsole.MarkupLine("\n[yellow]Запуск онлайн обновления...[/]");
            break;

        case "3. Установка пакетов (Р7, Яндекс, MAX)":
            if (!Directory.Exists(AppPaths.Apps) || !Directory.EnumerateFileSystemEntries(AppPaths.Apps).Any())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Папка 'apps' пуста. Нечего устанавливать.[/]");
                break;
            }
            AnsiConsole.MarkupLine("\n[yellow]Установка целевого ПО из папки apps...[/]");
            break;

        case "4. Установка КриптоПро CSP":
            AnsiConsole.MarkupLine("\n[yellow]Установка КриптоПро CSP...[/]");
            break;

        // === ИНТЕРФЕЙС ВЫБОРА ПРИНТЕРОВ ИЗ JSON ===
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

            var printerPanel = new Panel($"Модель: [cyan]{selectedPrinter.Name}[/]\nПапка: [grey]{selectedPrinter.FolderName}[/]\nСкрипт: [grey]{selectedPrinter.InstallScript}[/]")
            {
                Header = new PanelHeader("[yellow]Подготовка к установке принтера[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(printerPanel);
            break;
        // ==========================================

        case "6. Базовые твики (Время Dual-Boot, Служба печати, Шрифты)":
            AnsiConsole.MarkupLine("\n[yellow]Применение системных настроек...[/]");
            break;

        case "7. Настройка GRUB (Dual-Boot с Windows)":
            AnsiConsole.MarkupLine("\n[yellow]Настройка загрузчика...[/]");
            break;

        case "8. Удаление старых (фантомных) ядер Linux":
            AnsiConsole.MarkupLine("\n[yellow]Анализ старых ядер...[/]");
            break;

        case "9. Синхронизация репозитория (скачать новые патчи ОС)":
            if (!await NetworkService.IsOnlineAsync())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Для синхронизации нужен интернет![/]");
                break;
            }
            AnsiConsole.MarkupLine("\n[yellow]Обновление папки repo...[/]");
            break;

        case "10. Обновление пакетов ПО (докачать свежие .rpm в папку apps)":
            if (!await NetworkService.IsOnlineAsync())
            {
                AnsiConsole.MarkupLine("\n[red]ОШИБКА: Для скачивания программ нужен интернет![/]");
                break;
            }
            AnsiConsole.MarkupLine("\n[yellow]Загрузка свежих пакетов в apps...[/]");
            break;

        case "11. Финальная диагностика":
            AnsiConsole.MarkupLine("\n[yellow]Сбор сведений о системе...[/]");
            break;

        case "0. Выход":
            // Спрашиваем пользователя перед выходом
            if (AnsiConsole.Confirm("\n[yellow]Очистить историю логов (deploy_log.txt) перед выходом?[/]"))
            {
                if (Directory.Exists(AppPaths.Logs))
                {
                    // Удаляем все файлы внутри папки логов
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
            AnsiConsole.MarkupLine("[grey]Эта функция пока в разработке.[/]");
            break;
    }

    // Пауза перед возвратом в главное меню
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Нажмите любую клавишу для возврата в меню...[/]");
    Console.ReadKey(true);
    AnsiConsole.Clear();
}