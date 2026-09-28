# Native Downloads

Die Action **Release game** liefert neben dem Browser-ZIP drei native Archive samt gemeinsamer `SHA256SUMS.txt` aus:

| Plattform | Download | Start |
| --- | --- | --- |
| Linux x86_64 | `ClashOfCities-vX.Y.Z-Linux.tar.gz` | Entpacken, `ClashOfCities.x86_64` starten |
| Windows x86_64 | `ClashOfCities-vX.Y.Z-Windows.zip` | Vollständig entpacken, `ClashOfCities.exe` starten |
| macOS Intel + Apple Silicon | `ClashOfCities-vX.Y.Z-macOS.tar.gz` | Entpacken, `ClashOfCities.app` öffnen |

Alle Datenordner und Bibliotheken im Archiv werden benötigt. Für Spieler ist keine Unity-Installation erforderlich. Der macOS-Build ist universal, aber nicht mit einem Apple-Developer-Zertifikat signiert oder notarisiert; Gatekeeper kann den Start blockieren. Eine Prüfung auf echtem Windows und macOS steht noch aus.

## Neue Builds vorbereiten

Benötigt wird Unity 2022.3.62f3 mit Linux-, Windows-Mono- und Mac-Mono-Build-Support. Auf diesem Linux-Rechner sind die offiziellen Module installiert. Bei einer anderen Editor-Installation `UNITY_EDITOR` auf den Editorpfad setzen.

```bash
./scripts/build-native.sh all
python3 scripts/native-release.py prepare
```

Alternativ einzelne Builds mit `linux`, `windows` oder `macos` erzeugen. Vor `prepare` müssen alle drei Builds zum aktuellen Projektstand neu gebaut sein. Der macOS-Build wird als Universal-Mono-Player erstellt. Die Archive behalten die ausführbaren Unix-Dateirechte; Debug-Sicherungsordner werden nicht ausgeliefert.

Quellcode, gegebenenfalls von Unity geänderte `ProjectSettings`, und `native/` gemeinsam committen. `native/manifest.json` enthält SHA256-Prüfsummen der Archive sowie einen Fingerabdruck von `Assets`, `Packages` und `ProjectSettings`. Der Release-Workflow bricht bei abweichenden Quellen oder beschädigten Archiven vor dem Anlegen eines Releases ab. Die Browser-Dateien in `web/` wie bisher separat aktualisieren.

GitHub Actions prüft und verteilt die vorbereiteten Builds. Es findet keine Unity-Kompilierung auf GitHub statt; Unity-Zugangsdaten oder CI-Lizenzen sind daher nicht nötig. Native Archive werden nur als Release-Downloads und Actions-Artefakte hochgeladen, nicht auf GitHub Pages.

## Prüfung

Alle drei Plattform-Builds durchlaufen 96 Simulations-/Replay-Paare und den Import aller 15 Audioclips. Archivstruktur, Prüfsummen und ausführbare Unix-Dateirechte werden lokal geprüft. `dry_run` in der Release-Action prüft denselben Paketexport ohne Veröffentlichung.

Aktueller Build: Linux sowie Windows unter Wine bestanden jeweils den Test mit 240 gerenderten Kampfbildern und anschließendem Ragdoll-Übergang. Wine ersetzt keine Prüfung auf einem Windows-Rechner. Die macOS-Binärdatei enthält nachweislich x86_64- und arm64-Code.
