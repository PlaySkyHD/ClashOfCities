# Balance-Stichprobe demo-4

Gemessen am 25.09.2026 mit der deterministischen Headless-Simulation. Dies ist eine kleine CPU-Stichprobe zur Suche nach groben Ausreißern, keine nachgewiesene Wettbewerbsbalance für menschliche Spieler. Alle Klimawerte sind Demodaten.

## Methode

Alle 28 ungeordneten Paarungen der acht Städte, je drei Seeds (726491, 726492, 726493), jeweils mit beiden Startseiten: **168 Matches**. Umgebung `random`; die aufgelösten Umgebungen sind seedabhängig und decken in dieser kleinen Stichprobe nicht zwingend alle vier Klimalagen gleich häufig ab. Jede Stadt spielt 42 Matches. Keine Spiegelduelle, keine menschlichen Eingaben.

Reproduktion eines Einzelmatches: `dotnet run --project Headless -- simulate 1 muenster bonn 726491 random`. Für jede Paarung beide Reihenfolgen und alle drei Seeds ausführen. Die Ausgabe enthält Schaden, Zeit, Ergebnis und Fähigkeitsnutzungen. Zuvor bauen, damit die aktuelle JSON-Datei im Ausgabeverzeichnis liegt.

- Daten-SHA256 (`GameData.json`): `cd71abaef2fa7f62824d0dbe9ebdfec1c9ba0ece1f0996cf1e4dc52ed34832dd`
- Core-SHA256: `e17b030c9119d481ef46d943dade8d429043dce5dde6f7d8c893bce6b8935559` (SHA256 über die nach Dateiname sortierten Core-Quelldateien, jeweils Dateiname + NUL + Dateiinhalt).

## Ergebnisse

Aufgelöste Umgebungen: drought: 56, heatwave: 56, heavyrain: 56. `normal` ist in diesen drei Seeds nicht enthalten.

- K. o.: **166/168 (98.8 %)**.
- Zeitlimit: 2; Unentschieden: 0.
- Dauer: Mittel **61.02 s**, Median **44.78 s**, Minimum 28.80 s, Maximum 180.00 s.
- Durchschnittlicher tatsächlich verursachter Gegnerschaden pro Kampf (beide Seiten zusammen): **1804.3 HP**. Schildabsorption und Umweltschaden zählen nicht als Gegnerschaden.

| Stadt | Siege / 42 | Siegquote |
| --- | ---: | ---: |
| Münster | 17 / 42 | 40.5 % |
| Bonn | 23 / 42 | 54.8 % |
| Leipzig | 26 / 42 | 61.9 % |
| Ulm | 24 / 42 | 57.1 % |
| Weimar | 28 / 42 | 66.7 % |
| Mainz | 21 / 42 | 50.0 % |
| Hamburg | 16 / 42 | 38.1 % |
| Berlin | 13 / 42 | 31.0 % |

| Paarung | Siege links : rechts : Draw | Mittlere Dauer |
| --- | ---: | ---: |
| Münster – Bonn | 6 : 0 : 0 | 48.32 s |
| Münster – Leipzig | 0 : 6 : 0 | 145.75 s |
| Münster – Ulm | 4 : 2 : 0 | 139.33 s |
| Münster – Weimar | 3 : 3 : 0 | 43.40 s |
| Münster – Mainz | 0 : 6 : 0 | 61.52 s |
| Münster – Hamburg | 2 : 4 : 0 | 101.24 s |
| Münster – Berlin | 2 : 4 : 0 | 107.91 s |
| Bonn – Leipzig | 6 : 0 : 0 | 38.68 s |
| Bonn – Ulm | 2 : 4 : 0 | 36.18 s |
| Bonn – Weimar | 0 : 6 : 0 | 29.20 s |
| Bonn – Mainz | 4 : 2 : 0 | 39.80 s |
| Bonn – Hamburg | 6 : 0 : 0 | 30.84 s |
| Bonn – Berlin | 5 : 1 : 0 | 32.26 s |
| Leipzig – Ulm | 5 : 1 : 0 | 107.15 s |
| Leipzig – Weimar | 3 : 3 : 0 | 40.66 s |
| Leipzig – Mainz | 3 : 3 : 0 | 52.97 s |
| Leipzig – Hamburg | 3 : 3 : 0 | 66.02 s |
| Leipzig – Berlin | 6 : 0 : 0 | 100.08 s |
| Ulm – Weimar | 4 : 2 : 0 | 34.58 s |
| Ulm – Mainz | 3 : 3 : 0 | 46.04 s |
| Ulm – Hamburg | 5 : 1 : 0 | 68.07 s |
| Ulm – Berlin | 5 : 1 : 0 | 77.13 s |
| Weimar – Mainz | 6 : 0 : 0 | 40.21 s |
| Weimar – Hamburg | 4 : 2 : 0 | 32.81 s |
| Weimar – Berlin | 4 : 2 : 0 | 33.06 s |
| Mainz – Hamburg | 3 : 3 : 0 | 40.52 s |
| Mainz – Berlin | 4 : 2 : 0 | 42.03 s |
| Hamburg – Berlin | 3 : 3 : 0 | 72.79 s |

## Einordnung und nächste Prüfungen

Ein erster Arbeitsstand zeigte eine zu starke Goethe-Kombination (40 Siege aus 42). Daraufhin wurden die Budgets von Vorstoß, Feuerprojektil und Ultimate reduziert. Einsteins Kontrollzone und Brahms’ offensive Zeitfenster wurden angepasst. Nach der Wegfindungskorrektur war Humboldt mit 7/42 Siegen zu schwach. Sein Samen erhielt deshalb einen kurzen Slow und etwas mehr Schaden, der Nahstoß etwas mehr Schaden. Anschließend wurden die 42 betroffenen Berlin-Matches neu simuliert; die 126 Paarungen ohne Berlin blieben unverändert, da weder ihre Daten noch die Core-Regeln geändert wurden. Die Tabelle oben stammt vom abschließend gemessenen Stand; diese Zwischenmessung ist wegen gleichzeitig korrigierter CPU-Wegfindung kein isolierter Wirksamkeitsnachweis der Zahlenänderung.

Die Startseiten werden getauscht, doch die KI ist nur eine feste Strategie. Einzelne 6:0-Paarungen können bei sechs Matches noch keine belastbare Aussage über Spielerstärke liefern. Reichweiten, Hindernisse und Klima werden nicht voneinander isoliert. Mehr Seeds, alle vier Umgebungen getrennt, Tests mit gleichen Klimawerten und menschliche PvP-Runden bleiben nötig. Es wird daher weder eine 50:50-Balance noch ein reales Städteranking behauptet.
