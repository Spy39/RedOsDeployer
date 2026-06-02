#!/bin/bash
# Универсальный установщик драйверов принтера

echo "[i] Установка пакетов драйверов из папки $(pwd)..."
dnf install ./*.rpm -y >/dev/null 2>&1
echo "[i] Перезапуск службы печати CUPS..."
systemctl restart cups
exit 0