#!/bin/bash
# ==============================================================================
# Скрипт предрелизной очистки системы
# Автор: Spy
# GitHub: https://github.com/Spy39
# ==============================================================================

echo "--- Запуск очистки следов установки ---"

echo "[i] Очистка временных директорий (/tmp, /var/tmp)..."
rm -rf /tmp/* /var/tmp/* 2>/dev/null

echo "[i] Очистка кэша пакетного менеджера DNF..."
dnf clean all >/dev/null 2>&1

echo "[i] Очистка истории команд терминала (bash_history)..."
cat /dev/null > ~/.bash_history
if [ -f /home/*/.bash_history ]; then
    cat /dev/null > /home/*/.bash_history 2>/dev/null
fi
history -c

echo "✅ Система очищена и готова к передаче пользователю!"
exit 0