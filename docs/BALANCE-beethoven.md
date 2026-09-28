# Beethoven-Abschwächung · balance-6

Identische Core-Regeln, je 42 CPU-Kämpfe vor/nach den Datenänderungen: Bonn gegen jede der sieben anderen Städte, drei Seeds 726491–726493 und beide Startseiten, Klima `random`. Kleine CPU-Stichprobe, keine Garantie für menschliche Wettbewerbsbalance.

| Fähigkeit | Vorher | Jetzt |
|---|---|---|
| Schicksalsansturm | Schaden 28, Cooldown 6 s, Energie 20, Stun 1.15 s | Schaden 26, Cooldown 6.75 s, Energie 22, Stun 1.1 s |
| Paukenschlag | Schaden 45, Trefferbuff +22 % / 4 s | Schaden 42, Trefferbuff +18 % / 3.5 s |
| Donnerndes Finale | Schaden 104, Cooldown 24 s | Schaden 98, Cooldown 25.5 s |

Die Slow-Dauer des Ansturms sinkt von 1.3 auf 1.2 s. Fokus bleibt erhalten, damit die Kombination in eine kurze Ulti weiter funktioniert. Stadtwerte, andere Figuren und Basisangriffe bleiben unverändert.

Beethoven gewinnt vorher **24/42**, danach **14/42**. Die mittlere Kampfdauer steigt von **36.01 s** auf **37.42 s**. Eine erste, stärkere Kandidaten-Abschwächung wurde nach nur 8/42 Siegen verworfen.

| Gegner | Siege vorher / 6 | Siege nachher / 6 |
|---|---:|---:|
| muenster | 1 | 0 |
| leipzig | 2 | 0 |
| ulm | 2 | 0 |
| weimar | 6 | 5 |
| mainz | 6 | 5 |
| hamburg | 6 | 4 |
| berlin | 1 | 0 |

Aktuelle Daten-SHA256: `20971941bd659a5ca0ebb8d2e40e450affcddce1178d192549559a7da72de4b4`. Reproduktion: `dotnet run --project Headless -- simulate 1 bonn muenster 726491 random`, dann alle obigen Paarungen, Seeds und umgekehrte Startseiten. Frühere Replays benötigen die damaligen Regeln und Daten; die Versionskennung ist deshalb `balance-6`.
