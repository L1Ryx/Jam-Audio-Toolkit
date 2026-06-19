using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Controls how JamAudioPlayer positions playback in the scene.
    /// </summary>
    public enum JamAudioPositionMode
    {
        [InspectorName("None - 2D or UI")]
        None = 0,

        [InspectorName("3D GameObject Position")]
        Position3D = 1
    }
}
