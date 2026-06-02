#!/bin/bash
# ==============================================================================
# Скрипт интеллектуальной установки и ОБНОВЛЕНИЯ целевого ПО (Гибридный режим)
# ==============================================================================

APPS_DIR="$1"
OS_TYPE="$2"
shift 2

read -r -a ORIGINAL_PACKAGES <<< "$@"
PACKAGES=("${ORIGINAL_PACKAGES[@]}")

trap 'rm -f /etc/yum.repos.d/usb-apps.repo; dnf config-manager --set-enabled \* > /dev/null 2>&1; echo -e "\n[i] Системные репозитории восстановлены."' EXIT

echo "--- Остановка фоновых служб (снятие блокировки) ---"
systemctl stop packagekit >/dev/null 2>&1 || true

echo "--- Анализ конфигурации установки ---"
INSTALL_R7=false
for pkg in "${PACKAGES[@]}"; do
    if [[ "$pkg" == *"r7-office"* ]]; then INSTALL_R7=true; break; fi
done

if [ "$INSTALL_R7" = true ]; then
    if ! rpm -q r7-office >/dev/null 2>&1; then
        echo "[!] Запрошен Р7-Офис. Выполняется удаление LibreOffice..."
        dnf remove libreoffice* -y > /dev/null 2>&1
    fi
    dnf install r7-release -y >/dev/null 2>&1
fi

if grep -qiE "cert|серт" /etc/red-release /etc/os-release 2>/dev/null; then
    OS_TYPE="cert"
fi

if [ "$OS_TYPE" == "cert" ]; then
    echo "[i] Исключаем 'redoswelcome' (встроен в сертифицированную версию)."
    FILTERED_PACKAGES=()
    for pkg in "${PACKAGES[@]}"; do
        if [[ "$pkg" != *"redoswelcome"* ]]; then FILTERED_PACKAGES+=("$pkg"); fi
    done
    PACKAGES=("${FILTERED_PACKAGES[@]}")
fi

# ==========================================================================
# ЭТАП 1: УМНАЯ ОФЛАЙН УСТАНОВКА И ОБНОВЛЕНИЕ
# ==========================================================================
echo "--- ЭТАП 1: Синхронизация версий с локального носителя ---"
dnf config-manager --set-disabled \* > /dev/null 2>&1

tee /etc/yum.repos.d/usb-apps.repo >/dev/null <<EOF
[usb-apps]
name=USB Apps
baseurl=file://$APPS_DIR
enabled=1
gpgcheck=0
EOF

if [ -d "$APPS_DIR/repodata" ]; then
    # dnf install автоматически обновляет программы, если на флешке версия новее
    dnf install "${PACKAGES[@]}" -y --disablerepo="*" --enablerepo="usb-apps" --allowerasing --skip-broken
else
    echo "[!] Локальная база (repodata) не найдена на носителе. Офлайн-этап пропущен."
fi

# ==========================================================================
# ЭТАП 2: ОНЛАЙН ДОКАЧИВАНИЕ (Если флешка устарела)
# ==========================================================================
rm -f /etc/yum.repos.d/usb-apps.repo
dnf config-manager --set-enabled \* > /dev/null 2>&1

echo "--- ЭТАП 2: Онлайн-проверка пакетов ---"
if ping -c 1 8.8.8.8 >/dev/null 2>&1; then
    dnf install "${PACKAGES[@]}" -y --allowerasing --skip-broken || true
else
    echo "[!] Сеть недоступна. Онлайн-этап пропущен."
fi

echo -e "\n================================================="
echo -e "      ОТЧЕТ ПО ЦЕЛЕВЫМ ПРОГРАММАМ"
echo -e "================================================="

LOG_FILE="$APPS_DIR/install_report.txt"
echo "=== Отчет об установке от $(date) ===" > "$LOG_FILE"

for pkg in "${ORIGINAL_PACKAGES[@]}"; do
    INSTALLED_VERSION=$(rpm -q "$pkg" 2>/dev/null | head -n 1)
    if [ -n "$INSTALLED_VERSION" ] && [[ ! "$INSTALLED_VERSION" == *"is not installed"* ]]; then
        echo -e "* $pkg : \e[32m[УСТАНОВЛЕНО / ОБНОВЛЕНО]\e[0m -> $INSTALLED_VERSION"
        echo "[УСТАНОВЛЕНО] $pkg -> $INSTALLED_VERSION" >> "$LOG_FILE"
    else
        echo -e "* $pkg : \e[31m[ОШИБКА / НЕ УСТАНОВЛЕНО]\e[0m"
        echo "[ОШИБКА] $pkg не установлен" >> "$LOG_FILE"
    fi
done

echo "✅ Работа скрипта успешно завершена!"
exit 0