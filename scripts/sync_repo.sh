#!/bin/bash
REPO_DIR="$1"
if [ -z "$REPO_DIR" ] || [ ! -d "$REPO_DIR" ]; then exit 1; fi

dnf install createrepo_c dnf-utils -y > /dev/null 2>&1

# Скачиваем пакеты
dnf reposync -p "$REPO_DIR" --repo base --download-metadata --newest-only || exit 1
dnf reposync -p "$REPO_DIR" --repo updates --download-metadata --newest-only || exit 1

# Собираем метаданные строго внутри подпапок!
if [ -d "$REPO_DIR/base" ]; then
    createrepo_c -v --compress-type=zstd --general-compress-type=zstd "$REPO_DIR/base" || exit 1
fi

if [ -d "$REPO_DIR/updates" ]; then
    createrepo_c -v --compress-type=zstd --general-compress-type=zstd "$REPO_DIR/updates" || exit 1
fi

exit 0