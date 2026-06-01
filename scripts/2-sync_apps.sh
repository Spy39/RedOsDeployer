#!/bin/bash
# ==============================================================================
# Скрипт интеллектуальной загрузки и обновления пакетов (.rpm)
# Автор: Spy
# GitHub: https://github.com/Spy39
# 
# Назначение: Выкачивает свежие версии программ из сети, удаляет старые
# дубликаты из папки и пересобирает локальную базу (repodata).
# ==============================================================================

APPS_DIR="$1"
shift

# Железобетонный парсинг аргументов: режем всю оставшуюся строку по пробелам в массив.
# Это спасает от багов, если C# передаст строку с лишними кавычками.
read -r -a PACKAGES <<< "$@"

# Защита от дурака: проверяем, что передали папку и хотя бы один пакет
if [ -z "$APPS_DIR" ] || [ ${#PACKAGES[@]} -eq 0 ]; then 
    echo "Использование: sudo bash 2-sync_apps.sh /путь/к/apps пакет1 пакет2 ..."
    exit 1 
fi

# 1. Устанавливаем базовые зависимости для работы с репозиториями
dnf install createrepo_c dnf-utils yum-utils -y >/dev/null 2>&1
mkdir -p "$APPS_DIR"

# Костыль для первичного запуска: создаем пустую базу до начала всех проверок.
# Если этого не сделать, repomanage позже выкинет Curl error (37).
if [ ! -d "$APPS_DIR/repodata" ]; then
    createrepo_c -v --compress-type=zstd --general-compress-type=zstd "$APPS_DIR" >/dev/null 2>&1
fi

echo "--- Подготовка репозиториев ---"

# 2. Подключаем коммерческие репозитории
# Устанавливаем пакет-указатель для Р7-Офис, чтобы DNF увидел их сервера
dnf install r7-release -y >/dev/null 2>&1

# Временно монтируем репозитории Яндекса и MAX.
# Используем уникальные имена (yandex-temp, max-temp), чтобы DNF не спамил
# варнингами о дублировании репозиториев, если они уже есть в системе.
tee /etc/yum.repos.d/temp-sync.repo >/dev/null <<EOF
[yandex-temp]
name=Yandex Temp
baseurl=http://repo.yandex.ru/yandex-browser/rpm/stable/x86_64
enabled=1
gpgcheck=0

[max-temp]
name=MAX Temp
baseurl=https://download.max.ru/linux/rpm/el/9/x86_64
enabled=1
gpgcheck=0
EOF

# Форсированно обновляем кэш DNF, чтобы он подхватил только что добавленные сервера
dnf config-manager --set-enabled \*r7\* >/dev/null 2>&1 || true
dnf makecache >/dev/null 2>&1

# 3. Основной цикл скачивания
echo "--- Скачивание свежих версий ПО ---"
for pkg in "${PACKAGES[@]}"; do
    echo "-> Обработка: $pkg"
    
    # Сначала проверяем, существует ли вообще такой пакет на серверах.
    if dnf info "$pkg" >/dev/null 2>&1; then
        # Качаем пакет и все его зависимости. 
        # Если в папке уже лежит свежая версия, DNF сам напишет "Already downloaded" и скипнет.
        dnf download --resolve --alldeps "$pkg" --destdir="$APPS_DIR" --arch=x86_64,noarch
    else
        # Пакет не найден в сети. Либо опечатка, либо это ультра-закрытый софт.
        echo "   [!] '$pkg' нет в публичной сети. Положите .rpm в папку вручную."
    fi
done

# 4. Зачистка следов
rm -f /etc/yum.repos.d/temp-sync.repo

echo "--- Очистка устаревших пакетов ---"
# Обновляем локальный индекс с учетом только что скачанных файлов
createrepo_c -v --update --compress-type=zstd --general-compress-type=zstd "$APPS_DIR" >/dev/null 2>&1

# Удаляем старые версии программ (например, если скачали Яндекс 24.2, а в папке лежал 24.1).
# Перенаправляем stderr в /dev/null, чтобы скрыть специфичные ворнинги VirtualBox.
repomanage --old "$APPS_DIR" 2>/dev/null | xargs -r rm -f

# 5. Финальная сборка индекса
echo "--- Сборка финального локального индекса apps ---"
createrepo_c -v --update "$APPS_DIR" >/dev/null 2>&1

echo "--- Формирование отчета о версиях ---"
# Читаем метаданные из rpm файлов и сохраняем в versions.txt
rpm -qp --queryformat "%-25{NAME} | Версия: %{VERSION}\n" "$APPS_DIR"/*.rpm > "$APPS_DIR/versions.txt" 2>/dev/null

echo "✅ ПО успешно синхронизировано!"
exit 0