using System;
using System.IO;
using ClashOfCities.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ClashOfCities.Editor
{
    public static class BuildTools
    {
        static void ValidateAudio()
        {
            var localTools=Path.GetFullPath(".tools/bin");
            if(File.Exists(Path.Combine(localTools,"ffmpeg")))
                Environment.SetEnvironmentVariable("PATH",localTools+Path.PathSeparator+Environment.GetEnvironmentVariable("PATH"));
            var paths=Directory.GetFiles("Assets/ClashOfCities/Resources/Audio","*.wav");
            if(paths.Length!=15)throw new InvalidOperationException("Expected 15 game audio assets.");
            foreach(var path in paths)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(clip==null||clip.samples<=0)throw new InvalidOperationException("Audio import failed: "+path+". WebGL requires ffmpeg on PATH or in .tools/bin.");
            }
            Debug.Log("Clash audio: all 15 clips imported for active build target.");
        }
        [MenuItem("Clash of Cities/Validate Local Data")]
        public static void ValidateData()
        {
            var asset=Resources.Load<TextAsset>("GameData");
            if(asset==null) throw new InvalidOperationException("Missing Resources/GameData.json");
            DataValidator.Validate(JsonUtility.FromJson<GameData>(asset.text));
            Debug.Log("Clash of Cities: local demo data valid.");
        }
        [MenuItem("Clash of Cities/Play Game")]
        public static void PlayGame()
        {
            ValidateData();
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            EditorApplication.isPlaying = true;
        }
        [MenuItem("Clash of Cities/Validate Simulation")]
        public static void ValidateSimulation()
        {
            ValidateData();
            var data=JsonUtility.FromJson<GameData>(Resources.Load<TextAsset>("GameData").text);
            int count=0;
            foreach(var city in data.cities)
            foreach(var environment in data.environments)
            foreach(CpuDifficulty difficulty in Enum.GetValues(typeof(CpuDifficulty)))
            {
                var config=new MatchConfig {
                    cityAId=city.id, avatarAId=Array.Find(data.avatars,a=>a.cityId==city.id).id,
                    cityBId=data.cities[1].id, avatarBId=Array.Find(data.avatars,a=>a.cityId==data.cities[1].id).id,
                    climateEventId=environment.id, difficulty=difficulty, seed=726491, balanceVersion=data.balance.version
                };
                var first=new BattleSimulation(data,config).RunToEnd();
                var replay=new BattleSimulation(data,first.config).RunToEnd();
                if(JsonUtility.ToJson(first)!=JsonUtility.ToJson(replay))
                    throw new InvalidOperationException("Unity replay mismatch: "+city.id+" / "+environment.id);
                count++;
            }
            Debug.Log("Clash of Cities: "+count+" Unity simulation/replay pairs passed.");
        }
        [MenuItem("Clash of Cities/Build Browser Player")]
        public static void BuildWeb()
        {
            ValidateSimulation();
            ValidateAudio();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("WebGL Build Support fehlt.");
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.template="PROJECT:Clash";
            // The scene bootstraps runtime-created UI and primitive physics components.
            // Native engine stripping cannot infer all of these dependencies.
            PlayerSettings.stripEngineCode=false;
            PlayerSettings.runInBackground=false;
            string output=Environment.GetEnvironmentVariable("CLASH_WEB_OUTPUT")??"Builds/Web";
            var scenes=Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Array.ConvertAll(scenes,s=>s.path),locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Browser build failed: "+report.summary.result);
            Debug.Log("Built browser player: "+output);
        }
        [MenuItem("Clash of Cities/Build Linux Player")]
        public static void BuildLinux() => BuildNative(BuildTarget.StandaloneLinux64,"Linux/ClashOfCities.x86_64");
        [MenuItem("Clash of Cities/Build Windows Player")]
        public static void BuildWindows() => BuildNative(BuildTarget.StandaloneWindows64,"Windows/ClashOfCities.exe");
        [MenuItem("Clash of Cities/Build macOS Player")]
        public static void BuildMacOS() => BuildNative(BuildTarget.StandaloneOSX,"macOS/ClashOfCities.app");
        static void BuildNative(BuildTarget target,string relativeOutput)
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target))
                throw new InvalidOperationException("Unity Build Support fehlt: "+target);
            ValidateSimulation();
            ValidateAudio();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
            // Universal macOS player: Intel and Apple Silicon.
            if(target==BuildTarget.StandaloneOSX)PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone,2);
            var scenes=Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled);
            if(scenes.Length==0) throw new InvalidOperationException("No enabled scene in Build Settings.");
            string output=Environment.GetEnvironmentVariable("CLASH_BUILD_OUTPUT");
            if(string.IsNullOrEmpty(output)) output="Builds/"+relativeOutput;
            var options=new BuildPlayerOptions {
                scenes=Array.ConvertAll(scenes,s=>s.path),
                locationPathName=output,
                target=target,
                options=BuildOptions.None
            };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(options);
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Player build failed: "+report.summary.result);
            Debug.Log("Built offline player: "+options.locationPathName);
        }
    }
}
