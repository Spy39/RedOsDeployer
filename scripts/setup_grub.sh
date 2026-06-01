#!/bin/bash

# Проверяем наличие папки EFI
if [ -d /sys/firmware/efi ]; then
    # РЕЖИМ UEFI
    EFI_GRUB_PATH=$(find /boot/efi/EFI -name "grub.cfg" | grep -i "red" | head -n 1)
    WIN_NAME=$(awk -F\' '/menuentry / {print $2}' "$EFI_GRUB_PATH" | grep -i "Windows" | head -n 1)
    
    if [ -n "$WIN_NAME" ]; then
        grub2-set-default "$WIN_NAME"
        grub2-mkconfig -o "$EFI_GRUB_PATH" > /dev/null 2>&1
        exit 0
    else
        # Если Windows не найдена, отдаем ошибку в C#
        exit 1
    fi
else
    # РЕЖИМ LEGACY BIOS
    sed -i 's/^GRUB_GFXMODE/#GRUB_GFXMODE/g' /etc/default/grub
    sed -i 's/^GRUB_THEME/#GRUB_THEME/g' /etc/default/grub
    sed -i 's/^GRUB_BACKGROUND/#GRUB_BACKGROUND/g' /etc/default/grub
    sed -i 's/^GRUB_TERMINAL_OUTPUT/#GRUB_TERMINAL_OUTPUT/g' /etc/default/grub
    
    if ! grep -q "^GRUB_TERMINAL_OUTPUT=\"console\"" /etc/default/grub; then
        echo "GRUB_TERMINAL_OUTPUT=\"console\"" >> /etc/default/grub
    fi

    grub2-set-default 2
    grub2-mkconfig -o /boot/grub2/grub.cfg > /dev/null 2>&1
    exit 0
fi