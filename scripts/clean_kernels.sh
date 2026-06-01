#!/bin/bash
# Находим старые ядра, оставляя 2 самых новых
OLD_KERNELS=$(dnf repoquery --installonly --latest-limit=-2 -q)

if [ -n "$OLD_KERNELS" ]; then
    dnf remove $OLD_KERNELS -y > /dev/null 2>&1 || exit 1
fi
exit 0