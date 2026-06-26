using System;
using System.Collections.Generic;
using System.IO;
using JamAudioToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace JamAudioToolkit.Editor
{
    internal static class JamAudioCompanionImporter
    {
        private const string CompanionFormat = "JamAudioToolkitCompanion";
        private const string ImportDialogTitle = "Jam Audio Companion Import";
        private const string DefaultLibraryAssetPath = "Assets/Jam Audio/Companion/JamAudioLibrary.json";
        private const string GeneratedRootFolder = "Assets/Jam Audio/Generated";
        private const string GeneratedSoundFolder = GeneratedRootFolder + "/Sound Events";
        private const string GeneratedMusicFolder = GeneratedRootFolder + "/Music Events";

        [MenuItem("Tools/Jam Audio/Import Companion Library", false, 120)]
        private static void ImportCompanionLibrary()
        {
            string libraryPath = ResolveLibraryPath();
            if (string.IsNullOrEmpty(libraryPath))
            {
                return;
            }

            ImportCompanionLibrary(libraryPath);
        }

        internal static void ImportCompanionLibrary(string libraryPath)
        {
            if (!TryReadLibrary(libraryPath, out CompanionLibrary library))
            {
                return;
            }

            EnsureGeneratedFolders();

            ImportReport report = new ImportReport();
            AssetDatabase.StartAssetEditing();
            try
            {
                ImportSoundEvents(library.soundEvents ?? Array.Empty<CompanionSoundEvent>(), report);
                ImportMusicEvents(library.musicEvents ?? Array.Empty<CompanionMusicEvent>(), report);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ShowImportReport(report);
        }

        private static void EnsureGeneratedFolders()
        {
            EnsureFolder(GeneratedSoundFolder);
            EnsureFolder(GeneratedMusicFolder);
            AssetDatabase.Refresh();
        }

        private static string ResolveLibraryPath()
        {
            if (File.Exists(ToAbsolutePath(DefaultLibraryAssetPath)))
            {
                return DefaultLibraryAssetPath;
            }

            string selectedPath = EditorUtility.OpenFilePanel(
                "Import Jam Audio Companion Library",
                Application.dataPath,
                "json");

            if (string.IsNullOrEmpty(selectedPath))
            {
                return null;
            }

            string projectRelativePath = ToAssetPathIfPossible(selectedPath);
            return string.IsNullOrEmpty(projectRelativePath) ? selectedPath : projectRelativePath;
        }

        private static bool TryReadLibrary(string libraryPath, out CompanionLibrary library)
        {
            library = null;
            string absoluteLibraryPath = ToAbsolutePath(libraryPath);
            if (!File.Exists(absoluteLibraryPath))
            {
                EditorUtility.DisplayDialog(
                    ImportDialogTitle,
                    $"Could not find companion library:\n{libraryPath}",
                    "OK");
                return false;
            }

            try
            {
                string json = File.ReadAllText(absoluteLibraryPath);
                library = JsonUtility.FromJson<CompanionLibrary>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Jam Audio Companion import failed while reading JSON: {exception.Message}");
                EditorUtility.DisplayDialog(ImportDialogTitle, "Could not read the selected JSON file.", "OK");
                return false;
            }

            if (library != null && string.Equals(library.format, CompanionFormat, StringComparison.Ordinal))
            {
                return true;
            }

            EditorUtility.DisplayDialog(
                ImportDialogTitle,
                "This does not look like a Jam Audio Companion library.",
                "OK");
            return false;
        }

        private static void ShowImportReport(ImportReport report)
        {
            string message = report.BuildSummary();
            Debug.Log($"Jam Audio Companion import complete. {message.Replace("\n", " ")}");
            JamAudioCompanionImportReportWindow.ShowReport(
                report.soundEventsCreated,
                report.soundEventsUpdated,
                report.musicEventsCreated,
                report.musicEventsUpdated,
                report.warningCount,
                GeneratedRootFolder);
        }

        private static void ImportSoundEvents(IReadOnlyList<CompanionSoundEvent> soundEvents, ImportReport report)
        {
            for (int i = 0; i < soundEvents.Count; i++)
            {
                CompanionSoundEvent source = soundEvents[i];
                string displayName = GetDisplayName(source.name, source.id, $"Sound Event {i + 1}");
                string assetPath = GetGeneratedAssetPath(GeneratedSoundFolder, displayName);
                string legacyAssetPath = GetLegacyGeneratedAssetPath(GeneratedSoundFolder, source.id, displayName);

                JamSoundEvent soundEvent = LoadOrCreateAsset<JamSoundEvent>(
                    assetPath,
                    legacyAssetPath,
                    displayName,
                    ref report.soundEventsCreated,
                    ref report.soundEventsUpdated);

                MatchObjectNameToAssetFile(soundEvent);
                soundEvent.clips = LoadAudioClips(source.clips, displayName, report);

                SetBaseAndVariation(
                    source.volumeMin,
                    source.volumeMax,
                    0f,
                    1f,
                    out soundEvent.volume,
                    out soundEvent.volumeRandomRange);

                SetBaseAndVariation(
                    source.pitchMin,
                    source.pitchMax,
                    0f,
                    3f,
                    out soundEvent.pitch,
                    out soundEvent.pitchRandomRange);

                soundEvent.loop = source.loop;
                soundEvent.lowPassFilterAmount = Mathf.Clamp01(source.lowPassPercent);
                soundEvent.highPassFilterAmount = Mathf.Clamp01(source.highPassPercent);
                soundEvent.positionMode = ParsePositionMode(source.spatialMode);
                soundEvent.randomizeClip = source.randomizeClip;
                soundEvent.avoidRepeatingLastClips = Mathf.Max(0, source.recentClipsToAvoid);
                soundEvent.outputMixerGroup = LoadMixerGroup(source.mixerGroup, displayName, report);

                EditorUtility.SetDirty(soundEvent);
            }
        }

        private static void ImportMusicEvents(IReadOnlyList<CompanionMusicEvent> musicEvents, ImportReport report)
        {
            for (int i = 0; i < musicEvents.Count; i++)
            {
                CompanionMusicEvent source = musicEvents[i];
                string displayName = GetDisplayName(source.name, source.id, $"Music Event {i + 1}");
                string assetPath = GetGeneratedAssetPath(GeneratedMusicFolder, displayName);
                string legacyAssetPath = GetLegacyGeneratedAssetPath(GeneratedMusicFolder, source.id, displayName);

                JamMusicEvent musicEvent = LoadOrCreateAsset<JamMusicEvent>(
                    assetPath,
                    legacyAssetPath,
                    displayName,
                    ref report.musicEventsCreated,
                    ref report.musicEventsUpdated);

                MatchObjectNameToAssetFile(musicEvent);
                musicEvent.musicClip = LoadAudioClip(source.track, displayName, report);
                musicEvent.volume = Mathf.Clamp01(source.volume);
                musicEvent.loop = source.loop;
                musicEvent.persistAcrossScenes = source.persistAcrossScenes;
                musicEvent.fadeInDuration = Mathf.Max(0f, source.fadeInSeconds);
                musicEvent.fadeOutDuration = Mathf.Max(0f, source.fadeOutSeconds);
                musicEvent.lowPassFilterAmount = Mathf.Clamp01(source.lowPassPercent);
                musicEvent.highPassFilterAmount = Mathf.Clamp01(source.highPassPercent);
                musicEvent.outputMixerGroup = LoadMixerGroup(source.mixerGroup, displayName, report);

                EditorUtility.SetDirty(musicEvent);
            }
        }

        private static T LoadOrCreateAsset<T>(
            string assetPath,
            string legacyAssetPath,
            string displayName,
            ref int createdCount,
            ref int updatedCount)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                updatedCount++;
                return asset;
            }

            asset = MoveLegacyAssetIfNeeded<T>(legacyAssetPath, assetPath);
            if (asset != null)
            {
                updatedCount++;
                return asset;
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existingAsset != null && !(existingAsset is T))
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            createdCount++;
            return asset;
        }

        private static T MoveLegacyAssetIfNeeded<T>(string legacyAssetPath, string assetPath)
            where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(legacyAssetPath) || legacyAssetPath == assetPath)
            {
                return null;
            }

            T legacyAsset = AssetDatabase.LoadAssetAtPath<T>(legacyAssetPath);
            if (legacyAsset == null)
            {
                return null;
            }

            string targetPath = AssetDatabase.LoadMainAssetAtPath(assetPath) == null
                ? assetPath
                : AssetDatabase.GenerateUniqueAssetPath(assetPath);

            string moveError = AssetDatabase.MoveAsset(legacyAssetPath, targetPath);
            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogWarning($"Jam Audio Companion Import: Could not rename legacy generated asset {legacyAssetPath}: {moveError}");
                return legacyAsset;
            }

            T movedAsset = AssetDatabase.LoadAssetAtPath<T>(targetPath);
            return movedAsset != null ? movedAsset : legacyAsset;
        }

        private static AudioClip[] LoadAudioClips(CompanionClipReference[] clipReferences, string eventName, ImportReport report)
        {
            if (clipReferences == null || clipReferences.Length == 0)
            {
                return Array.Empty<AudioClip>();
            }

            List<AudioClip> clips = new List<AudioClip>(clipReferences.Length);
            foreach (CompanionClipReference clipReference in clipReferences)
            {
                AudioClip clip = LoadAudioClip(clipReference, eventName, report);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            return clips.ToArray();
        }

        private static AudioClip LoadAudioClip(CompanionClipReference clipReference, string eventName, ImportReport report)
        {
            if (clipReference == null || string.IsNullOrWhiteSpace(clipReference.path))
            {
                Warn(report, $"{eventName} has an empty audio clip path.");
                return null;
            }

            string assetPath = NormalizeAssetPath(clipReference.path);
            if (string.IsNullOrEmpty(assetPath))
            {
                Warn(report, $"{eventName} uses a clip outside this Unity project's Assets folder: {clipReference.path}");
                return null;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                Warn(report, $"{eventName} could not find an AudioClip at {assetPath}.");
            }

            return clip;
        }

        private static AudioMixerGroup LoadMixerGroup(string mixerGroupPath, string eventName, ImportReport report)
        {
            if (string.IsNullOrWhiteSpace(mixerGroupPath))
            {
                return null;
            }

            string assetPath = NormalizeAssetPath(mixerGroupPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                Warn(report, $"{eventName} has a mixer group value that is not an Assets path: {mixerGroupPath}");
                return null;
            }

            AudioMixerGroup mixerGroup = AssetDatabase.LoadAssetAtPath<AudioMixerGroup>(assetPath);
            if (mixerGroup == null)
            {
                Warn(report, $"{eventName} could not find an AudioMixerGroup at {assetPath}.");
            }

            return mixerGroup;
        }

        private static JamAudioPositionMode ParsePositionMode(string spatialMode)
        {
            return string.Equals(spatialMode, "3D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(spatialMode, "Position3D", StringComparison.OrdinalIgnoreCase)
                ? JamAudioPositionMode.Position3D
                : JamAudioPositionMode.None;
        }

        private static void SetBaseAndVariation(
            float min,
            float max,
            float clampMin,
            float clampMax,
            out float baseValue,
            out Vector2 variation)
        {
            float orderedMin = Mathf.Clamp(Mathf.Min(min, max), clampMin, clampMax);
            float orderedMax = Mathf.Clamp(Mathf.Max(min, max), clampMin, clampMax);
            baseValue = RoundAudioValue((orderedMin + orderedMax) * 0.5f);
            variation = new Vector2(
                RoundAudioValue(orderedMin - baseValue),
                RoundAudioValue(orderedMax - baseValue));
        }

        private static float RoundAudioValue(float value)
        {
            return Mathf.Round(value * 10000f) / 10000f;
        }

        private static string GetGeneratedAssetPath(string folderPath, string displayName)
        {
            string fileName = SanitizeFileName(displayName);
            return $"{folderPath}/{fileName}.asset";
        }

        private static string GetLegacyGeneratedAssetPath(string folderPath, string id, string displayName)
        {
            string fileName = SanitizeFileName(string.IsNullOrWhiteSpace(id) ? displayName : id);
            return $"{folderPath}/{fileName}.asset";
        }

        private static void MatchObjectNameToAssetFile(UnityEngine.Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                asset.name = fileName;
            }
        }

        private static string GetDisplayName(string name, string id, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id.Trim();
            }

            return fallback;
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) || normalizedPath == "Assets")
            {
                return normalizedPath;
            }

            return ToAssetPathIfPossible(normalizedPath);
        }

        private static string ToAssetPathIfPossible(string path)
        {
            string normalizedPath = path.Replace('\\', '/');
            string normalizedDataPath = Application.dataPath.Replace('\\', '/');

            if (normalizedPath.StartsWith(normalizedDataPath, StringComparison.Ordinal))
            {
                string relativePath = normalizedPath.Substring(normalizedDataPath.Length).TrimStart('/');
                return string.IsNullOrEmpty(relativePath) ? "Assets" : $"Assets/{relativePath}";
            }

            return normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) || normalizedPath == "Assets"
                ? normalizedPath
                : null;
        }

        private static string ToAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return projectRoot == null
                ? path
                : Path.Combine(projectRoot, path);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '-');
            }

            string sanitizedFileName = fileName.Trim();
            return string.IsNullOrEmpty(sanitizedFileName)
                ? "jam_audio_event"
                : sanitizedFileName;
        }

        private static void Warn(ImportReport report, string message)
        {
            report.AddWarning();
            Debug.LogWarning($"Jam Audio Companion Import: {message}");
        }

        [Serializable]
        private sealed class CompanionLibrary
        {
            public string format;
            public int version;
            public CompanionSoundEvent[] soundEvents;
            public CompanionMusicEvent[] musicEvents;
        }

        [Serializable]
        private sealed class CompanionSoundEvent
        {
            public string id;
            public string name;
            public string category;
            public CompanionClipReference[] clips;
            public float volumeMin = 1f;
            public float volumeMax = 1f;
            public float pitchMin = 1f;
            public float pitchMax = 1f;
            public bool randomizeClip = true;
            public int recentClipsToAvoid = 1;
            public bool loop;
            public string spatialMode;
            public float lowPassPercent;
            public float highPassPercent;
            public string mixerGroup;
        }

        [Serializable]
        private sealed class CompanionMusicEvent
        {
            public string id;
            public string name;
            public CompanionClipReference track;
            public float volume = 1f;
            public bool loop = true;
            public bool persistAcrossScenes = true;
            public float fadeInSeconds = 1f;
            public float fadeOutSeconds = 1f;
            public float lowPassPercent;
            public float highPassPercent;
            public string mixerGroup;
        }

        [Serializable]
        private sealed class CompanionClipReference
        {
            public string name;
            public string path;
        }

        private sealed class ImportReport
        {
            public int soundEventsCreated;
            public int soundEventsUpdated;
            public int musicEventsCreated;
            public int musicEventsUpdated;
            public int warningCount;

            public void AddWarning()
            {
                warningCount++;
            }

            public string BuildSummary()
            {
                return
                    $"Imported companion library.\n\n" +
                    $"Sound Events: {soundEventsCreated} created, {soundEventsUpdated} updated\n" +
                    $"Music Events: {musicEventsCreated} created, {musicEventsUpdated} updated\n" +
                    $"Warnings: {warningCount}";
            }
        }
    }

    internal sealed class JamAudioCompanionImportReportWindow : EditorWindow
    {
        private int soundEventsCreated;
        private int soundEventsUpdated;
        private int musicEventsCreated;
        private int musicEventsUpdated;
        private int warningCount;
        private string generatedRootFolder;

        public static void ShowReport(
            int soundEventsCreated,
            int soundEventsUpdated,
            int musicEventsCreated,
            int musicEventsUpdated,
            int warningCount,
            string generatedRootFolder)
        {
            JamAudioCompanionImportReportWindow window = CreateInstance<JamAudioCompanionImportReportWindow>();
            window.titleContent = new GUIContent("Jam Audio Import");
            window.soundEventsCreated = soundEventsCreated;
            window.soundEventsUpdated = soundEventsUpdated;
            window.musicEventsCreated = musicEventsCreated;
            window.musicEventsUpdated = musicEventsUpdated;
            window.warningCount = warningCount;
            window.generatedRootFolder = generatedRootFolder;
            window.minSize = new Vector2(340f, 220f);
            window.maxSize = new Vector2(520f, 320f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField(
                warningCount > 0 ? "Import Complete With Warnings" : "Import Complete",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    warningCount > 0
                        ? $"{warningCount} warning(s). Check the Console for details."
                        : "No warnings.");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Sound Events", $"{soundEventsCreated} created, {soundEventsUpdated} updated");
                EditorGUILayout.LabelField("Music Events", $"{musicEventsCreated} created, {musicEventsUpdated} updated");
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Generated Assets", generatedRootFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Generated Folder"))
                {
                    SelectGeneratedFolder();
                }

                if (GUILayout.Button("Close"))
                {
                    Close();
                }
            }
        }

        private void SelectGeneratedFolder()
        {
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(generatedRootFolder);
            if (folder == null)
            {
                Debug.LogWarning($"Jam Audio Companion Import: Could not select generated folder at {generatedRootFolder}.");
                return;
            }

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
}
