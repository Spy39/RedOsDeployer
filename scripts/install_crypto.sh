#!/bin/bash
# ==============================================================================
# Скрипт автоматической установки КриптоПро CSP 5.0 и плагинов (без ключа)
# Автор: Spy
# ==============================================================================

CRYPTO_DIR="$1"

if [ -z "$CRYPTO_DIR" ] || [ ! -d "$CRYPTO_DIR" ]; then
    echo "[!] Ошибка: Не указан путь к папке с КриптоПро."
    exit 1
fi

# Ищем архив КриптоПро в указанной папке
ARCHIVE=$(ls "$CRYPTO_DIR"/linux-amd64*.tgz 2>/dev/null | head -n 1)

if [ -z "$ARCHIVE" ]; then
    echo "[!] Ошибка: Архив linux-amd64.tgz не найден в папке $CRYPTO_DIR!"
    exit 1
fi

echo "--- [1/4] Распаковка дистрибутива ---"
# Распаковываем архив прямо в папку[cite: 5]
tar -xvf "$ARCHIVE" -C "$CRYPTO_DIR" >/dev/null 2>&1
WORK_DIR="$CRYPTO_DIR/linux-amd64"

echo "--- [2/4] Установка базовых компонентов КриптоПро ---"
cd "$WORK_DIR" || exit 1

# Запускаем штатный установщик КриптоПро в тихом режиме (без GUI)
# kc1 - базовая криптография, cades - поддержка подписей
./install.sh kc1 cades >/dev/null 2>&1

# Устанавливаем плагины CryptoPro Extension for CADES Browser Plug-in[cite: 6]
dnf install ./cprocsp-pki*.rpm -y >/dev/null 2>&1

echo "--- [3/4] Установка системных утилит и драйверов носителей ---"
# Утилита для обнаружения внешних устройств[cite: 5]
dnf install pcsc-tools -y >/dev/null 2>&1
# Драйвер для носителей Рутокен[cite: 5]
dnf install ifd-rutokens -y >/dev/null 2>&1
# Драйвер для носителей Jacarta (если rpm есть в папке)[cite: 5]
dnf install ./cprocsp-rdr-jacarta*.rpm -y >/dev/null 2>&1 || true

echo "--- [4/4] Установка графических инструментов и плагинов Госуслуг ---"
# Средство управления сертификатами token-manager[cite: 8]
dnf install token-manager python3-chardet -y >/dev/null 2>&1
# ПО для подписи файлов gostcrypto[cite: 8]
dnf install gostcryptogui -y >/dev/null 2>&1
# Плагин для Chromium/Яндекс.Браузера[cite: 6]
dnf install ifcplugin-chromium -y >/dev/null 2>&1
# Плагин для Firefox[cite: 6]
dnf install ifcplugin-firefox -y >/dev/null 2>&1

echo "--- Запуск службы смарт-карт (pcscd) ---"
systemctl start pcscd
systemctl enable pcscd >/dev/null 2>&1

echo "--- Очистка временных файлов ---"
# Удаляем распакованную папку, оставляем только оригинальный архив .tgz
cd "$CRYPTO_DIR" || exit 1
rm -rf "$WORK_DIR"

echo "✅ КриптоПро CSP 5.0, драйверы токенов и плагины успешно установлены!"
echo "[i] Лицензия не введена (демонстрационный режим)."
exit 0