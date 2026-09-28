# Lokale Daten und Balance

`Assets/ClashOfCities/Resources/GameData.json` ist die gemeinsame Datenquelle für Unity und den Headless-Runner. Änderungen werden lokal geladen; zur Laufzeit gibt es keine Recherche oder Netzwerkverbindung. Die Feldnamen entsprechen `Core/Models.cs`. Die aktuelle Balance-Version ist `balance-9`; bei Änderungen der Werte oder Regeln muss sie für belastbare Replay-Vergleiche erhöht werden. Ein Replay benötigt dieselbe Datendatei und dieselben Simulationsregeln zusätzlich zur MatchConfig.

## Demo-Klimawerte

**Alle Stadtkennzahlen sind frei gewählte Placeholder/Demo Data für technische Tests. Sie sind keine Messungen, kein wissenschaftlicher Index und kein reales Städteranking.** Jede Stadt trägt `isDemoData: true`. Auch die Ereignisgewichte sind Spielregeln, keine Klimaprognose.

Alle Scores liegen zwischen 0 und 100. `sealingScore` bedeutet Versiegelungsgrad: Ein hoher Wert ist schlecht. Die Formel verwendet daher `100 - sealingScore`. Entsprechend ist ein hoher `heatRiskScore` schlecht. Die übrigen Scores bedeuten höhere Qualität bei höheren Werten.

| Stadt | Grün | Wasser | Hitzeschutz | Versiegelung | Resilienz | Hitzerisiko |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Münster | 84 | 72 | 82 | 61 | 79 | 34 |
| Bonn | 72 | 81 | 74 | 64 | 77 | 43 |
| Leipzig | 79 | 65 | 73 | 57 | 80 | 40 |
| Ulm | 70 | 83 | 78 | 60 | 82 | 38 |
| Weimar | 82 | 67 | 76 | 55 | 80 | 39 |
| Mainz | 71 | 82 | 75 | 63 | 81 | 42 |
| Hamburg | 77 | 85 | 72 | 65 | 78 | 41 |
| Berlin | 75 | 76 | 79 | 66 | 83 | 44 |

Für Grün G, Wasser W, Hitzeschutz H, Versiegelung S, Resilienz R und Hitzerisiko T gilt:

```text
ClimatePower = (G*wG + W*wW + H*wH + (100-S)*wU + R*wR + (100-T)*wT)
               / (wG + wW + wH + wU + wR + wT)
```

Die Gewichte werden durch ihre Summe normiert. So bleibt ClimatePower im Bereich 0 bis 100. `Core/ClimateCalculator.cs` ist die ausführbare Referenz.

| Ereignis-ID | Grün | Wasser | Hitzeschutz | Unversiegelt | Resilienz | Geringes Hitzerisiko | Hitzeschaden/s vor Resistenz |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| normal | .20 | .15 | .25 | .10 | .20 | .10 | 0 |
| heatwave | .23 | .18 | .35 | .06 | .10 | .08 | 4 |
| drought | .20 | .30 | .20 | .10 | .12 | .08 | 2 |
| heavyrain | .12 | .28 | .10 | .12 | .30 | .08 | 0 |

Bei `normal` hat Münster beispielsweise ClimatePower 74,4. Das ergibt 872 maximale HP, 34,32 Angriff und 46 Verteidigung. Alle Avatare derselben Stadt erhalten dieselben Grundwerte.

## Mapping auf Kampfwerte

Die aktuellen Koeffizienten stehen zentral im `balance`-Objekt. Mit P = ClimatePower:

| Kampfwert | Formel mit Demo-Koeffizienten |
| --- | --- |
| MaxHealth | 500 + 5*P |
| AttackPower | 12 + .3*P |
| Defense | 5 + .3*G + .2*R |
| Energy | 80 + .5*W |
| EnergyRegeneration | 4 + .05*W pro Sekunde |
| HealthRegeneration | .025*G pro Sekunde |
| HeatResistance | H/100 |
| MovementStamina | 1 + .005*(100-S) |
| CooldownModifier | max(.1, 1 - .003*R) |

Verteidigung reduziert normalen Schaden über `max(0, raw) * 100 / (100 + max(0, Defense))`; die Konstante 100 ist `balance.defenseConstant`. HeatResistance vermindert Ereignishitze. Die Simulation läuft mit festen 1/60-Sekunden-Schritten; Rendering verändert die Regeln nicht.

## Movesets demo-5

Die aktuelle Konfiguration umfasst acht Figuren und 24 Fähigkeiten. Klimawerte bestimmen die Grundwerte; die Movesets bestimmen Reichweite, Trefferfenster und Kombinationsmöglichkeiten.

| Avatar | Q / K | E / L | R / I (aufladbar) |
|---|---|---|---|
| Annette von Droste-Hülshoff | Fesselvers (Projectile) | Rüschhaus-Ruhe (Self) | Lebendiger Blättersturm (Zone) |
| Ludwig van Beethoven | Schicksalsansturm (Dash) | Paukenschlag (Melee) | Donnerndes Finale (Melee) |
| Johann Sebastian Bach | Taktbrecher (Projectile) | Schutzchoral (Self) | Fugenkreis (Zone) |
| Albert Einstein | Zeitstopp (Projectile) | Lichtstoß (Melee) | Raumzeitbruch (Projectile) |
| Johann Wolfgang von Goethe | Faustsprung (Dash) | Feuerverse (Projectile) | Fausts Entscheidung (Dash) |
| Johannes Gutenberg | Bleisatz-Ansturm (Dash) | Bleiletter (Projectile) | Eiserne Druckerpresse (Self) |
| Johannes Brahms | Ungarischer Tanz (Projectile) | Wiegenlied (Projectile) | Prestissimo (Self) |
| Alexander von Humboldt | Botanischer Impuls (Projectile) | Expeditionsstoß (Melee) | Wurzelnetz (Zone) |

`delivery` ist `Projectile`, `Melee`, `Dash`, `Zone` oder `Self`. `effectType` beschreibt unabhängig davon die Wirkung: Schaden, Schaden über Zeit, Slow, Rückstoß, Heilung, Schild oder Buff. `description` erklärt Absicht und Gegenmittel im Spiel. Die konkrete Konfiguration aller 24 Fähigkeiten steht in der JSON-Datei; Tabellen mit identischen Slotbudgets aus demo-1 bis demo-3 gelten nicht mehr.

- **Projectile:** Geschwindigkeit und Radius sind pro Fähigkeit konfiguriert. Projektile treffen geometrisch entlang ihrer Flugbahn und werden von Hindernissen blockiert. `range` begrenzt ihre Reichweite.
- **Melee:** `range` und `coneDegrees` definieren einen gerichteten Nahkampfkegel. Zurückweichen, hinter den Angreifer gelangen oder ausweichen kann den Treffer verhindern.
- **Dash:** `dashDistance` begrenzt den Vorstoß. Feste Hindernisse blockieren den Weg; der Angriff ist kein Teleport durch Deckung.
- **Zone:** Die sichtbare Markierung warnt für `telegraphSeconds`. Danach wird das gesamte Schadensbudget über `zoneDuration` verteilt in Pulsen abgegeben; ein voller Betrag wird nicht bei jedem Puls erneut ausgeteilt. Bewegung aus der Zone reduziert die erhaltene Wirkung. `areaOfEffect` bestimmt den Radius.
- **Self:** Die Fähigkeit betrifft nur den Anwender. Schilde bleiben bis zum Aufbrauchen bestehen; `duration` ist keine Schildablaufzeit. Heilung bei vollen HP wird nicht verbraucht. Der einzige Heilskill ist bewusst langsamer und teurer als viele offensive Fähigkeiten.

Der Betrag skaliert mit `baseDamage * (balance.abilityScalingBase + gewählter_Klimascore/100)`; `abilityScalingBase` beträgt 0,5. Alle Ultimates skalieren mit ClimatePower. `baseDamage` bedeutet für Schild/Heilung deren Budget, für Buffs wird `magnitude` verwendet. DoT verteilt seinen Betrag über `duration`; Slow und Rückstoß kombinieren Schaden mit Kontrolle. `unsealed` ist `100 - sealingScore`. Aufgeladene Ultimates binden ihren Anwender während der Vorbereitung und sind frühestens nach acht Sekunden verfügbar. Stärke wächst mit der Ladung; ein kurzer oder voll geladener Einsatz hat unterschiedliche Risiken.

Kosten, Cooldowns und Nutzungszähler werden bei Cast-Beginn verbucht; ein verfehlter oder abgebrochener Cast zählt als Nutzung. Die Fähigkeiten sind künstlerische Spielinterpretationen und keine historischen Tatsachenbehauptungen. Gleiche Klimawerte bedeuten bei verschiedenen Werkzeugen nicht automatisch identische Siegraten. Der aktuelle gemessene Stand steht in [BALANCE-demo4.md](BALANCE-demo4.md).

## Historische Verbindungen und Quellen

Die folgenden Primärquellen wurden für die Projektbeschreibung geprüft (25.09.2026). Nur die historischen Verbindungen sind belegt; die Klimawerte oben stammen ausdrücklich nicht aus diesen Quellen.

- **Münster → Annette von Droste-Hülshoff:** Auf Burg Hülshoff **bei** Münster geboren; nach dem Tod des Vaters 1826 lebte sie auf Haus Rüschhaus. Der Datensatz verwendet deshalb `Lebensmittelpunkt`, ohne Burg Hülshoff als innerstädtischen Geburtsort auszugeben. Quelle: [Stadtmuseum Münster, Annette von Droste-Hülshoff](https://www.stadt-muenster.de/museum/museum/33-kabinette/2-obergeschoss/192-annette-von-droste-huelshoff).
- **Bonn → Ludwig van Beethoven:** 1770 in Bonn geboren und dort bis 1792 wohnhaft; Verbindung `Geburtsort`. Quelle: [Beethoven-Haus Bonn, Biographie](https://www.beethoven.de/Beethoven).
- **Leipzig → Johann Sebastian Bach:** Übernahm 1723 das Thomaskantorat in Leipzig; Verbindung `Wirkungsort`. Quelle: [Bach-Archiv Leipzig, Tagung zur Neubesetzung des Thomaskantorats 1723](https://www.bach-leipzig.de/en/bach-archiv/conference-replacement-thomaskantorat-1723-and-history-protestant-church-cantata-around).
- **Ulm → Albert Einstein:** Am 14. März 1879 in Ulm geboren; seine Familie zog im Juni 1880 nach München. Verbindung `Geburtsort`, ohne eine lange Wirkungszeit in Ulm zu behaupten. Quelle: [Stadt Ulm, Albert Einstein und Ulm](https://www.ulm.de/tourismus/stadtgeschichte/personen/einstein-der-relative-ulmer).

Neue Städte benötigen eindeutige IDs, Scores im Bereich 0–100 und mindestens einen zugeordneten Avatar. Neue Avatare referenzieren eine vorhandene Stadt und drei vorhandene Fähigkeiten; Slot 3 ist die Ultimate. Neue Daten sollten dieselben Budgetregeln erfüllen und ihre historischen Verbindungen belegen. Vor realen Klimadaten sind Quelle, Erhebungsjahr, räumliche Abgrenzung und Normalisierung zu dokumentieren.


## Bewegung, Deckung und Klimakarten ab demo-4

CPU gegen CPU, Spieler gegen CPU und lokales PvP verwenden dieselben Kampfregeln. Beide Spieler können Bewegung, Basisangriff, zwei Fähigkeiten, aufladbare Ultimate und Ausweichen steuern. Replays speichern die vollständigen Eingaben und benötigen dieselbe Balance-Version; alte Replays sind nicht mit demo-4 kompatibel.

Ausweichen kostet Energie und hat einen Cooldown. Rückwärtsbewegung ist langsamer als Vorwärtsbewegung. Nahkämpfer können mit ihren Vorstößen aufschließen; Fernkämpfer müssen zwischen Schussvorbereitung, Ausweichen und Positionswechsel abwägen. Ab Sekunde 45 schrumpft der sichere Arenabereich bis Sekunde 120 auf Radius 6,5. Rückwärtsbewegung verwendet den Faktor 0,72; diese Werte stehen in `balance`. Das begrenzt langes Spiel am Rand. Außerhalb entsteht Schaden, sodass endloses Ausweichen nicht zu einem folgenlosen Stillstand führt.

Seed und aufgelöstes Klima erzeugen dieselbe Arena. Sichtbare feste Hindernisse liefern echte Deckung: Sie blockieren Bewegung, Vorstöße und Projektile. Die CPU berücksichtigt freie Schusslinien und sucht Wege um Deckung. Bodenmuster, Pflanzen außerhalb des Spielfelds und Regenpartikel bleiben Kulisse. Die Klima-Gewichte und Hitzeschäden oben bleiben die klimatischen Kampfregeln. Die Stadtwerte sind weiterhin frei gewählte Demowerte, keine Messungen.

Zusätzliche historische Verbindungen, Primärquellen geprüft am 25.09.2026:

- **Weimar → Johann Wolfgang von Goethe:** kam 1775 nach Weimar; Verbindung `Wirkungsort`. [Klassik Stiftung Weimar, digitale Bildungsangebote](https://www.klassik-stiftung.de/bildung/ortsunabhaengige-angebote/digitale-angebote/) und [Goethe-Forschungsprojekte, Arbeitsbibliothek am Frauenplan](https://goethe.klassik-stiftung.de/de/forschen/forschungsprojekte/).
- **Mainz → Johannes Gutenberg:** dort entstanden seine Druckwerkstätten und die Bibelausgabe; Verbindung `Wirkungsort`, ohne das unsichere genaue Geburtsjahr als sicher auszugeben. [Stadt Mainz, Gutenberg und Mainz](https://www.mainz.de/microsite/gutenberg/zeit/gutenberg_mainz).
- **Hamburg → Johannes Brahms:** 1833 im Hamburger Gängeviertel geboren; Verbindung `Geburtsort`. [Stadt Hamburg, Johannes Brahms](https://www.hamburg.de/politik-und-verwaltung/senat/hamburger-ehrenbuerger/ehrenbuerger-1813-bis-heute/johannes-brahms-238066).
- **Berlin → Alexander von Humboldt:** am 14. September 1769 in Berlin geboren; Verbindung `Geburtsort`. [Staatsbibliothek zu Berlin, Humboldt: Leben](https://humboldt.staatsbibliothek-berlin.de/leben/).

## Trefferstatus und Terrain (demo-5)

Fähigkeiten besitzen `onHitSelfEffect`, `onHitSelfDuration`, `onHitSelfMagnitude` sowie die entsprechenden `onHitTarget*`-Felder. Fokus, Tempo und Verstärkung sind positive Effekte; Stun, Root, Slow und Vulnerable eröffnen oder verändern Trefferfenster. Der Validator prüft Effektnamen, Dauer und Stärke. `maxStunSeconds=1.4`, `maxRootSeconds=1.8`, `controlImmunitySeconds=2` begrenzen Kontrolle; addierte Modifikatoren sind auf 0.65 begrenzt.

Terrain ist aus Seed, Arenagröße und Klima deterministisch und symmetrisch: Regeneration heilt 4 HP/s und gibt 5 Energie/s, Fokus verkürzt Ladung/Vorbereitung bis zur Modifikatorgrenze, Tempo erhöht Bewegung um 25 %, Schlamm verlangsamt um 25 %. Hitze verursacht 7 Schaden/s vor der lokalen Hitzeresistenzminderung; globales Wetter bleibt zusätzlich wirksam. Temporäre Terrainstatus werden beim Aufenthalt erneuert und laufen nach dem Verlassen innerhalb von 0.15 s aus. Diese Terrainwerte stehen derzeit zentral in `BattleSimulation.ApplyTerrain`.

`MatchConfig.difficulty` speichert Normal (0, Standard), Easy (1) oder Hard (2). Die Stufe verändert CPU-Entscheidungen und Reaktionszeiten, nicht Lebenspunkte oder Schaden. PvP ignoriert sie; Replays übernehmen sie.
