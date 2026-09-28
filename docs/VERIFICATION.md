# Prüfstand

## Bewegungsüberarbeitung (GPT-6 Luna, orchestriertes Review)

GPT-6 Luna implementierte den gemeinsamen Schrittrhythmus, Bodenkontakt, Stop-Übergänge, angepasste Beinsegmente und Körper-/Statusübergänge. Der Hauptagent prüfte Code und gerenderte Testphasen und korrigierte abschließend den Start mitten im Schrittzyklus sowie vorgezogene Schritte bei drohender Überstreckung nach Richtungswechseln. Gameplay-Positionen und Kampfregeln wurden nicht verändert.

Finaler nativer Test `-clash-locomotion-test`: 990 gerenderte Frames über Stand, Vorwärts-/Rückwärtslauf, Stopp, Seitwärtslauf, Kurven, Laden, bewegtes Ausweichen, Erholung, Stun und erneute Erholung bestanden. Gameplay-Root exakt, alle animierten Gelenke verbunden. Schuhmittelpunkte 0,074–0,215 m über Boden (inklusive Schwungphase). Maximale Fußzielabweichung 0,0027 m, maximale Verschiebung während durchgehender Standphase 0,0006 m. Maximaler lokaler Kopfschritt 0,115 m pro Frame. Diese Grenzprüfungen ersetzen keine subjektive Bewegungsbewertung; die gerenderten Phasenbilder wurden zusätzlich gesichtet. Der Test deaktiviert ausschließlich für sich VSync und begrenzt auf 60 FPS; normale Spieleinstellungen bleiben erhalten.

240 gerenderte Kampfframes mit Kopf-/Gelenkverbindung und anschließendem K.-o.-Ragdoll-Übergang bestanden. Keine neuen laufenden Allokationen im Fußsolver. Native und WebGL-Unity-Buildprüfung: jeweils 96 Simulations-/Replay-Paare und alle 15 Audioimporte bestanden. Browserkampf im lokalen finalen Build gestartet und nach 13 Sekunden mit sichtbaren Skills, Status und Bewegung ohne Konsolenfehler geprüft. Azure-Produktionsdeployment erfolgreich; öffentliches HTML mit den versionierten Build-URLs stimmt bytegenau mit dem getesteten Build überein. Lokale Version neu gestartet.

Bildzeitstichprobe: 600 Frames bei 1280×763 auf AMD Vega 8, p95 22,76 ms, p99 40,19 ms. Eine einzelne 9.268,67-ms-Unterbrechung verzerrt den Mittelwert (32,68 ms); damit kein belastbarer Nachweis einer FPS-Verbesserung. Die Änderung zielt auf den sichtbaren Bewegungsablauf, nicht auf eine geänderte Simulationstaktrate.

## Reduzierte Duellauswahl

Kompakte Stadtkarten, Spielmodus und Start bleiben sichtbar. Stadtwerte/Skills, Spieltipps sowie Wetter/Seed sind in separate Fenster ausgelagert. Gerenderter nativer Controller-Test `-clash-selection-test` bestanden: Navigation, Schwierigkeitswechsel, verschachtelte Seed-Eingabe inklusive Übernehmen/Abbrechen, Zurück-Reihenfolge, isolierter Fensterfokus, Stadtinfo und Tipps. Screenshots der Auswahl und Detailansicht geprüft. Im Browser Auswahl, Arena-Fenster, Zahlenfeld und Tipps visuell geprüft. Finaler Browserlauf: Escape schließt Zahlenfeld und Arena-Fenster in der richtigen Reihenfolge; keine Konsolenfehler. Native und WebGL-Builds jeweils mit 96 Simulations-/Replay-Paaren bestanden. Web-Dateien erhalten Inhalts-Hashes als URL-Version; Laden mit dem zuvor problematischen Cache erfolgreich. Lokale Version neu gestartet. Azure-Produktionsdeployment erfolgreich.

## Chiptune-Audio

15 eigene WAV-Dateien deterministisch erzeugt (zwei Musikloops, zwölf kurze Cues, ein Ladeloop). PCM auf nichtleere Signale und fehlendes Sample-Clipping geprüft; Musikloop-Grenzen bei Null. Nativer Build mit 96 Simulations-/Replay-Paaren erfolgreich. Gerenderter Audiotest prüft geladene Clips, laufende Musikquelle, Ansteuerung der Cues und stummen Effektkanal; anschließend Kampfwechsel und Pause ohne Laufzeitfehler durchlaufen. Dies prüft Wiedergabezustände und Samples, keine subjektive Hörbewertung über die Lautsprecher des Nutzers.

Eigene begrenzte AudioSource-Pools, Pegelabsenkung bei überlagerten Effekten, getrennte persistente Lautstärken und verzögerte Audioaktivierung im Browser nach Interaktion. Keine Fremdaufnahmen. WebGL-Build mit 96 Simulations-/Replay-Paaren und expliziter Validierung aller 15 importierten Audioclips erfolgreich. Lokaler Browser: Menü, Lautstärkeänderung nach erster Interaktion, Kampfstart und sichtbare Audio-Regler im Pausenmenü geprüft; keine neuen Konsolenfehler. FFmpeg für den WebGL-AAC-Import liegt lokal unter `.tools/bin`; ein fehlender Audioimport bricht künftige Builds ab. Azure-Produktionsdeployment erfolgreich; öffentliche Version mit Lautstärkereglern und erster Audiointeraktion ohne Konsolenfehler geprüft. Lokale Audioversion neu gestartet.

## Kopfverbindung und FPS-Anzeige

Animierte kinematische Ragdoll-Körper verwenden keine Rigidbody-Interpolation mehr; erst beim K. o. wird sie für die physische Bewegung aktiviert. So überschreiben gespeicherte Physikpositionen nicht die animierte Kopf-/Armhierarchie. Gerenderter Test mit 240 Kampfbildern, Bewegung, Angriffen und Ausweichen bestanden; Kopfposition relativ zur Wirbelsäule bleibt stabil. Übergang zur Ragdoll separat bestanden. Sichtprüfung von `avatar-attachment.png` einschließlich FPS-Anzeige.

FPS stehen klein oben rechts in Bildschirmkoordinaten und werden über jeweils 0,5 Sekunden gemittelt. Die lokale Version wurde erfolgreich gebaut (96 Simulations-/Replay-Paare) und neu gestartet. WebGL-Build mit denselben 96 Prüfpaaren erfolgreich; Azure-Deployment abgeschlossen.

## Reaktionsfähigkeit und Bildzeiten (balance-9)

194 Headless-Prüfungen bestanden, darunter acht neue Reaktionsprüfungen: 60-Hz-Schritt, früher Skill-Tap, Verfall des Puffers, Ersetzen statt Aufstauen, Bewegung während Vorbereitung, Dodge-Abbruch ohne Cooldown-Erstattung, Angriff-Skill-Kette und unveränderter Grundangriffsrhythmus. Bestehende zeitabhängige Tests verwenden jetzt Sekunden statt fest angenommener 20-Hz-Tickzahlen. 15 SDL-Prüfungen und Browser-/Touch-Brückentests bestanden. Nativer und WebGL-Build jeweils mit 96 Simulations-/Replay-Paaren erfolgreich. Lokaler Browsertest: vollständige Auswahl, Kampfstart, Bewegung, Dodge und Tastaturpause; keine Konsolenfehler. Azure-Produktionsdeployment erfolgreich; öffentliche Auswahl, aktualisierte Kampfanzeige und Pause ohne Konsolenfehler geprüft.

600 gerenderte Frames eines CPU-Kampfs bei Starkregen, nach 120 Aufwärmframes, ohne parallelen Build: Mittel 16,72 ms (rund 59,8 FPS), p95 21,05 ms, p99 22,68 ms, Maximum 42,94 ms; ein Frame über 33,3 ms. Auflösung 1280 × 763, AMD Radeon Vega 8, 60-Hz-Display. Kein neuer Laufzeitfehler. Kurze lokale Stichprobe, kein Vorher/Nachher-Vergleich oder Nachweis für jedes Gerät.

Optimierungen: statische Arena-Geometrie gebündelt, Effektpartikel wiederverwendet, Primitive-Meshes statt wiederholter Collider-Erstellung, HUD-Stile gecacht, feste Positionspuffer, KI-Wegpunkte bis 200 ms wiederverwendet. Kantenglättung 2× und mittlere Schattenauflösung. Alte Replays benötigen ihre alten Kampfregeln.

## Desktop-WebGL-Menüfix (26.09.2026)

Fehler auf der öffentlichen Desktop-Seite reproduziert: ArgumentNullException beim Beginn des Beschreibungs-Scrollfelds, danach fehlten zweite Stadt und Start-/Umweltbedienung. Scrollbereich durch explizit beschnittenen Inhalt mit Mausrad, Pfeilbuttons und bestehender Controller-Scrollposition ersetzt. Native Engine-Stripping für Web-Builds deaktiviert, nachdem zusätzlich fehlende MonoScript-/SphereCollider-Komponenten im Laufzeitprotokoll sichtbar wurden. Seed-Text aufgehellt.

Produktionsdeployment auf Azure erfolgreich. Finaler Build: 96 Simulations-/Replay-Paare bestanden. JavaScript-Gamepad-/Touchtests bestanden. Im Desktop-Browser vollständige Auswahl, Scrollen, Zufallsauswahl, Seed-Eingabe 123456, Kampfstart und Escape-Pause geprüft. Nach finalem Neuladen und Kampfstart keine neuen Fehler im Browserprotokoll. Frühere Fehlermeldungen im gleichen Tab wurden anhand Zeitstempel und Blob-URL vom finalen Build unterschieden.

## Touch-Steuerung und Azure-Veröffentlichung

Touch-WebGL-Build erfolgreich, 96 Simulations-/Replay-Paare bestanden. JavaScript-Prüfungen für Gamepads und Touch bestanden: gleichzeitige Bewegungs-/Skill-Eingaben, getrenntes Loslassen, kurze Taps, Abbruch, Fokusverlust und Pause. Im Browser bei 844 × 390 geprüft: größere Auswahl, Kampf, Bewegungsstick, Pause und Fortsetzen. Keine Prüfung auf einem echten Smartphone.

Produktionsdeployment über Azure Static Web Apps erfolgreich: https://polite-plant-0c9e5e703.4.azurestaticapps.net . Öffentliche Seite mit geladenem Unity-Spiel und Touch-Auswahl im Browser geprüft. Deployment-Token nur als Prozessumgebungsvariable verwendet; Veröffentlichung auf Builds/Web begrenzt und Artefakte vorab auf Token/.env geprüft.

## Browser-Version (Unity WebGL)

Offizielles WebGL-Modul für Unity 2022.3.62f3 installiert. Browser-Build mit 96 Simulations-/Replay-Paaren erfolgreich; 186 Headless-Prüfungen bestanden. JavaScript-Gamepad-Brücke separat auf Nintendo-/Standard-Zuordnung, Achsen, zwei Geräte und Abziehen geprüft (`node scripts/test-browser-gamepads.cjs`). Echte physische Controller-Eingaben im Browser wurden nicht geprüft.

Im Chromium-basierten In-App-Browser tatsächlich geladen und bedient: Hauptmenü, Charakterauswahl, Spieler gegen CPU, Tastatureingabe/Pause, CPU-Kampf bis zum K. o. (Münster/Bonn, Starkregen, Leicht, Seed 726491, 49,20 s), Speicherung, Neuladen und Öffnen des gespeicherten Replays. Figuren, Physik, Effekte, HUD und P1/CPU-Markierungen sichtbar. Komprimierter Build etwa 5,2 MB; Paket `Builds/ClashOfCities-Browser.zip`. Ursprüngliche lokale Abnahme unter `http://localhost:8765`; öffentliche Bereitstellung siehe oben. Weitere Browser und Mobile nicht abgenommen.

## Spieler-Markierungen

P1/P2/CPU als kompakte, farbige Namensmarkierungen mit Pfeil über lebenden Figuren. Position folgt der projizierten Figurenposition und berücksichtigt HUD-Skalierung und Letterboxing. CPU-gegen-CPU, Spieler-gegen-CPU und lokales PvP im gerenderten Spieler visuell geprüft (`screenshots/markers`). Unity-Build und 96 Replay-Paare bestanden.

## Gelenkbewegung und K.-o.-Physik

Unity-Build und 96 Simulations-/Replay-Paare bestanden. Vollständiger gerenderter Probelauf mit 39 Controller-/Darstellungsprüfungen, darunter Aktivierung der Ragdoll und Prüfung sämtlicher Körper auf endliche Positionen, Bodenhöhe und begrenzte Geschwindigkeit. Laufpose und K. o. visuell geprüft: `screenshots/motion/25-weighted-stride.png`, `screenshots/motion/26-physical-knockout.png`. 59,5 FPS in einer kurzen 90-Frame-Stichprobe; keine garantierte Mindestleistung.

Dargestellte Bewegung: gedämpfte Oberkörperfeder, geglättete Arm-/Kopfgelenke, Fußkontakt mit Zwei-Gelenk-IK, zehn Rigidbody-Körper und begrenzte CharacterJoints für K. o., Boden und vereinfachte Hinderniscollider. Physik betrifft die Darstellung, daher bleibt die Balance-Version balance-8. Ragdoll-Posen sind kein deterministischer Bestandteil des Replay-Ergebnisses. Die Figuren bleiben stilisiert; keine Motion-Capture-Animationen.

## Einfacher Einstieg und eindeutiges Kampfende (balance-8)

186 Core-Prüfungen, 96 Unity-Simulations-/Replay-Paare und 37 Controller-/Menüprüfungen bestanden. Neue Prüfungen: kurze Annäherung bei gehaltenem Angriff, Vorrang manueller Bewegung, keine Fernverfolgung, erster K. o. beendet den Kampf dauerhaft, gehaltenes A überspringt das Ergebnis nicht, Loslassen hält Ergebnis offen, frische Bestätigung startet Revanche und erhält Controller-Zuordnung. Geräteauswahl kann im Kampf keine Bindung ändern. Disconnect/Reconnect weiterhin getestet.

Gerenderte Sichtprüfung von Nah-/Fernkampf-/Selbstwirkungsanzeigen, Steuerungshinweis, Controller-Tasten und K.-o.-Ergebnis mit Revanche-Fokus. 38 PNGs unter `screenshots/gameplay`. Der Testlauf verwendet virtuelle Controller; reales Spielerfeedback und physische Tastendrücke ersetzt er nicht. Eine automatische Umschaltung auf Tastatur im normalen Kampf ließ sich im Codepfad nicht reproduzieren; Testläufe wechseln Geräte absichtlich, der Gerätewähler ist nun zusätzlich im Kampf gesperrt. Test-Ergebnisse werden künftig im jeweiligen Screenshot-Verzeichnis gespeichert.

## Schwierigkeitsstufen und kompaktes HUD (balance-7)

181 Core-Prüfungen bestanden. Neu geprüft: gültige Schwierigkeitswerte, exakte Eingabe-Replays für Leicht/Normal/Schwer, unveränderte CPU-Lebenspunkte, frühere Projektilreaktion auf Schwer und unveränderte Regeln im reinen PvP.

96 Unity-Simulations-/Replay-Paare über alle drei Stufen bestanden. Linux-Spieler erfolgreich gebaut. Vollständiger Grafiktest mit 33 Controller-/Menüprüfungen bestanden, einschließlich Normal → Schwer → Leicht → Normal per A-Taste. Virtuelle SDL-Geräte prüfen Eingaben; reale physische Tastendrücke wurden nicht automatisiert getestet.

Sichtprüfung: kompakte Aktionskarten, HP-/Energiebalken, Controller-Tastensymbole, Ulti-Ladung, Abklingbalken, Statusanzeigen und Pause. 37 PNGs unter `screenshots/compact-hud`. Die vorherige Linux-Version wird unter `Builds/Previous-before-hud-difficulty` gesichert.

## Beethoven und Controller-Menüs (balance-6)

172 Core-Prüfungen und 32 Unity-Simulations-/Replay-Paare bestanden. Der gebaute Linux-Spieler bestand den vollständigen Grafiktest sowie **30 Controller-/Menüprüfungen** mit virtuellen SDL-Geräten: sichtbarer Fokus, Stick und Steuerkreuz, deaktivierte Schaltflächen überspringen, A aktivieren, B zurück, Zifferneingabe/Übernehmen/Abbrechen, Pause/Hilfe und Ergebnis. 37 Aufnahmen unter `screenshots/controller-menu`. Controller-Fokus umfasst die vorhandenen UI-Schaltflächen; physische Tasten wurden weiterhin nicht automatisch gedrückt.

Beethoven: 42 identische CPU-Paarungen vor/nach dem finalen Nerf, Siege 24 → 14. Details und genaue Änderungen in [BALANCE-beethoven.md](BALANCE-beethoven.md). Die Versionskennung verhindert, dass alte Replays mit geänderten Schadenswerten abgespielt werden.

## Controller-Update (26.09.2026)

**15 SDL-Backend-Checks** mit zwei virtuellen Controllern bestanden: getrennte Geräte, normalisierte Sticks/Deadzone, A/B/X/Y/R, Flanken gegenüber Halten, Plus/Minus, D-Pad, Abziehen/Wiederverbinden sowie keine künstliche Tastendruckflanke beim Verbinden. **15 zusätzliche Controller-Checks im gerenderten Unity-Spiel** prüfen Geräteauswahl, Mischbetrieb, doppelte Zuordnung, Moduswechsel, Routing, Skill-Puffer, tatsächliches Ulti-Aufladen/Freigeben, Pause und Wiederverbinden. Der vollständige Grafiktest und 32 Unity-Simulations-/Replay-Paare bestanden. 34 PNGs unter `screenshots/controllers`.

Ein physischer **Nintendo Switch Pro Controller wurde über SDL als unterstützter Controller erkannt**. Tatsächliche physische Stick-/Tastendrücke und erneutes Bluetooth-Pairing wurden nicht automatisiert geprüft; die Eingabeprüfungen nutzen virtuelle SDL-Geräte. Der Support gilt für diesen Linux-Build mit systemseitigem SDL2.

## Material-Update (26.09.2026)

Sechs nahtlose Oberflächentypen mit gemeinsamen 256²-Farb-/Normaltexturen, Mipmaps und anisotroper Filterung. Gemeinsame Arena-Materialien, individuelle Figurenmaterialien, UVs/Tangenten für prozedurale Kleidung und Ringe, passende Metall-/Wasser-/Hautreflexe, 4× MSAA und Aufhelllicht. Normalmap-Packung mit dem lokal installierten Standard-Shader abgeglichen. Unity-Build und 32 Simulations-/Replay-Paare bestanden. Gerenderter Test über alle Klimata, Figuren, Menüs und Skills.

Render-Stichprobe: **60.0 FPS im Mittel über 90 CPU-Kampf-Frames** auf diesem Rechner bei angeforderten 1280 × 800; 463 geladene Materialien und 36 geladene Texture2D-Objekte inklusive Oberfläche und interner Unity-Ressourcen. Eine kurze Stichprobe, keine garantierte Mindestbildrate. 31 Screenshots unter `screenshots/surfaces`.

## Design-Update (26.09.2026)

Menü, Stadtwahl, HUD und Ergebnis verwenden flache Schaltflächen und gerahmte Panels. Die Arena besitzt geschnittenes Pflaster, Kompassintarsien, Säulenverzierungen, Laternen und eine äußere Stadtkulisse. Sichtbare Demo-Texte und interne Versionsnummern wurden entfernt. Kampfregeln und Replay-Version bleiben unverändert. Unity-Build mit 32 Simulations-/Replay-Paaren geprüft; gerenderter Probelauf mit allen Menüs, vier Klimata und Skilldarstellungen erfolgreich. Bilder liegen unter `screenshots/design`.


## Aktuelle Abnahme (demo-5)

- **172 automatisierte Assertions bestanden**. Neu: gültige Trefferbuffs, Stun/Root und Kontrollimmunität, unterbrochene Casts, Root erlaubt Angriffe, Fokus beschleunigt Ladung, Stun→Fokus→volle Projektil-Ulti trifft, Selbst-Ulti und positive Sekundäreffekte, Terrain-Symmetrie/Wirkung/Ablauf sowie CPU-Wegfindung um Gefahrenfelder.
- **32 Simulations-/Replay-Paare in Unity bestanden**, Linux-Build mit Unity 2022.3.62f3 erfolgreich.
- Gerenderter Spieler: Menü, Auswahl, CPU-Kampf, Ergebnis und Replay, beide manuellen Modi, vier Wetterarenen, acht Figuren, fünf Skill-Lieferformen, Aufladen, Druckring und beide F1-Hilfeseiten. Zusätzlich echte Stun-/Fokuszustände, Aufladung und Projektilfreigabe sowie gleichzeitige positive/negative Terrainstatus dargestellt. **31 PNG-Aufnahmen**; die allgemeinen parallelen Ulti-Schaufenster besitzen künstlichen Kontrollschutz, damit sich die Figuren dort nicht gegenseitig die zu prüfende Freigabe unterbrechen. Der separate Stun/Fokus-Test besitzt diesen Schutz nicht.
- **168 CPU-Kämpfe**, alle durch K. o., Mittel 48.11 s; verbleibende Asymmetrien und Methode in [BALANCE-demo5.md](BALANCE-demo5.md).
- Die Grafikprüfung und Simulationseingaben ersetzen keine Prüfung tatsächlicher gleichzeitiger Tastendrücke. Frühere Replay-Versionen werden abgewiesen.

Bilder: [Stun mit Fokus-Ladung](screenshots/v5/15-stun-focus-charge.png), [Einsteins Ulti](screenshots/v5/16-combo-release.png), [Kartenhilfe](screenshots/v5/14-map-help.png).

## Frühere Abnahme (demo-4)

- **142 automatisierte Assertions bestanden**, darunter neue Prüfungen für Nahkampfwinkel und Reichweite, Ausweichen während der Vorbereitung, Vorstoß/Bewegung/Rückstoß an Deckung, blockierte Projektile, verzögert aktive Bodenfelder, langsameres Rückwärtsgehen, Ring-Schaden und Tod ohne Wiederbelebung durch Regeneration.
- CPU-Wegfindung durch freie Korridore und aus direktem Kontakt mit einer Hinderniskante geprüft. Ein zusätzlicher Test bestätigt Angriffe auf einen dauerhaft fliehenden Spieler. Alle fünf aufgeladenen Skill-Lieferformen werden als Eingabe-Replay exakt reproduziert.
- **32 Simulations-/Replay-Paare in Unity** bestanden; Linux-Build mit Unity 2022.3.62f3 erfolgreich.
- Gebauter Spieler durchlief Menü, Zufallsauswahl, CPU-Match samt Ergebnis/Replay und beide manuellen Modi. Zusätzlich wurden alle vier Wetterarenen, acht Figuren, aufgeladene Skills, Nahkampf, Vorstoß, Projektil, Bodenfeld und Selbstschutz gerendert. Druckring und F1-Skillhilfe wurden ebenfalls aufgenommen.
- Balance-Abnahme über **168 CPU-Kämpfe**: 166 K. o., zwei Timeouts, keine Unentschieden; Durchschnitt 61,02 s, Median 44,78 s. Gesamt-Siegquoten der Städte zwischen 31 % und 67 %. Einzelne Paarungen bleiben deutlich asymmetrisch; Details, Datenhashes und Grenzen der Stichprobe stehen in [BALANCE-demo4.md](BALANCE-demo4.md).
- Die Interaktionstests speisen Befehle in die Simulation ein; reale gleichzeitige Tastendrücke wurden nicht automatisiert geprüft. Grafikprüfung erfolgt im echten gerenderten Linux-Spieler. Alte Replays benötigen weiterhin ihre damalige Balance-Version.

Screenshots: [Nahkampfbogen](screenshots/v4/10-Melee-effect.png), [Bodenwarnung](screenshots/v4/10-Zone-effect.png), [kleiner gewordene Kampfzone](screenshots/v4/12-final-ring.png), [Skillhilfe](screenshots/v4/13-skill-help.png).

## Frühere Abnahme (demo-3)

- **116 automatisierte Assertions bestanden**. Neu geprüft: verzögerter Treffer durch Flugzeit, geradliniges Vorbeifliegen bei seitlichem Ausweichen, Kollisionsprüfung entlang des Flugsegments, kein zufälliger Dodge bei einem tatsächlichen Treffer, Abwehr von sekundären Statuseffekten sowie Halten/Loslassen/Abbruch und stärkere volle Ladung.
- **32 Simulations-/Replay-Paare in Unity** bestanden (acht Städte × vier Klimata); Linux-Spieler mit Unity 2022.3.62f3 gebaut, Exitcode 0.
- Gerenderter Probelauf bei 1440×900 prüft Menü, Zufallsauswahl, CPU-Kampf mit Ergebnis, beide manuellen Modi samt Replay, alle vier Wetterarenen, acht Figuren sowie aktive Aufladungen und fliegende Hauptskills. Exitcode 0, keine Gameplay-Exceptions.
- CPU-Stichprobe Münster/Bonn über 20 Seeds: mittlere Dauer 89,79 Sekunden. Das ist ein Funktions-/Tempo-Test, keine abgeschlossene Balance-Abnahme; diese Paarung bleibt asymmetrisch.
- Die Darstellung ist prozedural und stilisiert. Skill-Vorbereitung, Abschuss, Erholung und Aufladen sind Gelenkanimationen aus dem Simulationszustand. Projektile haben tatsächliche Trefferpositionen, Kamerabild und Effekte beeinflussen die Simulation nicht.
- Die manuellen Tests injizieren Befehle in die Simulation. Physische gleichzeitige Tastendrücke an der Tastatur wurden nicht automatisiert geprüft. Frühere Replay-Versionen bleiben wegen geänderter Regeln inkompatibel.

Screenshots: [Auswahl mit Zufallsbuttons](screenshots/v3/02-selection.png), [Fernkampf](screenshots/v3/03-battle.png), [Aufladen bei Dürre](screenshots/v3/07-drought-charge.png), [Fliegende Schüsse bei Starkregen](screenshots/v3/08-heavyrain-projectile.png).

## Frühere Abnahme (demo-2)

- **88 automatisierte Assertions bestanden**, einschließlich manueller Eingaben, aller drei Modi, identischer Eingabe-Replays, Bewegungsnormalisierung, Ausweichkosten und -dauer, Treffervermeidung sowie CPU-Seitwärtsschritten, Rückzug und Ausweichen.
- Linux-Build mit Unity **2022.3.62f3** erfolgreich (Exitcode 0); **16 Simulations-/Replay-Paare** in Unity bestanden.
- Gebauter Player bei **1440×900** ausgeführt: Menü, Auswahl, CPU-Kampf, Ergebnis sowie beide manuellen Modi inklusive Replay bestanden; Exitcode 0, keine Gameplay-Exceptions.
- Screenshots aller vier Figuren und der drei Modi visuell geprüft. Charaktere besitzen gegliederte Arme/Beine, unterschiedliche Kleidung und Requisiten, Lauf-/Kampfposen und Fähigkeitseffekte.
- Die manuellen Tests speisen Befehle direkt in die Simulation ein. Physische Tastendrücke und gleichzeitige Tastaturbelegung wurden nicht automatisiert geprüft.
- Die früheren Balance-Massenläufe unten gelten für demo-1; sie wurden nach den Bewegungsänderungen nicht wiederholt. Alte demo-1-Replays werden wegen der geänderten Regeln abgewiesen.

Aktuelle Bilder: [CPU-Kampf](screenshots/v2/03-battle.png), [Spieler gegen CPU](screenshots/v2/05-player-cpu.png), [lokales PvP](screenshots/v2/06-player-player.png).

## Ursprüngliche Abnahme (demo-1)

- Headless-Projekt mit .NET SDK 10.0.112 gegen net9.0 gebaut: **0 Fehler, 0 Warnungen**. Paketquellen sind leer; keine NuGet-Pakete erforderlich.
- Derselbe Core zusätzlich gegen **.NET Standard 2.1 / C# 7.3** kompiliert: **0 Fehler, 0 Warnungen**. Das prüft Sprach-/Bibliothekskompatibilität, ersetzt keinen Unity-Import.
- **66 automatisierte Assertions bestanden** über `dotnet run --project Headless -- test`: RNG-Reproduzierbarkeit und Grenzen, Klimawerte und Mapping, Verteidigungsformel, vollständige Ergebnisidentität, zufällig gewähltes und anschließend gespeichertes Klimaereignis, Basic Attacks, Skills, Ultimate, Ressourcen-Grenzen, unverändertes Ergebnis nach Matchende, ungültige Eingaben, vier Städte in vier Ereignissen, Tod, Timeout und Unentschieden. Alle neun generischen Effekte werden mit kontrollierten Daten auf Cast-Beginn, Wirkung, Cooldown und gegebenenfalls Ablauf geprüft.
- **1.000 Matches Münster–Bonn** mit Seeds 726491–727490 und zufälligem Klima erfolgreich simuliert: 1000:0 Siege, keine Unentschieden, mittlere Dauer 35,42 s.
- Weitere **600 Matches über alle sechs Städtepaarungen**, siehe Balancetabelle in `DATA.md`. Die Werte sind ein technischer Ausgangspunkt, kein fertig austariertes Wettbewerbsspiel.
- Headless-Ergebnis als JSON gespeichert und über den Replay-Befehl erneut eingelesen/verglichen (Ulm–Leipzig, Seed −42, zufällige Umwelt).
- Statischer Review der Unity-Schicht, Assembly-Grenzen, lokalen Ressourcen, Szene und Build-Einstellungen. Ein Material in Resources hält den verwendeten Standard-Shader im Build.

## Nachträgliche Unity-Abnahme auf diesem Rechner

Unity Hub 3.21.3 und Unity Editor **2022.3.62f3** sind installiert; Unity Personal wurde aktiviert. Der Download stammt direkt von Unity und das Editor-Archiv bestand die CRC64-Prüfung. Projekt-Import und C#-Compilation liefen erfolgreich. `BuildTools.BuildLinux` erzeugte einen Linux-Mono-Spieler mit Exitcode 0.

Vor dem Build bestanden **16 weitere Simulations-/Replay-Paare in der tatsächlichen Unity-Runtime** (vier Städte × vier Klimaereignisse). Der gebaute Spieler lief anschließend auf der AMD Radeon Vega 8 mit OpenGL 4.6. Ein opt-in Probelauf durchlief Menü, Stadtwahl, gerenderten Kampf mit 4× Tempo und Resultat. Das vollständige Ergebnis stimmte mit einer erneuten Simulation überein; `last-match.json` wurde im vorgesehenen lokalen Verzeichnis gespeichert. Der Probelauf beendete sich mit Exitcode 0 und ohne Gameplay-Exceptions. Anschließend wurde der Spieler normal für den Nutzer geöffnet.

Die vier Screenshots wurden visuell geprüft:

- [Menü](screenshots/01-menu.png)
- [Stadtwahl](screenshots/02-selection.png)
- [Kampf](screenshots/03-battle.png)
- [Ergebnis](screenshots/04-result.png)

Probelauf wiederholen:

```bash
./Builds/Linux/ClashOfCities.x86_64 -screen-fullscreen 0 -screen-width 1280 -screen-height 800 -clash-smoke-test -clash-smoke-output /tmp/clash-player-smoke -logFile /tmp/clash-player-smoke.log
```

Der normale Start enthält diese Testargumente nicht. Der automatische Probelauf prüft keine tatsächlichen Mausklicks; zusätzliche manuelle Interaktions- und Fenstergrößenprüfungen bleiben sinnvoll. Ein separater Lauf bei abgeschaltetem Netzwerk und Play-Mode innerhalb des Editorfensters wurden nicht durchgeführt. Editorstart im Batchmodus sowie der tatsächliche gerenderte Player sind geprüft. Das ursprüngliche Hindernis „Unity nicht installiert“ ist behoben.

## Zusätzliche manuelle Unity-Abnahme

1. Mit Unity 2022.3 LTS öffnen. Console muss frei von Compile-/Importfehlern sein. `Clash of Cities → Validate Local Data` ausführen.
2. `Assets/Scenes/Battle.unity` starten. Menü und Stadtwahl bei 1280×800 sowie kleinerem Fenster prüfen; Beschreibung und Fähigkeiten lassen sich scrollen.
3. Beide Seiten durch alle vier Städte schalten, Avatare und Werte prüfen. Ungültigen Seed eingeben und Fehlermeldung prüfen.
4. Einen Kampf starten: beide Figuren bewegen sich, Basic Attacks/Skills/Ultimate lösen aus, Kamera hält beide im Bild, HP/Energie/Cooldowns/Timer aktualisieren sich.
5. Pause und Tempo 1×/2×/4× in CPU-Kämpfen und Replays prüfen; menschliche Kämpfe laufen bei 1×. Das Ergebnis muss bei identischer Konfiguration gleich bleiben.
6. Jede Umwelt wählen; Seed und aufgelöstes Ereignis im HUD und Ergebnis prüfen. Replay muss dieselben vollständigen Werte erzeugen; Revanche erhöht den Seed.
7. Nach Matchende Fähigkeitenzählung, Gewinner und Statistiken prüfen. Zum Menü zurückkehren, gespeicherten Kampf erneut starten; auch nach Spielneustart prüfen.
8. Linux-Player über das Build-Menü erzeugen und mit deaktiviertem Netzwerk ausführen. Start, ein vollständiges Match und Replay prüfen.

## Bekannte Grenzen

Stilisierte prozedurale Figuren mit Gelenkanimationen und Mesh-Effekten; keine gescannten historischen Modelle. Musik und Geräusche werden als eigene Chiptune-Clips prozedural erzeugt. Animator-Hooks sind vorhanden, ein eigener Animator-Controller ist optional. Die Arena verwendet eine deterministische Hinderniskollision und Wegfindung über einen Sichtbarkeitsgraphen ohne NavMesh. Shields bleiben bis zum Verbrauch bestehen. Timeout gewinnt nach verbleibendem HP-Anteil; exakter Gleichstand ist ein Unentschieden. Die Balance-Version muss bei Änderungen an Daten oder Regeln erhöht werden. Plattformübergreifende Bitidentität der double-Arithmetik ist nicht zugesichert.
