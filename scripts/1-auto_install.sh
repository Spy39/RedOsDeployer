#!/bin/bash
# ==============================================================================
# Скрипт офлайн-обновления РЕД ОС с USB-накопителя
# Автор: Spy
# GitHub: https://github.com/Spy39
# ==============================================================================

REPO_DIR="$1"

if [ -z "$REPO_DIR" ]; then 
    echo "Использование: sudo bash 1-auto_install.sh /путь/к/repo"
    exit 1 
fi

# ЗАЩИТА: Гарантированный возврат репозиториев при любом исходе
trap 'rm -f /etc/yum.repos.d/usb-base.repo /etc/yum.repos.d/usb-updates.repo; dnf config-manager --set-enabled \* > /dev/null 2>&1; echo "[i] Системные репозитории восстановлены."' EXIT

echo "--- Автоопределение редакции ОС ---"
if grep -qiE "cert|серт" /etc/red-release /etc/os-release 2>/dev/null; then
    OS_TYPE="cert"
    echo "[i] Обнаружена Сертифицированная ОС (8.0c)."
else
    OS_TYPE="std"
    echo "[i] Обнаружена Стандартная/Образовательная ОС (8.0)."
fi

TARGET_REPO_DIR="$REPO_DIR/$OS_TYPE"

# Жесткая проверка: скачаны ли базы именно для этой ОС?
if [ ! -d "$TARGET_REPO_DIR/base/repodata" ] || [ ! -d "$TARGET_REPO_DIR/updates/repodata" ]; then
    echo "[!] ОШИБКА: Базы обновлений для редакции [$OS_TYPE] не найдены на носителе!"
    echo "    Сначала выполните пункт 9 (Синхронизация репозитория) на компьютере с такой же ОС."
    exit 1
fi

echo "--- Подключение локальных баз ---"
dnf config-manager --set-disabled \* > /dev/null 2>&1

tee /etc/yum.repos.d/usb-base.repo >/dev/null <<EOF
[usb-base]
name=USB Base
baseurl=file://$TARGET_REPO_DIR/base
enabled=1
gpgcheck=0
EOF

tee /etc/yum.repos.d/usb-updates.repo >/dev/null <<EOF
[usb-updates]
name=USB Updates
baseurl=file://$TARGET_REPO_DIR/updates
enabled=1
gpgcheck=0
EOF

echo "--- Запуск полного обновления системы ---"
dnf update -y --disablerepo="*" --enablerepo="usb-base,usb-updates"

echo "✅ Система успешно обновлена с локального носителя!"
exit 0