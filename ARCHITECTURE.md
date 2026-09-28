# Shared implementation contract
Core namespace ClashOfCities.Core, C# compatible with Unity 2022.3. Plain serializable public fields, no Unity dependencies. Models.cs is shared contract; coordinate before altering it.

BattleSimulation(GameData data, MatchConfig config) validates data/config and creates state. Properties Fighters (FighterState[]), Config (resolved copy), Environment (ClimateEventDefinition), Elapsed (double), IsFinished (bool), Result (MatchResult). Step() advances exactly balance.tickSeconds; RunToEnd() returns MatchResult. event Action<BattleEvent> Event emitted for Attack, Ability, Hit, Heal, Death. SeededRandom(uint seed): NextDouble(), NextInt(int exclusiveMax). ClimateCalculator.Calculate(CityDefinition, ClimateEventDefinition) and Map(CityDefinition, ClimateEventDefinition, BalanceConfig). DamageCalculator.Calculate(double raw, double defense, BalanceConfig balance) deterministic defense reduction.

Unity loads Resources/GameData.json with JsonUtility. Runtime bootstrap constructs arena, camera, presentation, UI (OnGUI allowed for dependency-free MVP). Scene committed, runtime bootstrap on scene load. Movement simulation in X/Z plane; render interpolation only, no physics controls simulation. Ability slots 0/1 active, third ultimate; generic effectType values Damage, Heal, Shield, Knockback, Slow, Buff, Debuff, AreaDamage, DamageOverTime. climateScaling values power, green, water, heatProtection, resilience, unsealed. sealingScore means percentage sealed (higher is worse). Environmental weights normalized.

Headless links same Core files and loads same JSON using System.Text.Json IncludeFields. No runtime network or packages. Seed replay uses fixed ticks. Demo values expressly non-scientific.

## Implementation decisions

The compact simulation keeps decisions and ability resolution in BattleSimulation; there are no per-avatar behavior scripts. DataValidator rejects malformed local data and mismatched selections. BattleSimulation snapshots source definitions and balance; callers treat exposed state as read-only except controlled tests. Step uses fixed tick duration and alternating action order. Status/environment updates precede decisions. Generic effects refresh by source/type. RNG for environment selection is independent of combat RNG so a resolved random event replays exactly.

Unity presentation uses IMGUI for all screens without registry dependencies. GameBootstrap runs after scene loading, creates arena/camera, loads local JSON and drives fixed ticks with an accumulator. FighterView interpolates state and reacts to events; animation never triggers simulation damage. Optional Animator parameters: Moving/Dead bools, Attack/Ability/Hit/Death triggers. Resource ArenaMaterial references the built-in Standard shader. OwnedMaterial destroys the per-object material. The world is a flat unobstructed arena, so direct deterministic movement is adequate; future obstacles require a deterministic navigation strategy in Core rather than frame-dependent physics.

BuildTools validates data and builds the enabled scene for Linux from Unity Editor. Only bundled Unity modules are referenced; no external package registration, Cinemachine, NavMesh package, test framework package or networking dependency. Headless uses the identical Core sources with package-free executable assertions and batch simulations.

Persistence stores one latest MatchResult under Application.persistentDataPath. Replay uses stored resolved config; balance version mismatch is rejected. Data/config snapshots across software releases are not embedded. Full historical replay requires preserving the exact dataset and simulation version. UI stores save failures as visible status instead of failing the match.

See docs/VERIFICATION.md for executed checks and Unity player validation; docs/DATA.md for numeric formulas, historical sources and measured MVP balance.

The optional -clash-smoke-test player flag runs a complete rendered match, compares its result with a fresh simulation, writes QA screenshots through Unity’s bundled ScreenCapture module, and exits. Normal gameplay is unaffected.


## demo-2: controls, movement and visuals

`MatchConfig.mode` selects CpuVsCpu, PlayerVsCpu or local PlayerVsPlayer. `FighterInput` is a plain struct consumed at each fixed tick; CPU-owned sides ignore supplied commands. Unity captures held movement/basic attack plus buffered skill/dodge presses. Manual games run at 1× and pause on focus loss. `MatchResult.inputs` records human-mode frames with contiguous tick numbers. Replay construction supplies those frames, rejects malformed timing/exhausted recordings, and ignores live keyboard input. Both modes use the same damage, energy and cooldown rules. Version demo-2 intentionally rejects old replay rules.

CPU movement continues while recovering: pursuit, circling and brief retreats; enemy cast wind-ups can trigger lateral dodges. Human movement permits chasing and retreating at will, with auto-aim toward the opponent. Arena bounds and minimum separation apply in Core. Derived per-tick velocities drive articulated hips/knees/arms, backwards walking and dodge poses. Visual meshes, themed motes, healing crosses, shield bands and expanding rings remain presentation-only; their lifetime and population are bounded. Procedural mesh/material owners release allocations.

## demo-3: Fernkampf und wetterabhängige Arenen

Die Simulation besitzt aktive `ProjectileState`-Instanzen mit festen Richtungen, Geschwindigkeiten und Lebensdauern. Sichtbare Projektile folgen diesen Zuständen; Darstellung verursacht keinen Schaden. Kollisionsprüfungen entlang des zurückgelegten Segments verhindern Durchtunneln. Offensive Statuseffekte werden erst bei Kollision angewendet.

`FighterInput.ultimate` ist ein gehaltener Zustand: Halten lädt, Loslassen feuert, Ausweichen bricht ab. `FighterState.Charge` liefert die normalisierte Ladung an Figur und HUD. Eingabe-Replays speichern auch die Halte-/Loslassfolge.

`ArenaView.Build(halfSize, seed, climateId)` erzeugt eine rein visuelle, wetterabhängige Arena mit separatem Zufallsgenerator. Aufgelöstes Klima und Seed aus `MatchConfig` reichen zum Wiederaufbau derselben Karte. Freie Kampfwege vermeiden dekorative Hindernisse ohne passende Simulationskollision. Zufallsauswahl im Menü erzeugt konkrete Stadt-/Avatar-IDs und einen Seed vor Matchbeginn.

## demo-4: Lieferformen, Deckung und Druckzone

`AbilityDefinition.delivery` trennt den Weg einer Fähigkeit (`Melee`, `Dash`, `Projectile`, `Zone`, `Self`) von ihrer Wirkung. Reichweite, Kegelwinkel, Vorstoßdistanz, Projektilgröße/-tempo, Warnzeit und Zonendauer sind lokale Daten. Alle offensive Wirkungen werden erst nach der passenden Geometrie- und Ausweichprüfung angewendet.

`ArenaLayout.Create(seed, halfSize)` liefert dieselben `ArenaObstacle`-Kreise an Simulation und Darstellung. Bewegung, Vorstoß, Rückstoß, Sichtlinie und Projektilflug berücksichtigen sie; die CPU nutzt einen Sichtbarkeitsgraphen um Hindernisse. `BattleEffectsView` zeigt `AreaState`-Warnphasen, aktive Felder und `SafeRadius` an.

Die Kampfzone und Rückwärtsgeschwindigkeit werden durch Balanceparameter gesteuert. Skillhilfe und HUD lesen die lokalen Ability-Daten. Manuelle Eingaben bleiben im bestehenden Replayformat; die Balance-Version verhindert das Abspielen alter Regeln mit neuem Code.
