#!/bin/bash
# ==============================================================================
# Скрипт интеллектуальной установки целевого ПО (Гибридный режим)
# Автор: Spy
# GitHub: https://github.com/Spy39
#
# Описание: 
# Универсальный установщик с предварительным отсевом уже установленных программ.
# Формирует красивый отчет и пишет лог с версиями на флешку.
# ==============================================================================

APPS_DIR="$1"
OS_TYPE="$2"
shift 2

# Сохраняем изначальный список пакетов для финального отчета
read -r -a ORIGINAL_PACKAGES <<< "$@"
PACKAGES=("${ORIGINAL_PACKAGES[@]}")

if [ -z "$APPS_DIR" ] || [ -z "$OS_TYPE" ] || [ ${#PACKAGES[@]} -eq 0 ]; then 
    echo "Использование: sudo bash 3-install_apps.sh /путь/к/apps <std|cert> пакет1 пакет2 ..."
    exit 1 
fi

# ==============================================================================
# ЗАЩИТА СИСТЕМЫ (Rollback)
# ==============================================================================
trap 'rm -f /etc/yum.repos.d/usb-apps.repo; dnf config-manager --set-enabled \* > /dev/null 2>&1; echo -e "\n[i] Системные репозитории восстановлены."' EXIT

echo "--- Анализ конфигурации установки ---"

# ==============================================================================
# ФИЛЬТРАЦИЯ 1: R7-Office vs LibreOffice
# ==============================================================================
INSTALL_R7=false
for pkg in "${PACKAGES[@]}"; do
    if [[ "$pkg" == *"r7-office"* ]]; then INSTALL_R7=true; break; fi
done

if [ "$INSTALL_R7" = true ]; then
    # Проверяем, установлен ли УЖЕ Р7. Если да - нет смысла удалять LibreOffice снова.
    if ! rpm -q r7-office >/dev/null 2>&1; then
        echo "[!] Запрошен Р7-Офис. Выполняется удаление LibreOffice..."
        dnf remove libreoffice* -y > /dev/null 2>&1
    fi
    dnf install r7-release -y >/dev/null 2>&1
else
    echo "[i] Р7-Офис не запрошен. Встроенный LibreOffice сохранен."
fi

# ==============================================================================
# ФИЛЬТРАЦИЯ 2: Редакция ОС (Автоопределение и защита)
# ==============================================================================
# Жесткая проверка системы: читаем системные файлы на наличие маркера сертификации
if grep -qiE "cert|серт" /etc/red-release /etc/os-release 2>/dev/null; then
    echo "[i] Система аппаратно определена как Сертифицированная (8.0c)."
    if [ "$OS_TYPE" != "cert" ]; then
        echo "[!] ВНИМАНИЕ: Передан неверный аргумент! Принудительно включаю режим 'cert'."
        OS_TYPE="cert"
    fi
else
    echo "[i] Система аппаратно определена как Стандартная/Образовательная (8.0)."
fi

if [ "$OS_TYPE" == "cert" ]; then
    echo "[i] Исключаем 'redoswelcome' (уже встроен в сертифицированную версию)."
    FILTERED_PACKAGES=()
    for pkg in "${PACKAGES[@]}"; do
        if [[ "$pkg" != *"redoswelcome"* ]]; then
            FILTERED_PACKAGES+=("$pkg")
        fi
    done
    PACKAGES=("${FILTERED_PACKAGES[@]}")
fi

# ==============================================================================
# ФИЛЬТРАЦИЯ 3: Подготовка ключей MAX
# ==============================================================================
INSTALL_MAX=false
for pkg in "${PACKAGES[@]}"; do
    if [[ "$pkg" == *"max"* ]]; then INSTALL_MAX=true; break; fi
done

if [ "$INSTALL_MAX" = true ]; then
    tee /etc/yum.repos.d/max.repo >/dev/null <<'EOF'
[max]
name=MAX Desktop
baseurl=https://download.max.ru/linux/rpm/el/9/x86_64
enabled=1
gpgcheck=1
repo_gpgcheck=1
gpgkey=https://download.max.ru/linux/rpm/public.asc
sslverify=1
metadata_expire=300
EOF
    rpm --import https://download.max.ru/linux/rpm/public.asc >/dev/null 2>&1 || true
fi

# ==============================================================================
# ФИЛЬТРАЦИЯ 4: Отсев уже установленных программ (Экономия времени)
# ==============================================================================
echo "--- Проверка системы на наличие программ ---"
TO_INSTALL=()
for pkg in "${PACKAGES[@]}"; do
    if rpm -q "$pkg" >/dev/null 2>&1; then
        echo "[i] Пакет '$pkg' уже установлен. Пропускаем."
    else
        TO_INSTALL+=("$pkg")
    fi
done
PACKAGES=("${TO_INSTALL[@]}")

# Если все пакеты уже стоят, пропускаем DNF
if [ ${#PACKAGES[@]} -gt 0 ]; then

    # ==========================================================================
    # ЭТАП 1: ОФЛАЙН УСТАНОВКА
    # ==========================================================================
    echo "--- ЭТАП 1: Офлайн-установка с локального носителя ---"
    dnf config-manager --set-disabled \* > /dev/null 2>&1

    tee /etc/yum.repos.d/usb-apps.repo >/dev/null <<EOF
[usb-apps]
name=USB Apps
baseurl=file://$APPS_DIR
enabled=1
gpgcheck=0
EOF

    if [ -d "$APPS_DIR/repodata" ]; then
        dnf install "${PACKAGES[@]}" -y --disablerepo="*" --enablerepo="usb-apps" --allowerasing --skip-broken
    else
        echo "[!] Локальная база (repodata) не найдена на носителе. Офлайн-этап пропущен."
    fi

    # ==========================================================================
    # ЭТАП 2: ОНЛАЙН ДОКАЧИВАНИЕ
    # ==========================================================================
    rm -f /etc/yum.repos.d/usb-apps.repo
    dnf config-manager --set-enabled \* > /dev/null 2>&1

    echo "--- ЭТАП 2: Онлайн-докачивание ---"
    if ping -c 1 8.8.8.8 >/dev/null 2>&1; then
        echo "[i] Сеть доступна. Проверка недостающих пакетов и зависимостей..."
        dnf install "${PACKAGES[@]}" -y --allowerasing --skip-broken || true
    else
        echo "[!] Сеть недоступна. Онлайн-этап пропущен."
    fi
else
    echo "[i] Все необходимые пакеты уже установлены. Установка пропущена."
fi

# ... после установки пакетов ...
echo -e "\n================================================="
echo -e "      ОТЧЕТ ПО ЦЕЛЕВЫМ ПРОГРАММАМ"
echo -e "================================================="

# Путь к файлу отчета прямо в папке apps
REPORT_FILE="$APPS_DIR/install_report.txt"
echo "=== Отчет об установке от $(date) ===" > "$REPORT_FILE"

for pkg in "${TARGET_PACKAGES[@]}"; do
    if rpm -q "$pkg" > /dev/null 2>&1; then
        echo -e "* $pkg : ${C_GREEN}[УСТАНОВЛЕНО]${C_RESET}"
        echo "[УСТАНОВЛЕНО] $pkg" >> "$REPORT_FILE"
    else
        echo -e "* $pkg : ${C_RED}[ОШИБКА / НЕ УСТАНОВЛЕНО]${C_RESET}"
        echo "[ОШИБКА] $pkg" >> "$REPORT_FILE"
    fi
done

echo "✅ ПО успешно синхронизировано!"
exit 0