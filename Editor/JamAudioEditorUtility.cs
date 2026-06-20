using System.Collections.Generic;
using System.IO;
using JamAudioToolkit;
using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    internal static class JamAudioEditorUtility
    {
        public static AudioClip[] GetSelectedAudioClips()
        {
            return Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
        }

        public static AudioClip[] GetDraggedAudioClips()
        {
            List<AudioClip> clips = new List<AudioClip>();

            foreach (Object draggedObject in DragAndDrop.objectReferences)
            {
                if (draggedObject is AudioClip clip)
                {
                    clips.Add(clip);
                }
            }

            return clips.ToArray();
        }

        public static JamSoundEvent CreateSoundEventAsset(AudioClip[] clips = null, string preferredName = null)
        {
            JamSoundEvent soundEvent = ScriptableObject.CreateInstance<JamSoundEvent>();
            soundEvent.clips = clips != null ? (AudioClip[])clips.Clone() : new AudioClip[0];

            string assetName = string.IsNullOrWhiteSpace(preferredName) ? "Empty Sound Event" : preferredName;
            string assetPath = GetUniqueAssetPath(assetName, ".asset");

            AssetDatabase.CreateAsset(soundEvent, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(soundEvent);
            Selection.activeObject = soundEvent;

            return soundEvent;
        }

        public static JamMusicEvent CreateMusicEventAsset(AudioClip clip = null, string preferredName = null)
        {
            JamMusicEvent musicEvent = ScriptableObject.CreateInstance<JamMusicEvent>();
            musicEvent.musicClip = clip;

            string assetName = string.IsNullOrWhiteSpace(preferredName) ? "Empty Music Event" : preferredName;
            string assetPath = GetUniqueAssetPath(assetName, ".asset");

            AssetDatabase.CreateAsset(musicEvent, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(musicEvent);
            Selection.activeObject = musicEvent;

            return musicEvent;
        }

        public static JamAudioPlayer CreateSoundPlayer(JamSoundEvent soundEvent = null)
        {
            string playerName = soundEvent != null ? $"{soundEvent.name} Player" : "Jam Sound Player";
            GameObject gameObject = new GameObject(playerName);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Jam Sound Player");

            JamAudioPlayer player = gameObject.AddComponent<JamAudioPlayer>();
            player.soundEvent = soundEvent;
            EditorUtility.SetDirty(player);

            Selection.activeGameObject = gameObject;
            return player;
        }

        public static JamMusicPlayer CreateMusicPlayer(JamMusicEvent musicEvent = null)
        {
            string playerName = musicEvent != null ? $"{musicEvent.name} Player" : "Jam Music Player";
            GameObject gameObject = new GameObject(playerName);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Jam Music Player");

            JamMusicPlayer player = gameObject.AddComponent<JamMusicPlayer>();
            player.musicEvent = musicEvent;
            EditorUtility.SetDirty(player);

            Selection.activeGameObject = gameObject;
            return player;
        }

        public static void AddSoundPlayerToSelectedObjects()
        {
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                if (gameObject.GetComponent<JamAudioPlayer>() != null)
                {
                    continue;
                }

                Undo.AddComponent<JamAudioPlayer>(gameObject);
            }
        }

        public static void AddMusicPlayerToSelectedObjects()
        {
            foreach (GameObject gameObject in Selection.gameObjects)
            {
                if (gameObject.GetComponent<JamMusicPlayer>() != null)
                {
                    continue;
                }

                Undo.AddComponent<JamMusicPlayer>(gameObject);
            }
        }

        public static string FormatClipLength(AudioClip clip)
        {
            if (clip == null)
            {
                return string.Empty;
            }

            if (clip.length < 1f)
            {
                return $"{clip.length:0.0}s";
            }

            int totalSeconds = Mathf.FloorToInt(clip.length);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return minutes > 0
                ? $"{minutes}:{seconds:00}"
                : $"{seconds}s";
        }

        private static string GetUniqueAssetPath(string assetName, string extension)
        {
            string folderPath = GetSelectedFolderPath();
            string sanitizedName = SanitizeFileName(assetName);
            string combinedPath = $"{folderPath}/{sanitizedName}{extension}";

            return AssetDatabase.GenerateUniqueAssetPath(combinedPath);
        }

        private static string GetSelectedFolderPath()
        {
            string folderPath = "Assets";

            Object activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                return folderPath;
            }

            string selectedPath = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrEmpty(selectedPath) || selectedPath.StartsWith("Packages/"))
            {
                return folderPath;
            }

            if (AssetDatabase.IsValidFolder(selectedPath))
            {
                return selectedPath;
            }

            string parentPath = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrEmpty(parentPath)
                ? folderPath
                : parentPath.Replace('\\', '/');
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '-');
            }

            string sanitizedFileName = fileName.Trim();
            return string.IsNullOrEmpty(sanitizedFileName)
                ? "New Jam Audio Asset"
                : sanitizedFileName;
        }
    }

    internal static class JamAudioMenuItems
    {
        [MenuItem("Assets/Create/Jam Audio/Sound Event From Selected Clip(s)", true)]
        private static bool ValidateCreateSoundEventFromSelectedClips()
        {
            return JamAudioEditorUtility.GetSelectedAudioClips().Length > 0;
        }

        [MenuItem("Assets/Create/Jam Audio/Sound Event From Selected Clip(s)", false, 201)]
        private static void CreateSoundEventFromSelectedClips()
        {
            AudioClip[] clips = JamAudioEditorUtility.GetSelectedAudioClips();
            string assetName = clips.Length == 1 ? $"{clips[0].name} Sound Event" : "New Jam Sound Event";

            JamAudioEditorUtility.CreateSoundEventAsset(clips, assetName);
        }

        [MenuItem("Assets/Create/Jam Audio/Music Event From Selected Clip", true)]
        private static bool ValidateCreateMusicEventFromSelectedClip()
        {
            return JamAudioEditorUtility.GetSelectedAudioClips().Length == 1;
        }

        [MenuItem("Assets/Create/Jam Audio/Music Event From Selected Clip", false, 203)]
        private static void CreateMusicEventFromSelectedClip()
        {
            AudioClip clip = JamAudioEditorUtility.GetSelectedAudioClips()[0];
            JamAudioEditorUtility.CreateMusicEventAsset(clip, $"{clip.name} Music Event");
        }

        [MenuItem("GameObject/Jam Audio/Sound Player", false, 10)]
        private static void CreateSoundPlayer(MenuCommand command)
        {
            JamAudioPlayer player = JamAudioEditorUtility.CreateSoundPlayer();
            GameObjectUtility.SetParentAndAlign(player.gameObject, command.context as GameObject);
        }

        [MenuItem("GameObject/Jam Audio/Music Player", false, 11)]
        private static void CreateMusicPlayer(MenuCommand command)
        {
            JamMusicPlayer player = JamAudioEditorUtility.CreateMusicPlayer();
            GameObjectUtility.SetParentAndAlign(player.gameObject, command.context as GameObject);
        }

        [MenuItem("GameObject/Jam Audio/Add Sound Player To Selected", true)]
        private static bool ValidateAddSoundPlayerToSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("GameObject/Jam Audio/Add Sound Player To Selected", false, 30)]
        private static void AddSoundPlayerToSelected()
        {
            JamAudioEditorUtility.AddSoundPlayerToSelectedObjects();
        }

        [MenuItem("GameObject/Jam Audio/Add Music Player To Selected", true)]
        private static bool ValidateAddMusicPlayerToSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("GameObject/Jam Audio/Add Music Player To Selected", false, 31)]
        private static void AddMusicPlayerToSelected()
        {
            JamAudioEditorUtility.AddMusicPlayerToSelectedObjects();
        }
    }
}
