# Steuerung und Darstellungsvertrag (demo-5)

`MatchMode` enthält `CpuVsCpu`, `PlayerVsCpu` und `PlayerVsPlayer`. Die UI startet standardmäßig mit Spieler gegen CPU. Lokales PvP nutzt dieselbe Tastatur.

`FighterInput`: `moveX/moveZ`, `basicAttack`, `skill1`, `skill2`, `ultimate`, `dodge`. Bewegung wird normalisiert. Basisangriff und Ultimate werden mit `GetKey` gehalten; Skills und Ausweichen werden mit `GetKeyDown` bis zum nächsten Simulationstakt gepuffert. Ultimate loslassen feuert nach ausreichender Aufladezeit; Ausweichen bricht die Ladung ab.

| Aktion | Spieler 1 | Spieler 2 |
|---|---|---|
| Bewegung | WASD | Pfeiltasten |
| Basisangriff halten | Leertaste | J |
| Skills | Q/E | K/L |
| Hauptskill halten / loslassen | R | I |
| Ausweichen | Linke Shift | Rechte Shift |
| Pause | Escape | Escape |
| Skillhilfe | F1 | F1 |

Manuelle Kämpfe laufen bei 1×. Fokusverlust pausiert sie. Während Pause oder UI-Interaktion werden keine Live-Befehle verarbeitet. Eingaben pro Tick werden in `MatchResult.inputs` aufgezeichnet und bei Replays erneut abgespielt. Frühere Balance-Versionen werden abgewiesen.

`FighterView.Initialize/CaptureTick/Render/OnBattleEvent` stellt die Simulation dar. `IsCharging`, `Charge` und `AimX/AimZ` steuern Leuchten, Ladepose und Zielmarkierung. `ProjectileView.Bind/Render/OnBattleEvent` zeichnet die aktiven `BattleSimulation.Projectiles`. Angriffseffekte entstehen am Abschuss oder am tatsächlichen Einschlag, nicht als nachträgliche Verbindung zwischen beiden Figuren. Die Darstellung verursacht keinen Schaden.

`ArenaView.Build(halfSize, seed, climateId)` verwendet einen separaten Zufallsgenerator. Wetter und Seed erzeugen dieselbe Arena; `MapName` wird im HUD angezeigt. Menü-Zufall wird vor Matchbeginn in konkrete IDs und einen Seed aufgelöst.

## Lieferformen und Deckung

`AbilityDefinition.delivery` ist `Melee`, `Dash`, `Projectile`, `Zone` oder `Self`. `description` erklärt das Gegenmittel; UI und F1-Hilfe lesen die Daten direkt. `FighterView.Configure(data, simulation)` folgt nach `Initialize`, um Reichweiten und Zielvorschauen exakt zu zeichnen.

`ArenaLayout.Create(seed, halfSize)` liefert die Hindernisse für Simulation und Arena. `BattleEffectsView.Bind(simulation, data)`, `Render(alpha)` und `OnBattleEvent` zeichnen `Areas`, Vorwarnungen und `SafeRadius`. Rückwärtsgeschwindigkeit und Ringzeitpunkte sind Balanceparameter. Die CPU umgeht Hindernisse und priorisiert das Verlassen gefährlicher Flächen.

`GameBootstrapSmoke.cs` enthält den optionalen Grafiktest als separaten Teil derselben Bootstrap-Klasse. Er verwendet gesteuerte Szenarien für alle fünf Lieferformen und beeinflusst den normalen Spielstart nicht.

## Trefferstatus und Terrain

`onHitSelfEffect/onHitTargetEffect` mit Dauer und Stärke wirken erst bei einem gültigen feindlichen Treffer. Stun unterbricht Cast/Charge und Aktionen; Root blockiert Bewegung und Dodge. `ControlImmunityRemaining` sperrt beide Kontrolltypen bis zwei Sekunden nach dem ursprünglichen Kontrollfenster. `ChargeElapsed` sichert die minimale echte Haltezeit trotz beschleunigtem Fokus.

`ArenaLayout.CreateZones(seed, halfSize, climateId)` liefert `BattleSimulation.TerrainZones`. `BattleEffectsView` zeichnet dieselben Zentren/Radien und Wirkungssymbole. `FighterView` zeichnet Status und Immunität; das HUD nennt Status und Restzeit. Die F1-Hilfe besitzt einen zweiten Bereich „Status & Karte“.

## Controller im Linux-Spiel

In der Stadtwahl besitzt jeder menschliche Spieler einen Geräteknopf: Tastatur oder ein konkret erkannter Controller. Zwei Spieler können dieselbe Tastatur mit getrennten Tasten, Tastatur plus Controller oder zwei verschiedene Controller verwenden. Ein Controller kann nicht beiden Spielern zugeordnet werden. Geräte werden anhand SDL-Instanz-IDs zugeordnet; beim Wiederverbinden erfolgt die Zuordnung nur bei einem eindeutigen Namen/Seriennummer-Treffer. Abziehen pausiert, leert gepufferte Eingaben und verhindert Fortsetzen mit fehlendem Gerät. Wiederverbinden setzt das Match nicht automatisch fort.

`LinuxGamepads` nutzt die auf diesem Rechner installierte SDL2-Laufzeit (`libSDL2-2.0.so.0`, 2.30.0), einschließlich Switch-HIDAPI-Mapping und Joy-Con-Paar-Hinweis. Tasten werden nach ihren Beschriftungen gemeldet: [SDL-Dokumentation](https://wiki.libsdl.org/SDL2/SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS). Die tatsächliche Erkennung hängt von USB/Bluetooth-Verbindung und Gerätezugriffsrechten des Systems ab. Der Backend ist für diesen Linux-Build; Windows/macOS benötigen eine passende native Anbindung.

| Aktion | Switch-Controller |
|---|---|
| Bewegung | Linker Stick / Steuerkreuz |
| Basisangriff halten | A |
| Skills 1 / 2 | X / Y |
| Ulti halten / loslassen | R (rechte Schultertaste) |
| Ausweichen | B |
| Pause / Fortsetzen | + |
| Skillhilfe | − |
| Hilfe zwischen Skills / Karte umschalten | A auf dem markierten Wechselknopf |

In allen Menüs bewegen linker Stick oder Steuerkreuz den goldenen Auswahlrahmen. A aktiviert die markierte Schaltfläche, B geht zurück beziehungsweise verwirft Eingaben. Deaktivierte Optionen werden übersprungen. Gehaltene Richtungen wiederholen nach 0.36 s mit 0.13 s Abstand; ein neutraler zweiter Controller unterbricht diesen Rhythmus nicht. Rechter Stick scrollt die Beschreibung der fokussierten Spielerseite. Städte, Figuren, Geräte, Modus, Wetter und Zufall sind über ihre Schaltflächen erreichbar. Der Arena-Code besitzt eine Controller-Zahlenmaske mit Löschen, Vorzeichen, Übernehmen und Abbrechen. Das Pausemenü bietet Fortsetzen, Hilfe und Rückkehr zur Auswahl; die Ergebnisaktionen sind ebenfalls fokussierbar. Maus und Tastatur bleiben verfügbar. Die Stick-Deadzone beträgt 18 %, Diagonalen sind normalisiert. Skills/Dodge werden pro Frame erfasst und bis zum nächsten Simulationstick gepuffert; Angriff und Ulti bleiben Halteaktionen. Fokusverlust verhindert Controller-Aktionen ebenso wie Tastatureingaben. Gerätewahl ist eine lokale Steuerungseinstellung, Replay-Dateien speichern weiterhin die tatsächlich verarbeiteten Kampfkommandos.

## Einsteigerhilfen (balance-8)

Gehaltene Basisangriffe nähern den menschlichen Spieler ohne manuelle Bewegungsrichtung einem nahen sichtbaren Gegner an (höchstens Angriffsreichweite + 3 m). Bewegungseingaben haben immer Vorrang. Die Simulation zeichnet die ursprüngliche Eingabe auf und reproduziert die Hilfe im Replay. Keine Änderung an Schadenswerten.

Gerätewechsel über die Geräteauswahl ist während eines Kampfes gesperrt. Verbindungsabbruch pausiert weiterhin und behält die Controller-Zuordnung. Ergebnisaktionen bleiben mindestens 0,8 Sekunden gesperrt und erfordern das Loslassen von Angriff/Maus. Standardfokus bei Controllerbedienung ist Revanche statt Replay.

## Reaktionsverhalten (balance-9)

Die Simulation verarbeitet 60 feste Schritte pro Sekunde. Skill-Taps werden maximal 200 ms vorgemerkt; nur die neueste Aktion bleibt vorgemerkt. Nach Ablauf verfällt sie. Ein Grundangriff behält seinen Schadensrhythmus, blockiert andere Skills aber nur 160 ms. Während einer Skill-Vorbereitung kann der Spieler mit 70 % Tempo laufen. Ausweichen bricht die Vorbereitung ab, ohne Kosten oder Cooldown zu erstatten. Stun und Root bleiben wirksam. Die Ulti wird weiterhin durch Halten und Loslassen gesteuert.

## Audio

Musik und Sounds besitzen getrennte Minus-/Plus-Schaltflächen im Haupt- und Pausenmenü. Sie funktionieren über Maus/Touch und die bestehende Controller-Navigation. 0 % bedeutet stumm; Lautstärken bleiben gespeichert. Im Browser aktiviert die erste Interaktion Audio. Maximal acht Effektstimmen, zwei Ladetöne und zwei überblendete Musikspuren; Lautstärke überlagerter Effektstimmen wird abgesenkt.

## Aufgeräumte Duellauswahl

Die Auswahl zeigt Spielmodus, zwei Stadtkarten mit Eingabegerät, Charakter und Kampftyp sowie den Startknopf. „Skills & Stadtinfo“ öffnet die bisherigen Werte und Beschreibungen. „So spielt man“ zeigt die Steuerung und Einstiegstipps. „Arena“ enthält Wetter, neuen Zufalls-Seed und das Zahlenfeld für einen eigenen Arena-Code. B beziehungsweise Escape schließen zuerst das Zahlenfeld, dann das offene Fenster und erst danach die Auswahl. Nur das oberste Fenster nimmt Controller-Navigation an. Der rechte Stick scrollt die jeweils geöffnete Stadtinfo.
