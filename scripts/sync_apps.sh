#!/bin/bash
APPS_DIR="$1"
shift
PACKAGES=("$@")

if [ -z "$APPS_DIR" ] || [ ${#PACKAGES[@]} -eq 0 ]; then exit 1; fi

dnf install createrepo_c dnf-utils -y > /dev/null 2>&1

for pkg in "${PACKAGES[@]}"; do
    dnf download --resolve --alldeps "$pkg" --destdir="$APPS_DIR" --arch=x86_64,noarch || true
done

createrepo_c -v --compress-type=zstd --general-compress-type=zstd "$APPS_DIR" || exit 1
exit 0