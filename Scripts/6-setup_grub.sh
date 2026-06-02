#!/bin/bash
# ==============================================================================
# Настройка загрузчика GRUB
# ==============================================================================

MODE="$1"
VALUE="$2"

if [ "$MODE" == "uefi" ]; then
    # Режим UEFI: установка по названию
    EFI_GRUB_PATH=$(find /boot/efi/EFI -name "grub.cfg" | grep -i "red" | head -n 1)
    grub2-set-default "$VALUE"
    grub2-mkconfig -o "$EFI_GRUB_PATH" > /dev/null 2>&1

elif [ "$MODE" == "legacy" ]; then
    # Режим Legacy BIOS: установка по индексу и ОТКЛЮЧЕНИЕ МЕРЦАНИЯ
    
    # Комментируем строки, вызывающие мерцание (по вашей инструкции)
    sed -i 's/^GRUB_GFXMODE/#GRUB_GFXMODE/g' /etc/default/grub
    sed -i 's/^GRUB_GFXPAYLOAD_LINUX/#GRUB_GFXPAYLOAD_LINUX/g' /etc/default/grub
    sed -i 's/^GRUB_THEME/#GRUB_THEME/g' /etc/default/grub
    sed -i 's/^GRUB_FONT/#GRUB_FONT/g' /etc/default/grub
    
    # Принудительно переводим GRUB в текстовый режим
    if ! grep -q "^GRUB_TERMINAL_OUTPUT=\"console\"" /etc/default/grub; then
        echo "GRUB_TERMINAL_OUTPUT=\"console\"" >> /etc/default/grub
    fi

    # Устанавливаем дефолтную ОС по цифре
    grub2-set-default "$VALUE"
    grub2-mkconfig -o /boot/grub2/grub.cfg > /dev/null 2>&1
else
    echo "Неверный аргумент."
    exit 1
fi

exit 0