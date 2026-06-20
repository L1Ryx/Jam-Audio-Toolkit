using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    /// <summary>
    /// Centralizes editor-only preview playback for Jam Audio Toolkit assets.
    /// </summary>
    public static class JamAudioPreviewUtility
    {
        private static readonly System.Type AudioUtilType =
            typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        /// <summary>
        /// Previews a sound event using its clip selection rules.
        /// </summary>
        public static void PreviewSoundEvent(JamSoundEvent soundEvent)
        {
            if (soundEvent == null)
            {
                Debug.LogWarning("Cannot preview a missing Jam Sound Event.");
                return;
            }

            AudioClip clip = soundEvent.GetClip();
            if (clip == null)
            {
                return;
            }

            PlayPreviewClip(clip, soundEvent.loop);
        }

        /// <summary>
        /// Previews a music event's assigned music clip.
        /// </summary>
        public static void PreviewMusicEvent(JamMusicEvent musicEvent)
        {
            if (musicEvent == null)
            {
                Debug.LogWarning("Cannot preview a missing Jam Music Event.");
                return;
            }

            if (musicEvent.musicClip == null)
            {
                Debug.LogWarning($"{musicEvent.name} has no music clip assigned.", musicEvent);
                return;
            }

            PlayPreviewClip(musicEvent.musicClip, musicEvent.loop);
        }

        /// <summary>
        /// Stops any active editor audio preview clip.
        /// </summary>
        public static void StopPreview()
        {
            StopPreview(true);
        }

        private static void StopPreview(bool logIfUnavailable)
        {
            if (TryInvokeStopMethod("StopAllPreviewClips") || TryInvokeStopMethod("StopAllClips"))
            {
                return;
            }

            if (logIfUnavailable)
            {
                Debug.LogWarning("Unity editor audio preview stop API was not available.");
            }
        }

        private static void PlayPreviewClip(AudioClip clip, bool loop)
        {
            StopPreview(false);

            if (TryInvokePlayMethod("PlayPreviewClip", clip, loop) || TryInvokePlayMethod("PlayClip", clip, loop))
            {
                return;
            }

            Debug.LogWarning("Unity editor audio preview API was not available.");
        }

        private static bool TryInvokePlayMethod(string methodName, AudioClip clip, bool loop)
        {
            if (AudioUtilType == null)
            {
                return false;
            }

            MethodInfo[] methods = AudioUtilType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                object[] arguments = BuildPlayArguments(method, clip, loop);
                if (arguments == null)
                {
                    continue;
                }

                method.Invoke(null, arguments);
                return true;
            }

            return false;
        }

        private static object[] BuildPlayArguments(MethodInfo method, AudioClip clip, bool loop)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            bool hasClipParameter = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                System.Type parameterType = parameters[i].ParameterType;

                if (parameterType == typeof(AudioClip))
                {
                    arguments[i] = clip;
                    hasClipParameter = true;
                }
                else if (parameterType == typeof(int))
                {
                    arguments[i] = 0;
                }
                else if (parameterType == typeof(bool))
                {
                    arguments[i] = loop;
                }
                else if (parameters[i].HasDefaultValue)
                {
                    arguments[i] = parameters[i].DefaultValue;
                }
                else
                {
                    return null;
                }
            }

            return hasClipParameter ? arguments : null;
        }

        private static bool TryInvokeStopMethod(string methodName)
        {
            if (AudioUtilType == null)
            {
                return false;
            }

            MethodInfo method = AudioUtilType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                System.Type.EmptyTypes,
                null);

            if (method == null)
            {
                return false;
            }

            method.Invoke(null, null);
            return true;
        }
    }
}
