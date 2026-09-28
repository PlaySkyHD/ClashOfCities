using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        enum ScreenMode { Menu, Selection, Battle, Result }
        readonly Color cyan = new Color(.23f,.82f,.85f), amber = new Color(1,.66f,.28f);
        GameData data;
        BattleSimulation simulation;
        MatchResult savedResult;
        FighterView[] views;
        BattleCamera battleCamera;
        Camera gameCamera;
        ArenaView arena;
        ProjectileView projectileView;
        BattleEffectsView effectsView;
        bool showSkillHelp, pausedBeforeHelp, showMapHelp;
        readonly System.Random selectionRandom = new System.Random();
        ScreenMode screen;
        int cityA, cityB=1, avatarA, avatarB, environment;
        string seedText = "726491", error = "", persistenceMessage = "";
        double accumulator;
        float speed=1;
        bool paused, smokeTesting, replayViewing;
        MatchMode selectedMode = MatchMode.PlayerVsCpu;
        readonly FighterInput[] pending = new FighterInput[2];
        bool LiveHuman { get { return simulation != null && !replayViewing && simulation.Config.mode != MatchMode.CpuVsCpu; } }
        static string ModeName(MatchMode mode) { return mode == MatchMode.PlayerVsCpu ? "Spieler gegen CPU" : mode == MatchMode.PlayerVsPlayer ? "Spieler gegen Spieler (lokal)" : "CPU gegen CPU"; }
        string Controls(int side) { if(UsesPad(side))return "P"+(side+1)+": Stick · A Angriff · X/Y Skills · R laden · B ausweichen · + Pause"; return side == 0 ? "P1: WASD · Leertaste Angriff · Q/E Skills · R laden · Shift links ausweichen" : "P2: Pfeile · J Angriff · K/L Skills · I laden · Shift rechts ausweichen"; }
        void ClearInput() { pending[0] = default(FighterInput); pending[1] = default(FighterInput); }
        void OnApplicationFocus(bool focused) { if (!focused) { ClearInput(); if (screen == ScreenMode.Battle && LiveHuman) paused = true; } }
        void TogglePause() { if(paused&&LiveHuman&&DeviceProblem(simulation.Config.mode)!="")return; if(showSkillHelp) { ToggleSkillHelp(); return; } paused = !paused; ClearInput(); GUIUtility.keyboardControl = 0; }
        void ToggleSkillHelp()
        {
            if(showSkillHelp) {showSkillHelp=false;paused=pausedBeforeHelp || !Application.isFocused || (LiveHuman&&DeviceProblem(simulation.Config.mode)!="");}
            else {pausedBeforeHelp=paused;showSkillHelp=true;paused=true;}
            ClearInput();GUIUtility.keyboardControl=0;
        }
        FighterInput ReadInput(int side)
        {
            var command = pending[side];
            if(UsesPad(side))
            {
                var pad=gamepads.Find(inputDevice[side]);
                if(pad==null){pending[side]=default(FighterInput);return default(FighterInput);}
                var held=pad.Command();command.moveX=held.moveX;command.moveZ=held.moveZ;command.basicAttack=held.basicAttack;command.ultimate=held.ultimate;
                pending[side]=default(FighterInput);return command;
            }
            command.moveX = (Input.GetKey(side == 0 ? KeyCode.D : KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(side == 0 ? KeyCode.A : KeyCode.LeftArrow) ? 1 : 0);
            command.moveZ = (Input.GetKey(side == 0 ? KeyCode.W : KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(side == 0 ? KeyCode.S : KeyCode.DownArrow) ? 1 : 0);
            command.basicAttack = Input.GetKey(side == 0 ? KeyCode.Space : KeyCode.J);
            command.ultimate = Input.GetKey(side == 0 ? KeyCode.R : KeyCode.I);
            pending[side] = default(FighterInput);
            return command;
        }
        void CaptureInput()
        {
            for (int side = 0; side < 2; side++)
            {
                if (!simulation.IsHuman(side)) continue;
                if(UsesPad(side))
                {
                    var pad=gamepads.Find(inputDevice[side]);if(pad!=null){var input=pad.Command();pending[side].skill1|=input.skill1;pending[side].skill2|=input.skill2;pending[side].dodge|=input.dodge;}continue;
                }
                pending[side].skill1 |= Input.GetKeyDown(side == 0 ? KeyCode.Q : KeyCode.K);
                pending[side].skill2 |= Input.GetKeyDown(side == 0 ? KeyCode.E : KeyCode.L);
                pending[side].dodge |= Input.GetKeyDown(side == 0 ? KeyCode.LeftShift : KeyCode.RightShift);
            }
        }
        Vector2 scrollA,scrollB;
        GUIStyle title, heading, label, small, button, textField, scrollButton;
        string savePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindObjectOfType<GameBootstrap>() == null)
                new GameObject("Clash of Cities").AddComponent<GameBootstrap>();
        }
        void Awake()
        {
            Application.targetFrameRate = 60;
            sound=gameObject.AddComponent<GameAudio>();
            gamepads=new LinuxGamepads();
            try
            {
                var asset = Resources.Load<TextAsset>("GameData");
                if (asset == null) throw new InvalidOperationException("Resources/GameData.json fehlt.");
                data = JsonUtility.FromJson<GameData>(asset.text);
                // Run the same validation as a match before exposing menus.
                var a = data.avatars.First(x=>x.cityId==data.cities[0].id);
                var b = data.avatars.First(x=>x.cityId==data.cities[1].id);
                new BattleSimulation(data,new MatchConfig {cityAId=a.cityId,avatarAId=a.id,cityBId=b.cityId,avatarBId=b.id,seed=1,climateEventId=data.environments[0].id,balanceVersion=data.balance.version,mode=selectedMode});
                savePath = Path.Combine(Application.persistentDataPath,"last-match.json");
                LoadSavedResult();
                environment = data.environments.Length;
                arena = new GameObject("Climate arena").AddComponent<ArenaView>();
                arena.Build((float)data.balance.arenaHalfSize,726491,"normal");
                gameCamera = new GameObject("Battle Camera").AddComponent<Camera>();
                gameCamera.tag = "MainCamera";
                gameCamera.clearFlags = CameraClearFlags.SolidColor;
                gameCamera.backgroundColor = new Color(.055f,.08f,.11f);
                gameCamera.fieldOfView = 48;
                gameCamera.nearClipPlane = .1f;
                gameCamera.farClipPlane = 150;
                gameCamera.transform.position = new Vector3(0,22,-26);
                gameCamera.transform.LookAt(Vector3.zero);
                gameCamera.gameObject.AddComponent<AudioListener>();
                battleCamera = gameCamera.gameObject.AddComponent<BattleCamera>();
                #if !UNITY_WEBGL || UNITY_EDITOR
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-locomotion-test") >= 0) StartCoroutine(LocomotionTest());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-selection-test") >= 0) StartCoroutine(SelectionMenuTest());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-audio-test") >= 0) StartCoroutine(AudioTest());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-avatar-test") >= 0) StartCoroutine(AvatarAttachmentTest());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-performance-test") >= 0) StartCoroutine(PerformanceTest());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clash-smoke-test") >= 0)
                    StartCoroutine(SmokeTest());
#endif
            }
            catch (Exception ex) { error = "Projekt konnte nicht gestartet werden: " + ex.Message; Debug.LogException(ex); data=null; }
        }
        void LoadSavedResult()
        {
            try {
#if UNITY_WEBGL && !UNITY_EDITOR
                if(PlayerPrefs.HasKey("last-match"))savedResult=JsonUtility.FromJson<MatchResult>(PlayerPrefs.GetString("last-match"));
#else
                if (File.Exists(savePath)) savedResult = JsonUtility.FromJson<MatchResult>(File.ReadAllText(savePath));
#endif
            }
            catch (Exception ex) { persistenceMessage = "Gespeichertes Ergebnis nicht lesbar: " + ex.Message; }
        }
        void Update()
        {
            UpdateFps();
            UpdateAudio();
            UpdateControllers();
            if(screen==ScreenMode.Selection&&Input.GetKeyDown(KeyCode.Escape))
            {if(seedEditing)seedEditing=false;else if(selectionSheet!=0)selectionSheet=0;else screen=ScreenMode.Menu;}
            if (simulation == null) return;
            if(screen==ScreenMode.Result&&!Input.GetKey(KeyCode.Space)&&!Input.GetKey(KeyCode.J)&&!Input.GetMouseButton(0)&&!gamepads.Devices.Any(p=>p.Command().basicAttack))resultInputReleased=true;
            if (screen == ScreenMode.Battle && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (screen == ScreenMode.Battle && Input.GetKeyDown(KeyCode.F1)) ToggleSkillHelp();
            bool inputAllowed = !paused && Application.isFocused && GUIUtility.hotControl == 0 && !smokeTesting && (!LiveHuman||DeviceProblem(simulation.Config.mode)=="");
            if (screen == ScreenMode.Battle && LiveHuman && inputAllowed) CaptureInput(); else ClearInput();
            if (screen == ScreenMode.Battle && !paused && (!LiveHuman || inputAllowed))
            {
                // Wall time controls presentation speed only. Every simulation step
                // remains fixed; a frame stall never creates a larger damage tick.
                accumulator += Math.Min(Time.unscaledDeltaTime,.25f) * (LiveHuman ? 1 : speed);
                int budget = 200;
                while (accumulator >= data.balance.tickSeconds && !simulation.IsFinished && budget-- > 0)
                {
                    if (LiveHuman) simulation.Step(ReadInput(0),ReadInput(1)); else simulation.Step();
                    foreach (var view in views) view.CaptureTick();
                    accumulator -= data.balance.tickSeconds;
                }
                if (simulation.IsFinished) { knockoutDelay+=Time.unscaledDeltaTime; if(knockoutDelay>=1.25f)FinishMatch(); }
            }
            float alpha = simulation.IsFinished ? 1 : (float)Math.Min(1,accumulator / data.balance.tickSeconds);
            views[0].Render(alpha,views[1].transform.position);
            views[1].Render(alpha,views[0].transform.position);
            if(projectileView != null) projectileView.Render(alpha);
            if(effectsView != null) effectsView.Render(alpha);
        }
        void Replay(MatchResult result)
        {
            if (result == null || result.config == null) { error="Kein lesbarer Replay vorhanden."; return; }
            if (result.config.balanceVersion != data.balance.version)
            { error="Dieses Replay verwendet ältere Kampfregeln. Bitte einen neuen Kampf starten."; return; }
            if (result.config.mode != MatchMode.CpuVsCpu && (result.inputs == null || result.inputs.Length == 0))
            { error="Dem Replay fehlen die aufgezeichneten Spielereingaben. Bitte einen neuen Kampf starten."; return; }
            StartMatch(result.config,result.inputs,true);
        }
        void StartMatch(MatchConfig config, InputFrame[] replayInputs = null, bool replay = false)
        {
            try
            {
                var next = new BattleSimulation(data,config,replay && config.mode != MatchMode.CpuVsCpu ? replayInputs : null);
                if (simulation != null) simulation.Event -= OnBattleEvent;
                if (views != null) foreach (var view in views) Destroy(view.gameObject);
                simulation = next;
                if(arena != null) { arena.gameObject.SetActive(false); Destroy(arena.gameObject); }
                arena = new GameObject("Climate arena " + simulation.Environment.id).AddComponent<ArenaView>();
                arena.Build((float)data.balance.arenaHalfSize,simulation.Config.seed,simulation.Environment.id);
                var floor=arena.gameObject.AddComponent<BoxCollider>();floor.center=new Vector3(0,-.17f,0);floor.size=new Vector3((float)data.balance.arenaHalfSize*2+8,.3f,(float)data.balance.arenaHalfSize*2+8);
                foreach(var obstacle in simulation.Obstacles){var solid=new GameObject("Ragdoll obstacle");solid.transform.SetParent(arena.transform,false);solid.transform.localPosition=new Vector3((float)obstacle.X,(float)obstacle.Height*.5f,(float)obstacle.Z);var collider=solid.AddComponent<BoxCollider>();collider.size=new Vector3((float)obstacle.Radius*1.4f,(float)obstacle.Height,(float)obstacle.Radius*1.4f);}
                if(projectileView != null) Destroy(projectileView.gameObject);
                projectileView = new GameObject("Visible projectiles").AddComponent<ProjectileView>();
                projectileView.Bind(simulation);
                if(effectsView != null) Destroy(effectsView.gameObject);
                effectsView = new GameObject("Skill zones and arena pressure").AddComponent<BattleEffectsView>();
                effectsView.Bind(simulation,data);
                replayViewing=replay; selectedMode=config.mode; ClearInput(); GUIUtility.keyboardControl=0;
                views = new FighterView[2];
                for (int i=0;i<2;i++)
                {
                    views[i] = new GameObject("Fighter " + i).AddComponent<FighterView>();
                    views[i].Initialize(simulation.Fighters[i], i==0 ? cyan : amber);
                    views[i].Configure(data,simulation);
                }
                battleCamera.A=views[0].transform; battleCamera.B=views[1].transform;
                simulation.Event += OnBattleEvent;
                knockoutDelay=0;accumulator=0; paused=false; showSkillHelp=false; speed=1; error=""; screen=ScreenMode.Battle;
            }
            catch (Exception ex) { error="Kampf konnte nicht gestartet werden: " + ex.Message; Debug.LogException(ex); }
        }
        void OnBattleEvent(BattleEvent e) { if(sound!=null)sound.BattleEvent(e); foreach (var view in views) view.OnBattleEvent(e); if(projectileView != null) projectileView.OnBattleEvent(e); if(effectsView != null) effectsView.OnBattleEvent(e); }
        float knockoutDelay;
        float resultReadyAt;
        bool resultInputReleased;
        void FinishMatch()
        {
            resultReadyAt=Time.unscaledTime+.8f;resultInputReleased=false;ClearInput();menuActivate=null;
            screen=ScreenMode.Result;
            if(sound!=null)sound.Cue("victory",-1,.5f);
            savedResult=simulation.Result;
            try
            {
                #if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString("last-match",JsonUtility.ToJson(savedResult));PlayerPrefs.Save();
                persistenceMessage="Ergebnis in diesem Browser gespeichert";
#else
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(savePath,JsonUtility.ToJson(savedResult,true));
                persistenceMessage="Ergebnis lokal gespeichert · last-match.json";
#endif
            }
            catch (Exception ex) { persistenceMessage="Speichern nicht möglich: " + ex.Message; }
        }
        Texture2D UiTexture(Color color)
        {
            var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();return texture;
        }
        void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        void BuildStyles()
        {
            if (title != null) return;
            title = Style(44,FontStyle.Bold);
            heading = Style(24,FontStyle.Bold);
            label = Style(17,FontStyle.Normal);
            small = Style(14,FontStyle.Normal);
            button = new GUIStyle(GUI.skin.button) {fontSize=18, padding=new RectOffset(14,14,9,9)};
            button.normal.background=UiTexture(new Color(.14f,.23f,.28f));
            button.hover.background=UiTexture(new Color(.22f,.36f,.40f));
            button.active.background=UiTexture(new Color(.12f,.43f,.47f));
            button.normal.textColor=new Color(.93f,.95f,.91f);button.hover.textColor=Color.white;button.active.textColor=Color.white;
            button.border=new RectOffset(0,0,0,0);
            scrollButton=new GUIStyle(button){fontSize=14,padding=new RectOffset(0,0,0,0),alignment=TextAnchor.MiddleCenter};
            textField = new GUIStyle(GUI.skin.textField) {fontSize=20,padding=new RectOffset(10,10,8,8)};
            textField.normal.background=UiTexture(new Color(.035f,.07f,.10f));textField.focused.background=UiTexture(new Color(.09f,.18f,.23f));
            textField.normal.textColor=textField.hover.textColor=textField.focused.textColor=Color.white;
        }
        GUIStyle Style(int size, FontStyle weight)
        {
            var style=new GUIStyle(GUI.skin.label) {fontSize=size,fontStyle=weight,wordWrap=true};
            style.normal.textColor=new Color(.92f,.95f,.97f);
            return style;
        }
        void OnGUI()
        {
            BuildStyles();
            BeginMenuNavigation();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            float offsetX=(Screen.width-1280*scale)/2, offsetY=(Screen.height-800*scale)/2;
            if(screen!=ScreenMode.Battle)Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.045f,.068f));
            GUI.matrix=Matrix4x4.TRS(new Vector3(offsetX,offsetY,0),Quaternion.identity,Vector3.one*scale);
            if (data == null) { Panel(new Rect(50,150,1180,450)); GUI.Label(new Rect(80,190,1120,350),error,heading); return; }
            GUI.enabled=!seedEditing&&selectionSheet==0;
            if (screen==ScreenMode.Menu) DrawMenu();
            else if(screen==ScreenMode.Selection) DrawSelection();
            else if(screen==ScreenMode.Battle) DrawBattle();
            else DrawResult();
            GUI.enabled=true;
            if(selectionSheet!=0)DrawSelectionSheet();
            if(seedEditing)DrawSeedEditor();
            if(menuItems.Count>0&&!menuItems.Any(x=>x.Id==menuFocus))menuFocus=menuItems[0].Id;
            if (!string.IsNullOrEmpty(error))
            {
                Panel(new Rect(30,705,1220,65));
                GUI.Label(new Rect(45,711,1190,55),error,small);
            }
            DrawFps();
        }
        void DrawMenu()
        {
            Fill(new Rect(0,0,1280,800),new Color(.025f,.045f,.068f,.97f));
            for(int i=0;i<16;i++)
            {
                float x=660+i*42,h=80+(i*71%190);
                Fill(new Rect(x,590-h,32,h),new Color(.055f,.105f,.135f));
                for(int row=0;row<4;row++)Fill(new Rect(x+9,580-h+row*26,4,8),new Color(.22f,.34f,.35f));
            }
            Fill(new Rect(76,75,48,4),amber);
            GUI.Label(new Rect(76,96,570,30),"ACHT STÄDTE. DEINE ARENA.",small);
            var hero=new GUIStyle(title){fontSize=76};
            GUI.Label(new Rect(70,157,640,196),"CLASH OF\nCITIES",hero);
            GUI.Label(new Rect(77,364,490,72),"Wähle deine Legende. Meistere die Elemente. Entscheide das Duell.",heading);
            var old=GUI.backgroundColor;GUI.backgroundColor=cyan;
            if(NavButton(new Rect(78,478,450,60),"SPIELEN  →",button)){screen=ScreenMode.Selection;error="";}
            GUI.backgroundColor=old;
            GUI.enabled=savedResult!=null&&savedResult.config!=null;
            if(NavButton(new Rect(78,553,450,48),"Letzten Kampf ansehen",button))Replay(savedResult);
            GUI.enabled=true;
            #if UNITY_WEBGL && !UNITY_EDITOR
            if(NavButton(new Rect(78,617,210,44),"Vollbild",button))Screen.fullScreen=!Screen.fullScreen;
#else
            if(NavButton(new Rect(78,617,210,44),"Beenden",button))Application.Quit();
#endif
            Panel(new Rect(715,155,475,370));
            Fill(new Rect(715,155,475,3),amber);
            GUI.Label(new Rect(747,187,405,32),"DAS DUELL DER STÄDTE",small);
            GUI.Label(new Rect(747,242,405,62),"TAKTIK TRIFFT\nTEMPERAMENT",heading);
            GUI.Label(new Rect(747,329,405,105),"Kontrolliere das Feld. Verbinde Skills zu Kombinationen und entfessle deine aufgeladene Ulti.",label);
            Fill(new Rect(747,438,405,1),new Color(.27f,.36f,.39f));
            GUI.Label(new Rect(747,460,405,42),"SOLO  /  LOKALES DUELL  /  CPU",small);
            DrawAudio(new Rect(715,552,475,84));
            Fill(new Rect(78,719,1112,1),new Color(.18f,.28f,.32f));
            GUI.Label(new Rect(78,741,1110,30),"Stick / Steuerkreuz: wählen     ·     A: bestätigen     ·     B: zurück",small);
        }
        void DrawSelection()
        {
            GUI.Label(new Rect(48,35,800,60),"DEIN DUELL",title);
            GUI.Label(new Rect(50,99,800,28),"Zwei Städte. Eine Arena.",small);
            if(NavButton(new Rect(1020,48,210,48),"Alles zufällig",button))RandomizeSelection();
            var modes=new[]{MatchMode.PlayerVsCpu,MatchMode.PlayerVsPlayer,MatchMode.CpuVsCpu};
            string[] names={"Gegen CPU","Lokal zu zweit","CPU gegen CPU"};
            for(int i=0;i<3;i++)
            {
                var old=GUI.backgroundColor;GUI.backgroundColor=selectedMode==modes[i]?cyan:Color.white;
                if(NavButton(new Rect(50+i*398,148,384,48),names[i],button))selectedMode=modes[i];
                GUI.backgroundColor=old;
            }
            DrawSelectionCard(0,new Rect(50,225,568,340),cyan);
            DrawSelectionCard(1,new Rect(662,225,568,340),amber);
            if(NavButton(new Rect(50,594,330,48),"Arena · "+(environment==data.environments.Length?"Zufall":data.environments[environment].name),button))selectionSheet=1;
            if(selectedMode!=MatchMode.PlayerVsPlayer&&NavButton(new Rect(396,594,240,48),"KI · "+DifficultyName(selectedDifficulty),button))selectedDifficulty=selectedDifficulty==CpuDifficulty.Easy?CpuDifficulty.Normal:selectedDifficulty==CpuDifficulty.Normal?CpuDifficulty.Hard:CpuDifficulty.Easy;
            if(NavButton(new Rect(990,594,240,48),"So spielt man",button))selectionSheet=2;
            Fill(new Rect(50,674,1180,1),new Color(.18f,.28f,.32f));
            if(NavButton(new Rect(50,700,180,52),"Zurück",button)){screen=ScreenMode.Menu;error="";}
            var tint=GUI.backgroundColor;GUI.backgroundColor=cyan;
            if(NavButton(new Rect(910,695,320,62),"DUELL STARTEN",button))StartSelectedMatch();
            GUI.backgroundColor=tint;
        }

        void RandomizeSelection()
        {
            cityA=selectionRandom.Next(data.cities.Length);
            cityB=(cityA+1+selectionRandom.Next(data.cities.Length-1))%data.cities.Length;
            avatarA=selectionRandom.Next(Avatars(cityA).Length); avatarB=selectionRandom.Next(Avatars(cityB).Length);
            environment=data.environments.Length;seedText=selectionRandom.Next().ToString();scrollA=scrollB=Vector2.zero;error="";
        }
        AvatarDefinition[] Avatars(int index) { return data.avatars.Where(x=>x.cityId==data.cities[index].id).ToArray(); }
        void DrawCityPanel(Rect rect,ref int cityIndex,ref int avatarIndex,ref Vector2 scroll,Color tint,string side)
        {
            Panel(rect);
            GUI.Label(new Rect(rect.x+16,rect.y+10,rect.width-32,30),side,small);
            var old=GUI.backgroundColor;GUI.backgroundColor=tint;
            if(NavButton(new Rect(rect.x+16,rect.y+43,42,42),"‹",button)) {cityIndex=(cityIndex+data.cities.Length-1)%data.cities.Length;avatarIndex=0;scroll=Vector2.zero;}
            if(NavButton(new Rect(rect.x+65,rect.y+43,rect.width-225,42),data.cities[cityIndex].name+"  ▸",button)) {cityIndex=(cityIndex+1)%data.cities.Length;avatarIndex=0;scroll=Vector2.zero;}
            if(NavButton(new Rect(rect.x+rect.width-152,rect.y+43,136,42),"Zufall ↻",button)) {cityIndex=selectionRandom.Next(data.cities.Length);avatarIndex=0;scroll=Vector2.zero;}
            GUI.backgroundColor=old;
            CityDefinition city=data.cities[cityIndex];
            var avatars=Avatars(cityIndex); avatarIndex=Math.Min(avatarIndex,avatars.Length-1);
            var avatar=avatars[avatarIndex];
            var env=environment==data.environments.Length ? data.environments[0] : data.environments[environment];
            GUI.Label(new Rect(rect.x+16,rect.y+96,rect.width-32,30),"ClimatePower "+ClimateCalculator.Calculate(city,env).ToString("F1")+" / 100"+(environment==data.environments.Length ? " (Vorschau: "+env.name+")" : ""),label);
            string[] names={"Grün","Wasser","Hitzeschutz","Versiegelung ↓","Resilienz","Hitzerisiko ↓"};
            double[] values={city.greenScore,city.waterScore,city.heatProtectionScore,city.sealingScore,city.resilienceScore,city.heatRiskScore};
            for(int i=0;i<6;i++)
            {
                float x=rect.x+16+(i%2)*278,y=rect.y+134+(i/2)*27;
                GUI.Label(new Rect(x,y,175,26),names[i],small);
                GUI.Label(new Rect(x+180,y,72,26),values[i].ToString("F0")+" / 100",small);
            }
            if(NavButton(new Rect(rect.x+16,rect.y+220,rect.width-32,39),avatar.displayName+(avatars.Length>1 ? "  ▸" : ""),button)) avatarIndex=(avatarIndex+1)%avatars.Length;
            // Explicit clipping avoids the native-backed BeginScrollView state that
            // throws ArgumentNullException in the WebGL player. Keep wheel and pad scrolling.
            var viewport=new Rect(rect.x+12,rect.y+267,rect.width-24,rect.height-279);
            float contentHeight=157+85*avatar.abilityIds.Length;
            float maxScroll=Mathf.Max(0,contentHeight-viewport.height);
            if(Event.current.type==EventType.ScrollWheel&&viewport.Contains(Event.current.mousePosition))
            {scroll.y+=Event.current.delta.y*24;Event.current.Use();}
            if(GUI.RepeatButton(new Rect(viewport.xMax-23,viewport.y,23,23),"^",scrollButton))scroll.y-=4;
            if(GUI.RepeatButton(new Rect(viewport.xMax-23,viewport.yMax-23,23,23),"v",scrollButton))scroll.y+=4;
            scroll.y=Mathf.Clamp(scroll.y,0,maxScroll);
            float trackHeight=viewport.height-50;
            Fill(new Rect(viewport.xMax-14,viewport.y+25,4,trackHeight),new Color(.14f,.23f,.28f));
            float thumbHeight=Mathf.Max(12,trackHeight*viewport.height/contentHeight);
            Fill(new Rect(viewport.xMax-14,viewport.y+25+(maxScroll>0?scroll.y/maxScroll:0)*(trackHeight-thumbHeight),4,thumbHeight),tint);
            GUI.BeginGroup(new Rect(viewport.x,viewport.y,viewport.width-28,viewport.height));
            GUI.BeginGroup(new Rect(0,-scroll.y,viewport.width-28,contentHeight));
            GUI.Label(new Rect(4,0,rect.width-56,28),avatar.combatClass+" · Basis: "+DeliveryName(avatar.attackStyle)+" "+avatar.attackRange.ToString("F1")+" m",label);
            GUI.Label(new Rect(4,32,rect.width-56,53),avatar.description,small);
            GUI.Label(new Rect(4,88,rect.width-56,60),avatar.connectionType+": "+avatar.connectionDescription,small);
            int yAbility=157;
            foreach(var id in avatar.abilityIds)
            {
                var ability=data.abilities.First(x=>x.id==id);
                GUI.Label(new Rect(4,yAbility,rect.width-56,82),(ability.isUltimate ? "HAUPTSKILL · " : "")+ability.name+" · "+DeliveryName(ability.delivery)+"\n"+AbilityDetails(ability),small);
                yAbility+=85;
            }
            GUI.EndGroup();
            GUI.EndGroup();
        }
        void DrawBattle()
        {
            DrawCompactBattle();
            if(paused && !showSkillHelp)
            {
                Panel(new Rect(450,260,380,295));GUI.Label(new Rect(475,277,330,40),"PAUSE",heading);
                GUI.enabled=!LiveHuman||DeviceProblem(simulation.Config.mode)=="";
                if(NavButton(new Rect(475,330,330,48),"Fortsetzen",button))TogglePause();GUI.enabled=true;
                if(NavButton(new Rect(475,389,330,48),"Skills & Kartenhilfe",button))ToggleSkillHelp();
                if(NavButton(new Rect(475,448,330,48),"Zurück zur Auswahl",button)){ClearInput();screen=ScreenMode.Selection;}
                GUI.Label(new Rect(475,513,330,30),"A bestätigen · B zurück",small);
                DrawAudio(new Rect(450,565,380,84));
            }
            if(showSkillHelp)DrawSkillHelp();
        }

        static string DeliveryName(string delivery)
        {
            switch(delivery){case "Melee":return "Nahkampf";case "Dash":return "Vorstoß";case "Zone":return "Bodenfeld";case "Self":return "Schutz";default:return "Fernschuss";}
        }
        static string EffectName(string effect)
        {
            switch(effect){case "Stun":return "Betäubung";case "Root":return "Festhalten";case "Haste":return "Tempo";case "Focus":return "Fokus";case "Vulnerable":return "Verwundbarkeit";case "Slow":return "Verlangsamen";case "Knockback":return "Rückstoß";case "Heal":return "Heilung";case "Shield":return "Schild";case "Buff":return "Verstärkung";case "Debuff":return "Schwächung";case "DamageOverTime":return "Schaden über Zeit";case "AreaDamage":return "Flächenschaden";default:return "Schaden";}
        }
        string AbilityDetails(AbilityDefinition ability)
        {
            return EffectName(ability.effectType)+(ability.delivery=="Self" ? "" : " · "+ability.range.ToString("F1")+" m")+" · "+ability.cooldown.ToString("F1")+" s Cooldown · "+ability.energyCost.ToString("F0")+" Energie\n"+ability.description;
        }
        void DrawSkillHelp()
        {
            Panel(new Rect(80,215,1120,398));
            GUI.Label(new Rect(100,229,800,35),showMapHelp ? "STATUS & KARTENEFFEKTE" : "SKILLS & KOMBINATIONEN",heading);
            if(NavButton(new Rect(930,229,245,35),showMapHelp ? "← Skills" : "Status & Karte →",button))showMapHelp=!showMapHelp;
            if(showMapHelp){DrawMapHelp();return;}
            for(int side=0;side<2;side++)
            {
                var fighter=simulation.Fighters[side];
                GUI.Label(new Rect(100+side*550,269,530,28),fighter.City.name+" · Basis: "+DeliveryName(fighter.Avatar.attackStyle),label);
                for(int slot=0;slot<3;slot++)
                {
                    var ability=data.abilities.First(a=>a.id==fighter.Avatar.abilityIds[slot]);
                    string key=SkillKey(side,slot);
                    GUI.Label(new Rect(100+side*550,303+slot*86,530,83),"["+key+"] "+ability.name+" · "+DeliveryName(ability.delivery)+"\n"+AbilityDetails(ability).Replace("Q ",SkillKey(side,0)+" ").Replace("E ",SkillKey(side,1)+" ").Replace("R ",SkillKey(side,2)+" "),small);
                }
            }
            GUI.Label(new Rect(100,566,1070,36),"Kombo: Kontrolle → Fokus → Ulti laden. Controller: A Karte · B zurück · − schließen. F1: schließen.",small);
        }

        string StatusSummary(FighterState fighter)
        {
            var order=new[]{"Stun","Root","Focus","Buff","Haste","Vulnerable","Slow","DamageOverTime"};
            var parts=new System.Collections.Generic.List<string>();
            foreach(var type in order)
            {
                double remaining=fighter.Effects.Where(e=>e.Type==type).Select(e=>e.Remaining).DefaultIfEmpty(0).Max();
                if(remaining>0)parts.Add(EffectName(type)+" "+remaining.ToString("F1")+"s");
            }
            if(fighter.ControlImmunityRemaining>0)parts.Add("Kontrollschutz "+fighter.ControlImmunityRemaining.ToString("F1")+"s");
            return parts.Count==0 ? (SkillKey(fighter.Index,0)+"/"+SkillKey(fighter.Index,1))+" treffen → Buff oder Kontrolle → Ulti kombinieren" : string.Join(" · ",parts.Take(4).ToArray());
        }
        static string TerrainName(string kind)
        {
            switch(kind){case "Regen":return "+ Regeneration";case "Focus":return "Stern: Fokus";case "Haste":return "Pfeile: Tempo";case "Heat":return "Wellen: Hitze";case "Mud":return "Balken: Schlamm";default:return kind;}
        }
        void DrawMapHelp()
        {
            GUI.Label(new Rect(100,277,520,270),"TREFFER & STATUS\n\nBetäubung: stoppt Aktionen und bricht Vorbereitung ab.\nFesthalten: stoppt Bewegung; Angreifen bleibt möglich.\nFokus: schnelleres Vorbereiten und Aufladen.\nTempo: schnellere Bewegung.\nVerstärkung: mehr eigener Schaden.\nVerwundbar: mehr eingehender Schaden.\nNach Stun/Root verhindert Kontrollschutz sofortige Ketten.",label);
            string terrain="KARTE: "+simulation.Environment.name+"\n\n";
            foreach(var zone in simulation.TerrainZones.GroupBy(z=>z.Kind).Select(g=>g.First()))
            {
                string effect=zone.Kind=="Regen" ? "Heilung und Energie im Kreis" : zone.Kind=="Focus" ? "Schneller aufladen im Kreis" : zone.Kind=="Haste" ? "Tempo im Kreis" : zone.Kind=="Heat" ? "Schaden im Kreis" : "Verlangsamung im Kreis";
                terrain+=TerrainName(zone.Kind)+": "+effect+".\n\n";
            }
            terrain+="Positive Felder: vier kurze Randmarken.\nGefahren: acht lange Randmarken.\nEffekte enden nach dem Verlassen; kein Stun durch Mapfelder.";
            GUI.Label(new Rect(650,277,525,280),terrain,label);
            GUI.Label(new Rect(100,566,1070,35),"Felder gelten für beide Seiten. Deckung blockiert Schüsse. Der rote Außenring zwingt später zur Mitte. F1 / − schließt; Controller: A Skills · B zurück.",small);
        }

        string AbilityReadiness(FighterState fighter,FighterState enemy,AbilityDefinition ability,double cooldown)
        {
            if(ability.isUltimate && fighter.IsCharging) return "Lädt "+(fighter.Charge*100).ToString("F0")+"% · Loslassen: auslösen";
            if(cooldown>0) return cooldown.ToString("F1")+"s";
            if(ability.isUltimate && simulation.Elapsed<data.balance.ultimateUnlockSeconds) return "ab "+data.balance.ultimateUnlockSeconds.ToString("F0")+"s";
            if(fighter.Effects.Any(e=>e.Type=="Stun")) return "betäubt";
            if(fighter.CastingAbilityId!=null) return "wirkt";
            if(fighter.RecoveryRemaining>0 || fighter.DodgeRemaining>0) return "Erholung";
            if(fighter.Energy<ability.energyCost) return "Energie fehlt";
            if(ability.effectType=="Heal" && fighter.Health>=fighter.Stats.MaxHealth) return "HP voll";
            if(ability.effectType=="Shield" && fighter.Shield>0) return simulation.IsHuman(fighter.Index) ? "Schild verstärken" : "Schild aktiv";
            if(ability.effectType=="Buff" && fighter.Effects.Any(e=>e.Type=="Buff")) return simulation.IsHuman(fighter.Index) ? "Buff erneuern" : "Buff aktiv";
            bool self=ability.effectType=="Heal" || ability.effectType=="Shield" || ability.effectType=="Buff";
            double x=fighter.X-enemy.X,z=fighter.Z-enemy.Z;
            if(!self && x*x+z*z>ability.range*ability.range) return "zu weit ("+ability.range.ToString("F1")+" m)";
            if(!self && !simulation.HasLineOfSight(fighter.Index,enemy.Index)) return "Deckung im Weg";
            return ability.isUltimate ? "Halten: laden · Lösen: wirken" : "bereit";
        }
        void DrawFighterHud(Rect rect,FighterState fighter,Color color)
        {
            Panel(rect);
            GUI.Label(new Rect(rect.x+15,rect.y+8,rect.width-30,33),fighter.City.name,heading);
            GUI.Label(new Rect(rect.x+15,rect.y+46,rect.width-30,28),fighter.Avatar.displayName,label);
            Meter(new Rect(rect.x+15,rect.y+80,rect.width-30,17),fighter.Health/fighter.Stats.MaxHealth,color);
            GUI.Label(new Rect(rect.x+15,rect.y+102,rect.width-30,24),"HP "+fighter.Health.ToString("F0")+" / "+fighter.Stats.MaxHealth.ToString("F0")+"   Schild "+fighter.Shield.ToString("F0"),small);
            Meter(new Rect(rect.x+15,rect.y+130,rect.width-30,8),fighter.Energy/fighter.Stats.Energy,new Color(.56f,.59f,1));
            GUI.Label(new Rect(rect.x+15,rect.y+144,rect.width-30,26),"Energie "+fighter.Energy.ToString("F0")+" / "+fighter.Stats.Energy.ToString("F0")+"  ·  "+StateName(fighter.State),small);
            if(fighter.IsCharging) Meter(new Rect(rect.x+15,rect.y+173,rect.width-30,5),fighter.Charge,Color.Lerp(color,Color.white,.5f));
        }
        static string StateName(CombatState state)
        {
            switch(state)
            {
                case CombatState.Idle:return "Bereit";case CombatState.MoveToTarget:return "Positionieren";
                case CombatState.BasicAttack:return "Angriff";case CombatState.UseAbility:return "Skill";
                case CombatState.Recover:return "Erholung";case CombatState.Defend:return "Abwehr";
                case CombatState.Retreat:return "Rückzug";case CombatState.Strafe:return "Seitwärtsschritt";
                case CombatState.Dodge:return "Ausweichen";case CombatState.Dead:return "Besiegt";
                default:return state.ToString();
            }
        }
        void DrawResult()
        {
            var result=simulation.Result;
            Panel(new Rect(80,50,1120,695));
            string winner=result.winnerIndex>=0 && result.winnerIndex<2 ? simulation.Fighters[result.winnerIndex].City.name+" gewinnt" : "Unentschieden";
            GUI.Label(new Rect(110,80,1050,65),(result.outcome=="Knockout"?"K. O. · ":"")+winner,title);
            GUI.Label(new Rect(112,153,1040,55),ModeName(result.config.mode)+(result.config.mode==MatchMode.PlayerVsPlayer?"":" · "+DifficultyName(result.config.difficulty))+" · "+simulation.Environment.name+" · "+result.duration.ToString("F2")+" Sekunden · Seed "+result.config.seed,label);
            DrawFighterResult(new Rect(110,220,510,320),result.fighterA,simulation.Fighters[0],cyan);
            DrawFighterResult(new Rect(660,220,510,320),result.fighterB,simulation.Fighters[1],amber);
            GUI.Label(new Rect(112,551,1040,53),persistenceMessage,small);
            bool resultEnabled=Time.unscaledTime>=resultReadyAt&&resultInputReleased;
            GUI.enabled=resultEnabled;
            if(menuNavigation&&menuFocus==null&&resultEnabled)menuFocus=NavigationContext+":"+new Rect(440,622,310,55).ToString();
            if(NavButton(new Rect(110,622,310,55),"Replay ansehen",button))Replay(result);
            if(NavButton(new Rect(440,622,310,55),"Revanche · neuer Seed",button))
            {
                MatchConfig config=JsonUtility.FromJson<MatchConfig>(JsonUtility.ToJson(result.config));
                config.seed=unchecked(config.seed+1);StartMatch(config);
            }
            if(NavButton(new Rect(770,622,200,55),"Städte ändern",button)){screen=ScreenMode.Selection;error="";}
            if(NavButton(new Rect(990,622,180,55),"Menü",button)){screen=ScreenMode.Menu;error="";}
            GUI.enabled=true;
            GUI.Label(new Rect(110,695,1050,30),"Kampf beendet · Revanche startet erst nach deiner Bestätigung.",small);
        }
        void DrawFighterResult(Rect rect,FighterResult result,FighterState fighter,Color tint)
        {
            Panel(rect);
            GUI.Label(new Rect(rect.x+16,rect.y+12,rect.width-32,36),fighter.City.name,heading);
            GUI.Label(new Rect(rect.x+16,rect.y+53,rect.width-32,30),fighter.Avatar.displayName,label);
            GUI.Label(new Rect(rect.x+16,rect.y+96,rect.width-32,120),"Verbleibende HP: "+result.remainingHealth.ToString("F1")+"\nSchaden: "+result.damageDealt.ToString("F1")+" · Heilung: "+result.healingDone.ToString("F1")+"\nBasisangriffe: "+result.basicAttacks+" · Skills: "+result.abilitiesUsed,label);
            if(result.abilityIds!=null && result.abilityCounts!=null)
                for(int i=0;i<Math.Min(result.abilityIds.Length,result.abilityCounts.Length);i++)
                {
                    var ability=data.abilities.FirstOrDefault(x=>x.id==result.abilityIds[i]);
                    GUI.Label(new Rect(rect.x+16,rect.y+214+i*28,rect.width-32,28),(ability==null ? result.abilityIds[i] : ability.name)+": "+result.abilityCounts[i]+"×",small);
                }
        }
        void Panel(Rect rect)
        {
            Fill(new Rect(rect.x+4,rect.y+6,rect.width,rect.height),new Color(0,0,0,.22f));
            Fill(rect,new Color(.045f,.085f,.115f,.96f));
            Fill(new Rect(rect.x,rect.y,rect.width,1),new Color(.28f,.41f,.44f,.7f));
            Fill(new Rect(rect.x,rect.y,3,rect.height),new Color(.18f,.37f,.40f,.7f));
        }
        void Meter(Rect rect,double fraction,Color tint)
        {
            Color old=GUI.color;GUI.color=new Color(.13f,.18f,.22f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=tint;GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01((float)fraction),rect.height),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
