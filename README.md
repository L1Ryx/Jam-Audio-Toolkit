# Jam Audio Toolkit

Ridiculously fast Unity audio implementation for game jams.

Jam Audio Toolkit is an easy way to put audio into your Unity games without
building a whole audio system first. Create reusable sound and music events,
tweak them in friendly inspectors, then play them with no-code components,
UnityEvents, or one-line C# calls.

No middleware. No required scene setup. No audio expertise needed to start.

## What It Does

- Reusable Sound Event assets for sound effects, UI sounds, ambience, and loops.
- Reusable Music Event assets for tracks, fades, persistence, and crossfades.
- No-code playback components for common Unity callbacks.
- A small `JamAudio` API for code-driven gameplay sounds and music.
- Random clip selection, pitch/volume variation, and recent-clip repeat avoidance.
- Human-readable low-pass and high-pass filters.
- 2D or 3D positioning per Sound Event.
- Optional Audio Mixer Group routing.
- Inspector preview buttons and validation warnings.

## Current Status

Jam Audio Toolkit is in early MVP development. The core runtime, editor
inspectors, preview buttons, no-code players, static API, and music manager are
working. Sample scenes and fuller release polish are still in progress.

## Installation

### Unity Asset Store

Import the package into your Unity project. After import, you can immediately
create Jam Audio assets and call `JamAudio` from scripts.

### Git URL

In Unity, open `Window > Package Manager`, press `+`, choose
`Add package from git URL...`, and enter:

```text
https://github.com/L1Ryx/Jam-Audio-Toolkit.git
```

## How To Use This Package

Jam Audio Toolkit is built around two ideas:

- **Events are reusable audio recipes.** They store clips, volume, pitch,
  randomization, filters, fades, routing, and positioning.
- **Players are optional scene helpers.** Use them when a designer wants
  inspector-driven playback. Skip them when gameplay code can call `JamAudio`
  directly.

### Create A Sound Event

Use Sound Events for sound effects, UI sounds, impacts, footsteps, ambience
loops, and any short reusable sound.

1. Right-click in the Project window.
2. Choose `Create > Jam Audio > Empty Sound Event`.
3. Add one or more clips to `Clip(s)`.
4. Adjust playback settings such as `Volume (%)`, `Pitch (%)`, filters,
   positioning, and randomization.
5. Press `Preview` to quickly audition a clip.

You can also select one or more `AudioClip` assets and choose:

```text
Create > Jam Audio > Sound Event From Selected Clip(s)
```

One clip is completely fine. Multiple clips are useful when you want variation,
like footsteps, impacts, or repeated UI sounds.

### Play A Sound Without Code

Use `JamAudioPlayer` when a GameObject should play a sound from common Unity
callbacks.

1. Add `Jam Audio Player` to a GameObject.
2. Assign a Sound Event.
3. Choose a `Preset`, such as `Play On Start`, `Trigger Enter`, or
   `Collision Enter`.
4. Press Play.

Choose `Code or UnityEvent` if this component should only be called by a
UnityEvent or another script.

### Play A Sound From Code

You do not need a `JamAudioPlayer` for normal gameplay code. Add a serialized
Sound Event field and call `JamAudio.Play(...)`.

```csharp
using JamAudioToolkit;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] private JamSoundEvent pickupSound;

    private void Collect()
    {
        JamAudio.Play(pickupSound);
    }
}
```

For 3D playback on a specific object:

```csharp
JamAudio.Play(hitSound, gameObject);
```

For 3D playback at a world position:

```csharp
JamAudio.PlayAtPosition(explosionSound, transform.position);
```

### Create A Music Event

Use Music Events for level music, menu music, combat music, stingers that should
crossfade, or any longer track controlled by the music manager.

1. Right-click in the Project window.
2. Choose `Create > Jam Audio > Empty Music Event`.
3. Assign a music clip.
4. Set `Volume (%)`, loop behavior, persistence, filters, and transition fades.
5. Press `Preview` to audition the clip.

You can also select one `AudioClip` and choose:

```text
Create > Jam Audio > Music Event From Selected Clip
```

### Play Music Without Code

Use `JamMusicPlayer` when a scene should automatically request music.

1. Add `Jam Music Player` to a GameObject.
2. Assign a Music Event.
3. Choose `Play On Start`.
4. Press Play.

The music manager is created automatically at runtime.

### Play Music From Code

```csharp
using JamAudioToolkit;
using UnityEngine;

public class MusicDebugKeys : MonoBehaviour
{
    [SerializeField] private JamMusicEvent levelMusic;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            JamAudio.PlayMusic(levelMusic);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            JamAudio.PauseMusic();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            JamAudio.ResumeMusic();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            JamAudio.StopMusic();
        }
    }
}
```

## Programmer API

### Sounds

```csharp
JamAudio.Play(soundEvent);
JamAudio.Play(soundEvent, gameObject);
JamAudio.Play(soundEvent, component);
JamAudio.Play(soundEvent, transform);
JamAudio.PlayAtPosition(soundEvent, worldPosition);
```

### Music

```csharp
JamAudio.Play(musicEvent);
JamAudio.PlayMusic(musicEvent);
JamAudio.PauseMusic();
JamAudio.ResumeMusic();
JamAudio.StopMusic();
```

`JamAudio.Play(musicEvent)` and `JamAudio.PlayMusic(musicEvent)` do the same
thing. Use whichever reads better in your script.

## Common Workflows

### UI Button Click

Create a Sound Event with one click clip. Set positioning to `None - 2D or UI`.
In a Unity Button `OnClick`, call `JamAudioPlayer.Play()` or call
`JamAudio.Play(clickSound)` from code.

### Footsteps

Create a Sound Event from several footstep clips. Keep `Randomize Clip` enabled
and set `Recent Clips To Avoid` to `1` or `2`. Call
`JamAudio.Play(footstepSound, gameObject)` from the character or animation
event.

### Explosion Or Impact

Create a Sound Event, set positioning to `3D GameObject Position`, and call:

```csharp
JamAudio.PlayAtPosition(explosionSound, hitPoint);
```

### Level Music

Create a Music Event, assign the track, set fade times, then call:

```csharp
JamAudio.PlayMusic(levelMusic);
```

Changing to another Music Event automatically crossfades.

## Units And Controls

- `Volume (%)` is shown as `0-100%` and converted to Unity's linear `0-1`
  AudioSource volume scale, not dB.
- `Pitch (%)` is shown as `0-300%`. `100%` is normal pitch/speed.
- `Volume Variation (%)` and `Pitch Variation (%)` use `Min (-%)` and
  `Max (+%)` offsets around the base value.
- `Low-Pass Filter (%)` is `0%` for clear/unfiltered audio. Higher values remove
  more high frequencies and sound more muffled.
- `High-Pass Filter (%)` is `0%` for full/unfiltered audio. Higher values remove
  more low frequencies and sound thinner or more radio-like.
- `Fade In (Seconds)` and `Fade Out (Seconds)` are measured in seconds.

## Troubleshooting

### Nothing Plays

- Make sure the Sound Event or Music Event has a clip assigned.
- Runtime playback only works in Play Mode.
- Check that your Audio Mixer Group is not muted.
- For 3D sounds, make sure the listener and source are positioned sensibly.

### Trigger Or Collision Sounds Do Not Play

- Make sure the GameObject has the needed Collider or Collider2D.
- For collision callbacks, Unity usually needs a Rigidbody or Rigidbody2D on one
  of the colliding objects.
- Check the `JamAudioPlayer` preset and callback toggles.

### Do I Need A Jam Audio Player?

No. `JamAudioPlayer` is for no-code Unity callback workflows. For normal
gameplay scripts, use `JamAudio.Play(...)` directly.

## Unity Version

Unity 2022.3 LTS or newer is recommended.
