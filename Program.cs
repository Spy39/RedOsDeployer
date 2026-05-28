using Spectre.Console;

// Очищаем консоль при старте
AnsiConsole.Clear();

// Рисуем красивый ASCII-заголовок
AnsiConsole.Write(
    new FigletText("RedOS Deployer")
        .LeftJustified()
        .Color(Color.Red));

AnsiConsole.MarkupLine("[grey]Универсальная утилита для настройки РЕД ОС[/]");
AnsiConsole.WriteLine();

// Бесконечный цикл меню, чтобы программа не закрывалась после одного действия
while (true)
{
    // Создаем интерактивное меню
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[green]Выберите необходимое действие:[/]")
            .PageSize(10) // Количество пунктов на экране
            .HighlightStyle(new Style(foreground: Color.Cyan1))
            .MoreChoicesText("[grey](Используйте стрелочки вверх/вниз для навигации)[/]")
            .AddChoices(new[] {
                "1. Офлайн обновление ОС (с USB-накопителя)",
                "2. Онлайн обновление ОС (докачивание патчей)",
                "3. Установка целевого ПО (Р7, Яндекс, MAX)",
                "4. Установка КриптоПро CSP",
                "5. Настройка принтеров",
                "6. Настройка GRUB (Dual-Boot с Windows)",
                "7. Диагностика и чек-лист системы",
                "0. Выход"
            }));

    // Обрабатываем выбор пользователя
    switch (choice)
    {
        case "1. Офлайн обновление ОС (с USB-накопителя)":
            AnsiConsole.MarkupLine("\n[yellow]Запуск модуля офлайн обновления...[/]");

            // Вызываем наш BashRunner
            int result = await RedOsDeployer.Services.BashRunner.RunAsync("scripts/1-auto_install.sh");

            if (result == 0)
            {
                AnsiConsole.MarkupLine("[green]Обновление успешно завершено![/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Произошла ошибка! Код завершения: {result}[/]");
            }
            break;

        case "2. Онлайн обновление ОС (докачивание патчей)":
            AnsiConsole.MarkupLine("\n[yellow]Проверка сети и запуск онлайн обновления...[/]");
            break;

        case "3. Установка целевого ПО (Р7, Яндекс, MAX)":
            AnsiConsole.MarkupLine("\n[yellow]Распаковка и установка пакетов...[/]");
            break;

        case "0. Выход":
            AnsiConsole.MarkupLine("\n[red]Завершение работы...[/]");
            return; // Выход из приложения

        default:
            // Заглушка для остальных пунктов
            AnsiConsole.MarkupLine($"\n[cyan]Вы выбрали: {choice}[/]");
            AnsiConsole.MarkupLine("[grey]Эта функция пока в разработке.[/]");
            break;
    }

    // Пауза перед возвратом в главное меню
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Нажмите любую клавишу для возврата в меню...[/]");
    Console.ReadKey(true);
    AnsiConsole.Clear(); // Стираем старый вывод, чтобы меню всегда было сверху
}