# Clash of Cities

Offline-3D-Kampfspiel für Unity mit direkter Steuerung und automatischem Modus: Deutsche Städte treten mit historischen Avataren gegeneinander an. Die Klimawerte sind frei gewählte Spielwerte. Die Kampfwerte stammen ausschließlich aus den Stadtdaten; Avatare bestimmen Bewegungsprofil, Reichweite und Moveset.

## Bewegung und Körperphysik

Die stilisierten Figuren besitzen einen gegliederten Oberkörper, gedämpfte Gewichtsverlagerung und weiche Arm-/Kopfbewegungen. Die Beine nutzen Fußkontakt und Zwei-Gelenk-IK für Knie und Schritte in Laufrichtung. Bei K. o. übernehmen zehn Rigidbody-Körper mit begrenzten Gelenken und Boden-/Hinderniskollision die Fallbewegung; der Abschluss bleibt 1,25 Sekunden sichtbar.

Die Körperphysik steuert die Darstellung. Kampfpositionen, Treffer und Replay-Ergebnisse bleiben von der deterministischen Simulation bestimmt. Die Modelle bleiben prozedurale, stilisierte Figuren.

## Schnell einsteigen

Neue Spiele beginnen mit KI-Stufe Leicht. Stick/WASD bewegt die Figur; **A/Leertaste halten** greift wiederholt an und nähert sich ohne Bewegungseingabe einem nahen Gegner kurz an. Eigene Bewegung hat Vorrang; die Hilfe verfolgt keine entfernten Gegner und umgeht keine Deckung. **B/Shift** weicht aus, **X/Y bzw. Q/E** aktivieren Skills, **R halten und loslassen** lädt und feuert die Ulti.

Die Karten zeigen Nahkampf, Fernkampf, Vorstoß, Fläche oder Selbstwirkung. Der erste K. o. beendet das Duell. Für die nächste Aktion nach dem Ergebnis muss Angriff zuerst losgelassen werden; Controller-Fokus startet auf Revanche. Replays tragen eine sichtbare Wiederholungskennzeichnung.

## Kampf-HUD und KI-Stufen

In der Charakterauswahl lässt sich die KI auf **Leicht, Normal oder Schwer** stellen, auch per Controller. Leicht reagiert langsamer und setzt weniger Kombinationen ein; Schwer reagiert schneller und weicht häufiger aus. Lebenspunkte und Schaden bleiben gleich. Im PvP ist die Auswahl deaktiviert.

Das kompakte HUD zeigt Gesundheit und Energie oben sowie fünf kleine Aktionskarten pro Seite unten. Controller-Tasten entsprechen A/B/X/Y/R und +/−; bei Tastatursteuerung erscheinen die zugehörigen Tasten. Abklingzeiten und Ulti-Ladung werden als Fortschrittsbalken angezeigt. Details bleiben über Hilfe erreichbar.

Unity-Build, Simulation, Controller-Menüs und Darstellung sind geprüft. Details stehen in `docs/VERIFICATION.md`.

## Im Browser spielen

Zusätzlicher WebGL-Build unter `Builds/Web`. Starte `./scripts/play-browser.sh` und öffne `http://localhost:8765`. Mit Tastatur, unterstützten Controllern und Touch-Steuerung für Spieler 1; auf Handys im Querformat. [Anleitung und Hosting](docs/BROWSER.md).

## Direkt spielen (dieser Rechner)

Im Anwendungsmenü **Clash of Cities** öffnen oder:

```bash
./scripts/play.sh
```

Der geprüfte Linux-Spieler liegt in `Builds/Linux/ClashOfCities.x86_64`. Unity Hub 3.21.3 und Unity Editor 2022.3.62f3 sind unter `~/Applications/UnityHub` beziehungsweise `~/Unity/Hub/Editor/2022.3.62f3` installiert. Unity Personal ist aktiviert. Der gebaute Spieler braucht zum Spielen weder Hub noch Editor.

## Start in Unity

1. Projektordner mit **Unity 2022.3 LTS** öffnen (3D Built-in Render Pipeline).
2. Szene `Assets/Scenes/Battle.unity` öffnen und Play drücken.
3. „Kampf vorbereiten“ wählen, beide Städte und Avatare sowie Seed und Klimaereignis auswählen und den Kampf starten.
4. Modus wählen: Spieler gegen CPU, zwei Spieler lokal oder CPU gegen CPU.
5. Im Ergebnis zeigt Replay denselben Kampf mit aufgezeichneten Spielereingaben; Revanche startet einen neuen Seed.

Der Editor und die Plattform-Buildmodule müssen zuvor installiert sein. Das Spiel selbst benötigt weder Netzwerkzugriff noch Backend, Accounts, externe Datenbank oder Dienste. Die Grafik verwendet acht gegliederte, stilisierte historische Figuren mit Kostümen, Accessoires, Lauf-/Ausweichanimationen und thematischen Kampfeffekten. Es müssen keine Art-Assets heruntergeladen werden.


## Steuerung und Spielmodi

**Spieler gegen CPU** steuert Stadt A. **Spieler gegen Spieler (lokal)** verwendet eine gemeinsame Tastatur. **CPU gegen CPU** lässt beide Seiten automatisch kämpfen. Basisangriffe unterscheiden sich je Figur: Nahkämpfer schlagen im kurzen Bogen zu, Fernkämpfer schießen geradlinige Projektile. Skills halten ihre angekündigte Richtung beziehungsweise Zielposition fest; seitliche Bewegung, Ausweichen und Deckung können Treffer verhindern. Fähigkeiten brauchen Energie und einen abgelaufenen Cooldown; Ultimates werden nach acht Sekunden freigeschaltet.

| Aktion | Spieler 1 | Spieler 2 |
| --- | --- | --- |
| Bewegen | WASD | Pfeiltasten |
| Basisangriff (halten) | Leertaste | J |
| Fähigkeit 1 / 2 | Q / E | K / L |
| Hauptskill aufladen / abfeuern | R halten / loslassen | I halten / loslassen |
| Ausweichen | Linke Umschalttaste | Rechte Umschalttaste |
| Pause / Fortsetzen | Escape | Escape |
| Skillhilfe (pausiert den Kampf) | F1 | F1 |

Manuelle Kämpfe laufen mit 1× Tempo. Bei Fokusverlust pausiert das Spiel; Escape setzt fort. CPU-Kämpfe und Replays bieten 1×/2×/4×. Ausweichen kostet 18 Energie, hat 3 s Cooldown und weicht während 0,28 s direkten Treffern aus. Schaden über Zeit und Umwelthitze bleiben wirksam. Die CPU umkreist, nähert sich, zieht sich kurz nach Treffern zurück und reagiert auf sichtbare heranfliegende Projektile; alle Entscheidungen bleiben regelbasiert.

Spielereingaben werden pro festem Simulationstakt in `MatchResult.inputs` gespeichert. Dadurch benötigen manuelle Replays neben Seed/Konfiguration auch die Aufzeichnung. **Balance-Version balance-9 ist mit früheren Replays nicht kompatibel**, weil die Simulation jetzt mit 60 Hz läuft und Eingabepuffer, kurze Angriffserholung sowie Bewegung und Ausweichen während der Skill-Vorbereitung ergänzt wurden. Alte Dateien werden nicht stillschweigend mit neuen Regeln abgespielt.

## Skills und Gegenmaßnahmen

- **Nahkampfbogen:** kurze Reichweite und sichtbares Ausholen. Seitlich oder aus der Reichweite ausweichen; Deckung blockiert den Schlag.
- **Vorstoß:** schließt Distanz und schlägt am Ende zu. Hindernisse stoppen den Weg; verfehlte Vorstöße kosten Energie und Cooldown.
- **Fernschuss:** unterschiedliche Fluggeschwindigkeiten, Größen und Effekte. Projektile verfolgen nicht nach und treffen zuerst Deckung, falls diese im Weg steht.
- **Bodenfeld:** feste Zielposition mit Warnphase und sichtbarem Radius; danach zeitlich begrenzte Schadensimpulse und gegebenenfalls Verlangsamung. Die Warnfläche verlassen, statt im Feld stehenzubleiben.
- **Selbstschutz:** Heilung, Schild oder Verstärkung eröffnen eine defensive Option, kosten aber Zeit und Energie.

Beethoven, Goethe und Gutenberg kämpfen mit Nahkampf-Basisangriffen und Vorstößen; die anderen fünf Figuren bringen unterschiedliche Fernkampf- und Kontrollwerkzeuge mit. F1 zeigt beide Movesets mit Wirkung, Reichweite und Gegenmaßnahmen; die Simulation pausiert beim Lesen.

R beziehungsweise I halten lädt den Hauptskill, Loslassen löst ihn aus. Die Vorschau entspricht seiner Angriffsform. Eine längere Ladung verstärkt die Wirkung; Ausweichen bricht ab.

## Trefferkombinationen und unterschiedliche Ultis

Kontrollskills können bei einem Treffer **Stun** (Aktionen unterbrechen) oder **Root** (Bewegung stoppen, Angreifen bleibt möglich) auslösen. Viele geben dem Angreifer gleichzeitig **Fokus**, damit eine aufgeladene Ulti ins Zeitfenster passt. Beispiel Ulm: Q treffen, R halten und rechtzeitig loslassen. Fokus verkürzt Vorbereitung und Ladung; eine kurze Teilladung ist ebenfalls möglich. Andere Treffer geben mehr Schaden oder Tempo beziehungsweise machen das Ziel verwundbarer. Fehlschüsse, Deckung und ausgewichene Treffer geben keinen Trefferbuff.

Nach Stun/Root folgt ein gemeinsamer Kontrollschutz gegen erneute Betäubung und Festhalten. Schadens- und Tempobuffs sind begrenzt. Der Status samt Restzeit steht im HUD; F1 erklärt Skills und über „Status & Karte“ die Regeln.

Die acht Ultis unterscheiden sich: Droste setzt einen Blättersturm, Beethoven einen breiten Nahkampfschlag, Bach einen verlangsamenden Fugenkreis, Einstein einen schweren Raumzeitschuss, Goethe einen Vorstoß und Humboldt ein Wurzelfeld ein. Gutenberg erhält Schild und Schadensverstärkung; Brahms verstärkt Schaden und Tempo. Alle lassen sich aufladen.

## Deckung und Kampfzone

Zusätzlich entstehen zwei positive und zwei negative, symmetrisch platzierte Terrainfelder. Der Seed bestimmt Regeneration mit Energie, Fokus oder Tempo; normales Wetter und Starkregen erzeugen Schlamm, Hitze und Dürre erzeugen Schadensfelder. Die Wirkung gilt innerhalb des sichtbaren Kreises für beide Kämpfer. Symbole und vier kurze beziehungsweise acht lange Randmarken unterscheiden Nutzen und Gefahr.

Vier symmetrisch verteilte Hindernisse blockieren Figuren, Vorstöße, Rückstöße und Geschosse. Die CPU sucht Wege um die Deckung. Höhe und Grundfläche der sichtbaren Säulen entsprechen den Simulationsdaten.

Rückwärtsbewegung ist langsamer als Vorwärtsbewegung; seitliche Schritte und begrenzte Ausweichmanöver bleiben möglich. Vorstöße geben Nahkämpfern ein Werkzeug zum Aufholen. Nach 45 Sekunden beginnt die sichere Kampfzone zu schrumpfen; nach 120 Sekunden beträgt ihr Radius 6,5 Meter. Außerhalb entsteht ansteigender Schaden. Die Grenze und ihr Zustand sind im Spiel sichtbar. Diese Regeln gelten für Spieler und CPU.

Acht Städte stehen zur Auswahl: Münster, Bonn, Leipzig, Ulm, Weimar, Mainz, Hamburg und Berlin. **Zufall** würfelt eine Seite; **Alles zufällig** wählt zwei unterschiedliche Städte, einen neuen Seed und zufälliges Wetter. **Neue Map** ändert den Seed. Wetter und Seed erzeugen die Arena reproduzierbar; Klima-Modifikatoren bestimmen weiterhin die Kampfwerte.

## Headless ausführen

Mit .NET SDK 9 oder neuer, ohne NuGet-Pakete:

```bash
dotnet run --project Headless -- test
dotnet run --project Headless -- simulate 1000 muenster bonn 726491 random
dotnet run --project Headless -- simulate 1 ulm leipzig 42 heatwave > result.json
dotnet run --project Headless -- replay result.json
```

`simulate` akzeptiert Anzahl, Stadt A, Stadt B, Startseed und Klimaereignis. Die Seeds steigen pro Match um eins. Ein einzelner Lauf gibt außerdem das vollständige Ergebnis als JSON aus. `replay` nimmt ein gespeichertes MatchResult und vergleicht das komplette erneut berechnete Ergebnis; Exitcode 1 meldet Abweichungen oder ungültige Eingaben. Demo-Verteilungen dienen dem Gameplay-Balancing und erlauben keine Aussagen über die tatsächliche Klimaresilienz einer Stadt.

## Aufbau

- `Assets/ClashOfCities/Core`: Unity-unabhängige Datenmodelle, Klimaberechnung, feste Simulationstakte, deterministischer Zufall und generische Fähigkeiten.
- `Assets/ClashOfCities/Resources/GameData.json`: lokale Städte, historische Avatare, Fähigkeiten, Ereignisse und zentrale Balancekonfiguration.
- `Assets/ClashOfCities/Presentation`: Unity-Arena, Spielfiguren, Kamera, Animation/VFX und UI.
- `Headless`: Konsolenprogramm und automatisierte Kernlogik-Prüfungen, verwendet dieselben C#-Dateien und dieselbe JSON-Datei.
- `docs/DATA.md`: Kennzeichnung der Demo-Daten und historische Belege.
- `ARCHITECTURE.md`: gemeinsame Schnittstellen und Architekturentscheidungen.

## Daten ändern

Die JSON-Datei wird lokal geladen. IDs müssen eindeutig sein, Avatare zur gewählten Stadt gehören und referenzierte Fähigkeiten vorhanden sein. `sealingScore` bedeutet **Versiegelungsgrad**: 100 ist vollständig versiegelt und negativ. Auch `heatRiskScore` wirkt negativ. Die übrigen Scores sind positive Qualitätswerte von 0 bis 100.

Um Replay-Verwechslungen zu vermeiden, bei jeder Änderung von Daten, Kampfregeln oder Balancewerten `balance.version` erhöhen. Eine gespeicherte MatchConfig enthält die Version und alle Auswahlparameter, aber keinen vollständigen Datensnapshot. Historische Replays benötigen deshalb die damalige JSON-Datei und denselben Simulationscode.

Die Simulation läuft unabhängig von Render-FPS in festen Takten. Seed-basierte Zufallsgeneratoren steuern ausschließlich die Simulation und die Ereignisauswahl. Renderbewegung und Effekte ändern keine Kampfergebnisse. Die C#-Simulation verwendet double-Arithmetik; exakte plattformübergreifende Bitidentität zwischen unterschiedlichen Unity-/Mono-/IL2CPP-Runtimes wird nicht garantiert.


## Player bauen

Im Editor **Clash of Cities → Validate Local Data** ausführen, dann **Clash of Cities → Build Linux Player**. Das Linux-Buildmodul muss installiert sein. Alternativ für automatisierte Builds:

```bash
/path/to/Unity -batchmode -quit -projectPath "$PWD" -executeMethod ClashOfCities.Editor.BuildTools.BuildLinux -logFile /tmp/clash-unity-build.log
```

Für andere Plattformen die enthaltene Battle-Szene in den Unity Build Settings verwenden. Letzte Ergebnisse liegen in `Application.persistentDataPath/last-match.json` (unter Linux normalerweise `~/.config/unity3d/ClashOfCities/Clash of Cities/last-match.json`).

## Verifikation und verbleibende Prüfung

Unity 2022.3.62f3 wurde anschließend auf diesem Rechner installiert. **Unity-Import, Linux-Build und ein vollständiger gerenderter Probelauf einschließlich Replay sind jetzt erfolgreich geprüft.** Screenshots und Details stehen in `docs/VERIFICATION.md`.

## Oberflächen und Grafik

Stein, Holz, Stoff, Leder, Blattwerk und Haar verwenden nahtlos wiederholbare Farb- und Normaltexturen aus `SurfaceLibrary`. Sechs Paare mit jeweils 256 × 256 Pixeln werden einmal erzeugt, mit Mipmaps versehen und zwischen Matches geteilt. Die CPU-Kopien der Pixel werden danach freigegeben. Arena-Materialien teilen sich eine begrenzte Farbpalette und unterstützen GPU-Instancing; animierte Figuren behalten eigene Materialien für Trefferblitze und Buff-Leuchten. Metall, Haut und Wasser besitzen unterschiedliche Glanzeinstellungen. Vierfach-Kantenglättung, anisotrope Filterung und ein schattenfreies Aufhelllicht ergänzen die Darstellung.

## Mit Switch-Controller spielen

Controller per USB anschließen oder in den Linux-Bluetooth-Einstellungen koppeln. In der **Stadtwahl oben auf der jeweiligen Spielerseite „Tastatur“ anklicken** und den erkannten Controller wählen. Alternativ im Menü am Controller A drücken. Im lokalen Duell sind Tastatur + Controller und zwei getrennte Controller möglich. Bei fehlendem Controller kann auf Tastatur zurückgeschaltet werden.

**Linker Stick / Steuerkreuz:** bewegen · **A:** angreifen · **X/Y:** Skills · **R halten/loslassen:** Ulti · **B:** ausweichen · **+/−:** Pause/Hilfe. Nintendo-Beschriftungen werden berücksichtigt. Beim Abziehen pausiert der Kampf. Die Linux-Version verwendet die systemseitige SDL2-Bibliothek. Weitere Details und Auswahlsteuerung: [Steuerung](docs/CONTROLS-CONTRACT.md).

Controller-Backend separat testen: `dotnet run --project Headless -- controllers` (erzeugt zwei virtuelle SDL-Controller; verändert keine physischen Geräte).

### Controller-Menüführung

Linker Stick / Steuerkreuz bewegt den goldenen Fokus. **A bestätigt, B geht zurück**. Das gilt für Hauptmenü, Charakter-/Geräteauswahl, Pause, Hilfe und Ergebnis. Der rechte Stick scrollt Beschreibungen. Arena-Codes lassen sich mit einem Bildschirm-Ziffernfeld eingeben. Beethovens Abschwächung ist in [der Balance-Stichprobe](docs/BALANCE-beethoven.md) dokumentiert.

## Online spielen

[Clash of Cities auf GitHub Pages](https://playskyhd.github.io/ClashOfCities/) · [Build und Veröffentlichung](docs/BROWSER.md)

Native Linux-, Windows- und macOS-Downloads werden durch die [Release-Action](https://github.com/PlaySkyHD/ClashOfCities/actions/workflows/release.yml) mitgeliefert. [Build-Anleitung und Plattformhinweise](docs/NATIVE-RELEASES.md).
