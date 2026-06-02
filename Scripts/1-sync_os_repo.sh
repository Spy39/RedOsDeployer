#!/bin/bash
# ==============================================================================
# Скрипт синхронизации локального репозитория РЕД ОС
# Автор: Spy
# GitHub: https://github.com/Spy39
# ==============================================================================

REPO_DIR="$1"
OS_TYPE="$2"

if [ -z "$REPO_DIR" ] || [ -z "$OS_TYPE" ]; then 
    echo "Использование: sudo bash 1-sync_os_repo.sh /путь/к/repo <std|cert>"
    exit 1 
fi

if ! ping -c 1 8.8.8.8 >/dev/null 2>&1; then
    echo "[!] Ошибка: Нет подключения к интернету."
    exit 1
fi

echo "--- Подготовка к синхронизации репозиториев ОС ---"
dnf install createrepo_c dnf-utils yum-utils -y >/dev/null 2>&1

# Создаем изолированную папку для конкретной версии ОС
TARGET_REPO_DIR="$REPO_DIR/$OS_TYPE"
mkdir -p "$TARGET_REPO_DIR/base"
mkdir -p "$TARGET_REPO_DIR/updates"

echo "--- [1/2] Синхронизация базы ОС (base) в папку $OS_TYPE ---"
# Добавлены фильтры: --arch=x86_64,noarch (только 64-битные и универсальные пакеты)
dnf reposync --repoid=base -p "$TARGET_REPO_DIR" --newest-only --delete --download-metadata --arch=x86_64,noarch

echo "--- [2/2] Синхронизация обновлений ОС (updates) в папку $OS_TYPE ---"
dnf reposync --repoid=updates -p "$TARGET_REPO_DIR" --newest-only --delete --download-metadata --arch=x86_64,noarch

echo "--- Финальная сборка индексов ---"
createrepo_c -v --update "$TARGET_REPO_DIR/base" >/dev/null 2>&1
createrepo_c -v --update "$TARGET_REPO_DIR/updates" >/dev/null 2>&1

echo "✅ Локальный репозиторий для версии [$OS_TYPE] успешно синхронизирован!"
exit 0