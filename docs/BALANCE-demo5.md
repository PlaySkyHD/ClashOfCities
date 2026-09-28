# Balance-Stichprobe demo-5

Gemessen am 25.09.2026: alle 28 Städtepaarungen, drei Seeds (726491–726493), beide Startseiten, zufälliges Wetter. 168 CPU-Kämpfe, 42 Einsätze je Stadt. Kleine Funktions- und Ausreißerstichprobe; menschliche Wettkampfbalance ist damit nicht nachgewiesen.

Reproduktion: `dotnet run --project Headless -- simulate 1 muenster bonn 726491 random`, für alle Paare, Seeds und umgekehrten Startseiten wiederholen.

- Daten-SHA256: `87efe7d298c51f8d745bd247f63361418462480c30040391abcec23fc3d20e3d`
- Core-SHA256: `f05d5f29ad586d158cd4d6782b9a536288446b47460a0faa7640fdcf382d9728` (sortierte Dateinamen + NUL + Inhalt).
- K. o.: 168/168; mittlere Dauer 48.11 s, Median 43.35 s.
- Aufgelöstes Klima: heavyrain: 56, heatwave: 56, drought: 56. Normal wird separat in den automatisierten Tests geprüft.

| Stadt | Siege / 42 | Quote |
|---|---:|---:|
| Münster | 15 / 42 | 35.7% |
| Bonn | 24 / 42 | 57.1% |
| Leipzig | 33 / 42 | 78.6% |
| Ulm | 24 / 42 | 57.1% |
| Weimar | 10 / 42 | 23.8% |
| Mainz | 27 / 42 | 64.3% |
| Hamburg | 11 / 42 | 26.2% |
| Berlin | 24 / 42 | 57.1% |

Die anfängliche Stichprobe zeigte 120.5 s mittlere Dauer und 42/42 Siege für Mainz. Ursache für viele lange Matches war erneutes Hineinlaufen der CPU in gerade verlassene Gefahrenfelder. Die Wegfindung berücksichtigt nun Gefahrenkreise, mit einem Regressionstest für das Erreichen des Gegners hinter einem Feld. Mainzs Schild-Ulti hat einen kleineren Schild, höhere Energiekosten und längeren Cooldown.

Leipzig bleibt in dieser Stichprobe stärker, Weimar/Hamburg schwächer. Die Werte sind keine Garantie für ausgeglichene einzelne Paarungen. Die neuen kontrollierten Tests prüfen unabhängig vom CPU-Ergebnis, dass Stun → Fokus → volle Ulti einen tatsächlichen Treffer ermöglicht und Deckung/Ausweichen einen Trefferbuff verhindern.
