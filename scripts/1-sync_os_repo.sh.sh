#!/bin/bash

REPO_DIR="$1"
OS_TYPE="$2"

if [ -z "$REPO_DIR" ]; then 
    echo "Использование: sudo bash 1-sync_os_repo.sh /путь/к/repo"
    exit 1 
fi

# Если запускаем руками и не передали версию - скрипт спросит сам
if [ -z "$OS_TYPE" ]; then
    echo "Какую редакцию РЕД ОС синхронизируем?"
    echo "1) Стандартная (8.0)"
    echo "2) Сертифицированная (8.0c)"
    read -p "Выберите (1/2): " choice
    if [ "$choice" == "1" ]; then OS_TYPE="std"; else OS_TYPE="cert"; fi
fi

if [ "$OS_TYPE" == "cert" ]; then
    BASE_REPO="redos-cert"
    UPDATES_REPO="updates-cert"
else
    BASE_REPO="redos"
    UPDATES_REPO="updates"
fi

echo "--- Синхронизация репозитория: $BASE_REPO ---"
dnf reposync -p "$REPO_DIR" --repo "$BASE_REPO" --download-metadata --newest-only --delete || exit 1

echo "--- Синхронизация репозитория: $UPDATES_REPO ---"
dnf reposync -p "$REPO_DIR" --repo "$UPDATES_REPO" --download-metadata --newest-only --delete || exit 1

echo "✅ Синхронизация ОС успешно завершена. Индексы скачаны автоматически!"
exit 0