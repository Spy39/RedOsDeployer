#!/bin/bash
APPS_DIR="$1"
shift
PACKAGES=("$@")

# 1. Удаляем LibreOffice
dnf remove libreoffice* -y > /dev/null 2>&1

# 2. Добавляем репозиторий MAX
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

# 3. Сначала ставим локальные пакеты с флешки (если они там есть)
if [ -d "$APPS_DIR" ]; then
    shopt -s nullglob
    rpms=("$APPS_DIR"/*.rpm)
    shopt -u nullglob
    if [ ${#rpms[@]} -gt 0 ]; then
        echo "Обнаружены локальные пакеты. Запуск офлайн-установки..."
        dnf install "${rpms[@]}" --allowerasing --skip-broken -y
    fi
fi

# 4. Доустанавливаем целевое ПО онлайн (из config.json)
if [ ${#PACKAGES[@]} -gt 0 ]; then
    echo "Проверка и онлайн-установка целевого ПО (Яндекс, Р7, MAX)..."
    dnf install "${PACKAGES[@]}" --allowerasing --skip-broken -y || true
fi

exit 0