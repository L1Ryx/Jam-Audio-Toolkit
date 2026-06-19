using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Controls how a JamAudioPlayer-generated AudioSource appears while debugging.
    /// </summary>
    public enum JamAudioSourceDebugView
    {
        [InspectorName("Off")]
        Off = 0,

        [InspectorName("Read Only At Runtime")]
        ReadOnlyAtRuntime = 1,

        [InspectorName("Editable At Runtime")]
        EditableAtRuntime = 2
    }
}
