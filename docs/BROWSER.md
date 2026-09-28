# Browser-Version

Öffentlich spielen: https://playskyhd.github.io/ClashOfCities/

Zusätzlicher WebGL-Build neben dem Linux-Spieler. Ziel sind Desktop-Browser mit WebGL2 und WebAssembly. Touch-Steuerung für Spieler 1 wird auf Geräten mit grobem Zeiger automatisch aktiviert; alternativ oben „Touch an“ wählen. Für Handys ist Querformat vorgesehen. Größere Charakterauswahl, linker Bewegungsstick, A Angriff, B Ausweichen, X/Y Skills und R zum Halten/Loslassen der Ulti. Mehrere Finger gleichzeitig werden unterstützt. Pause und Hilfe blenden die Steuerelemente aus und löschen gehaltene Eingaben. Der zweite lokale Spieler benötigt Tastatur oder einen eigenen Controller. Auf echten Smartphones wurde diese Version noch nicht geprüft; Unity 2022 WebGL bietet keine allgemeine Garantie für mobile Browser.

## Lokal spielen

Im Projektordner `./scripts/play-browser.sh` starten und `http://localhost:8765` öffnen. Der Server muss laufen, während das Spiel geöffnet ist. Er stellt ausschließlich `Builds/Web` bereit und ist nur auf diesem Rechner erreichbar. Eine HTML-Datei per Doppelklick (`file://`) reicht für Unity-WebGL nicht aus.

Tastatur und lokale Zweispielermodi funktionieren wie im Desktop-Spiel. Für Controller zuerst im Browser eine Controllertaste drücken und anschließend das Gerät in der Charakterauswahl wählen. Der Browser muss eine standardisierte Gamepad-Zuordnung liefern; Nintendo-Controller werden anhand der Gerätekennung auf A/B/X/Y umgeordnet. Browser und Betriebssystem müssen das Gerät unterstützen. Die Tastenbelegung ist automatisiert mit simulierten API-Daten geprüft; das ersetzt keine Prüfung echter physischer Tasten im Browser.

Das letzte Ergebnis wird über Unity PlayerPrefs im Browserspeicher gespeichert. Dieser Speicher ist an Browserprofil und Website-Adresse gebunden; privater Modus, Speicherbeschränkungen oder Löschen der Websitedaten können Speicherung verhindern oder Ergebnisse entfernen. Das Linux-Ergebnis bleibt separat.

## Neu bauen

`./scripts/build-browser.sh` oder in Unity **Clash of Cities → Build Browser Player**. Erforderlich: Unity 2022.3.62f3 inklusive WebGL Build Support. Das passende offizielle Modul ist auf diesem Rechner installiert. Ausgabe: `Builds/Web`.

## Öffentlich bereitstellen

Hosting: **GitHub Pages**, Repository [PlaySkyHD/ClashOfCities](https://github.com/PlaySkyHD/ClashOfCities).

1. Browser-Build mit `./scripts/build-browser.sh` erzeugen. Das Skript exportiert anschließend automatisch die benötigten Dateien nach `web/`.
2. Nach einem Build über das Unity-Menü stattdessen `python3 scripts/version-browser-assets.py Builds/Web` und `python3 scripts/prepare-pages.py` ausführen.
3. Quellcode und `web/` auf `main` committen und pushen. Der Workflow `.github/workflows/pages.yml` veröffentlicht ausschließlich `web/`.

GitHub Actions veröffentlicht den bereits gebauten WebGL-Player; Unity wird dort nicht neu kompiliert. Dadurch sind keine Unity-Lizenz oder Deployment-Tokens in GitHub nötig. GitHub Pages muss in den Repository-Einstellungen auf **GitHub Actions** stehen. Der Workflow lässt sich auch manuell starten.

Die komprimierten Unity-Dateien nutzen Decompression Fallback und benötigen keine besonderen Content-Encoding-Regeln. Alle URLs sind relativ und funktionieren im Unterverzeichnis `/ClashOfCities/`. `.env`, lokale Werkzeuge und Unity-Zwischendateien werden nicht versioniert. Azure wird nicht mehr verwendet.

In der ursprünglichen lokalen Arbeitsumgebung ist `.git` ein schreibgeschützter Platzhalter. Der veröffentlichbare Git-Checkout liegt deshalb unter `.publish/github`. Änderungen aus dem Arbeitsordner müssen vor einem Push in diesen Checkout übernommen werden. Bei einem normalen neuen Clone arbeitet man direkt im geklonten Repository.


Die Browser-Version ergänzt lokale CPU- und Zweispielerkämpfe. Sie fügt kein Netzwerk-Multiplayer hinzu.

## Quellen

- [Offizielles Unity-Release mit Linux-WebGL-Modul](https://unity.com/releases/editor/whats-new/2022.3.62f3)
- [Unity: Eingaben in WebGL](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-input.html)

## Musik und Sounds

Eigene prozedural komponierte Chiptune-Loops für Menü und Kampf, dazu Effekte für Treffer, Skill-Arten, Ausweichen, Kontrolle, Ulti-Aufladen, K. o. und Ergebnis. Lautstärke für Musik und Sounds im Haupt- und Pausenmenü getrennt einstellen; 0 % schaltet den jeweiligen Kanal aus. Einstellungen werden im lokalen Browserprofil gespeichert. Browser starten Audio nach der ersten Interaktion im Spiel. Bei Pause wird Musik leiser und der Ladeton gestoppt.

Quellgenerator: `scripts/generate-audio.py`. Keine fremden Musikaufnahmen oder Samples verwendet.

Auf Linux benötigt der Unity-WebGL-Audioimport FFmpeg. Der Build nutzt einen Konverter auf `PATH` oder in `.tools/bin/ffmpeg`; hier liegt eine lokale Version aus dem Paket `imageio-ffmpeg` (0.6.0). Das Werkzeug wird nicht mit dem Spiel veröffentlicht. Der Build bricht bei fehlenden/ungültigen Audioclips ab.
