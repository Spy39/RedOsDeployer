#!/bin/bash

# Получаем путь к папке repo от C#-программы
REPO_DIR="$1"

# Если путь пустой или папки нет - возвращаем ошибку
if [ -z "$REPO_DIR" ] || [ ! -d "$REPO_DIR" ]; then exit 1; fi

# 1. Отключаем сетевые репозитории
dnf config-manager --set-disabled \* > /dev/null 2>&1

# 2. Создаем временный конфиг для флешки
tee /etc/yum.repos.d/usb.repo >/dev/null <<EOF
[usb-base]
name=USB Base
baseurl=file://$REPO_DIR/base
enabled=1
gpgcheck=0

[usb-updates]
name=USB Updates
baseurl=file://$REPO_DIR/updates
enabled=1
gpgcheck=0
EOF

# 3. Обновляем систему
dnf update -y || exit 1

# 4. Удаляем временный конфиг и возвращаем сетевые
rm -f /etc/yum.repos.d/usb.repo
dnf config-manager --set-enabled \* > /dev/null 2>&1

exit 0