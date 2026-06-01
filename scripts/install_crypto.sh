#!/bin/bash
CRYPTO_DIR="$1"
if [ ! -d "$CRYPTO_DIR" ]; then exit 1; fi
cd "$CRYPTO_DIR" || exit 1

# 1. Установка системных зависимостей для токенов
dnf install pcsc-tools -y > /dev/null 2>&1

# 2. Установка библиотеки Рутокен PKCS#11 (если скачана)
RUTOKEN_RPM=$(ls librtpkcs11ecp*.rpm 2>/dev/null | head -n 1)
if [ -n "$RUTOKEN_RPM" ]; then
    dnf localinstall "$RUTOKEN_RPM" -y > /dev/null 2>&1
fi

# 3. Распаковка и установка КриптоПро
ARCHIVE=$(ls linux-amd64*.tgz 2>/dev/null | head -n 1)
if [ -z "$ARCHIVE" ]; then exit 1; fi

tar -xf "$ARCHIVE" || exit 1
cd linux-amd64 || exit 1

# Тихая установка ядра КриптоПро
./install.sh > /dev/null 2>&1 || exit 1

# Установка GUI (cptools) и драйверов
dnf install cprocsp-rdr-gui*.rpm ifd-rutokens cprocsp-rdr-jacarta*.rpm -y > /dev/null 2>&1

exit 0