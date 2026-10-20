#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
work="$(mktemp -d)"
unzip -qo "$root/GOOD.zip" -d "$work"
mcs -sdk:4.5 -target:winexe -optimize+ -out:"$root/combined/AutoFish.exe" \
  -r:System.Windows.Forms.dll -r:System.Drawing.dll \
  -resource:"$work/GOOD/E_spaudiklis.exe",fish.exe \
  -resource:"$work/GOOD/Online/Auto_fish_online.exe",online.exe \
  -resource:"$work/GOOD/Raktai/Auto_fish_raktai.exe",keys.exe \
  "$root/combined/Launcher.cs"
rm -rf "$work"
file "$root/combined/AutoFish.exe"
ls -lh "$root/combined/AutoFish.exe"
