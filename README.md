# Jam Audio Toolkit v1.0.0

Ridiculously fast Unity audio implementation for game jams.

**Free for personal and commercial Unity projects.**

Jam Audio Toolkit is an easy way to put audio into your Unity games
without building a whole audio system first. Create reusable sound and music
events then play them with no-code components or one-line script calls.

It is called Jam Audio Toolkit because it is built to be fast enough for game
jams: drag in a clip, make an event, then play it. I made this after spending too many jam hours wiring up basic audio behavior from scratch, like crossfades, filters, pitch/volume randomization, looping, positioning, and scene persistence. 

No Jam Audio scene setup is needed: this works right out of the box. Your Unity
scene still needs one active Audio Listener, usually on the Main Camera, just
like any other Unity audio setup. It builds for all Unity-supported platforms
and is completely Unity-native, with no audio middleware required.

## What It Does

- Reusable Sound Event assets for sound effects, UI sounds, ambience, and loops.
- Reusable Music Event assets for tracks, fades, persistence, and crossfades.
- No-code Jam Audio Player components for common Unity callbacks.
- A small `JamAudio` API for code-driven gameplay sounds and music.
- Music transitions, fades, and crossfades.
- Random clip selection, pitch/volume variation, and recent-clip repeat avoidance.
- Human-readable low-pass and high-pass filters.
- 2D or 3D positioning per Sound Event.
- Optional Audio Mixer Group routing.
- Inspector validation warnings.

## Current Status

The core runtime, editor inspectors, Jam Audio Players, static API, music
manager, and Quick Demo sample are operational. 

More features are on the way! All feedback is welcome. Submit them [here](mailto:shawnguo.dev@gmail.com).

## Optional Companion App

Jam Audio Toolkit Companion is a separate desktop authoring tool for quickly
creating Sound Events and Music Events outside Unity. It can preview audio,
validate event data, and export a `JamAudioLibrary.json` file that this Unity
package imports into normal Jam Audio ScriptableObject assets.

![Jam Audio Toolkit Companion](https://raw.githubusercontent.com/L1Ryx/Jam-Audio-Toolkit-Companion/main/docs/JATC-Screenshot.png)

The Companion app is optional. Jam Audio Toolkit works fully inside Unity
without it.

Download the latest Companion release here:

```text
https://github.com/L1Ryx/Jam-Audio-Toolkit-Companion/releases/latest
```

## Installation

<details>
<summary><strong>Unity Asset Store</strong></summary>

Import the free package into your Unity project. After import, you can
immediately create Jam Audio assets and call `JamAudio` from scripts.

</details>

<details>
<summary><strong>Git URL</strong></summary>

In Unity, open `Window > Package Manager`, press `+`, choose
`Add package from git URL...`, and enter:

```text
https://github.com/L1Ryx/Jam-Audio-Toolkit.git
```

</details>

## How To Use This Package

First, make sure your scene can hear audio:

- Your scene needs one active `AudioListener`. Unity usually adds this to the
  `Main Camera` automatically.
- If Unity warns that there are no audio listeners, choose
  `GameObject > Jam Audio > Ensure Audio Listener On Main Camera`.
- Jam Audio Player and Jam Music Player inspectors also show a fix button when
  the current scene has no active listener.

Jam Audio Toolkit is built around two ideas:

- **Events are reusable audio recipes.** They store clips, volume, pitch,
  randomization, filters, fades, routing, and positioning.
- **Jam Audio Players are optional scene helpers.** Use them when a designer wants
  inspector-driven playback. Skip them when gameplay code can call `JamAudio`
  directly.

<details>
<summary><strong>Create A Sound Event</strong></summary>

Use Sound Events for sound effects, UI sounds, impacts, footsteps, ambience
loops, and any short reusable sound.

1. Right-click in the Project window.
2. Choose `Create > Jam Audio > Empty Sound Event`.
3. Add one or more clips to `Clip(s)`.
4. Adjust playback settings such as `Volume (%)`, `Pitch (%)`, filters,
   positioning, and randomization.
5. Play it through a Jam Audio Player, UnityEvent, or `JamAudio.Play(...)`.

You can also select one or more `AudioClip` assets and choose:

```text
Create > Jam Audio > Sound Event From Selected Clip(s)
```

One clip is completely fine. Multiple clips are useful when you want variation,
like footsteps, impacts, or repeated UI sounds.

</details>

<details>
<summary><strong>Import From Jam Audio Companion</strong></summary>

Jam Audio Companion can export a `JamAudioLibrary.json` file directly into your
Unity project. In Companion, set your Unity project, then click
`Export JSON for Unity`.

In Unity, choose:

```text
Tools > Jam Audio > Import Companion Library
```

If the file exists at `Assets/Jam Audio/Companion/JamAudioLibrary.json`, Jam
Audio imports it automatically. Otherwise, Unity asks you to choose a JSON file.
Most users should not need to browse for the file.

Imported assets are created or updated here:

```text
Assets/Jam Audio/Generated/Sound Events
Assets/Jam Audio/Generated/Music Events
```

The importer turns Companion Sound Events and Music Events into normal
`JamSoundEvent` and `JamMusicEvent` ScriptableObjects.

</details>

<details>
<summary><strong>Play A Sound Without Code</strong></summary>

Use `JamAudioPlayer` when a GameObject should play a sound from common Unity
callbacks.

1. Add `Jam Audio Player` to a GameObject.
2. Assign a Sound Event.
3. Choose a `Preset`, such as `Play On Start`, `Trigger Enter`, or
   `Collision Enter`.
4. Press Play.

Choose `Code or UnityEvent` if this component should only be called by a
UnityEvent or another script.

</details>

<details>
<summary><strong>Play A Sound From Code</strong></summary>

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

</details>

<details>
<summary><strong>Create A Music Event</strong></summary>

Use Music Events for level music, menu music, combat music, stingers that should
crossfade, or any longer track controlled by the music manager.

1. Right-click in the Project window.
2. Choose `Create > Jam Audio > Empty Music Event`.
3. Assign a music clip.
4. Set `Volume (%)`, loop behavior, persistence, filters, and transition fades.
5. Play it through a Jam Music Player, UnityEvent, or `JamAudio.PlayMusic(...)`.

You can also select one `AudioClip` and choose:

```text
Create > Jam Audio > Music Event From Selected Clip
```

</details>

<details>
<summary><strong>Play Music Without Code</strong></summary>

Use `JamMusicPlayer` when a scene should automatically request music.

1. Add `Jam Music Player` to a GameObject.
2. Assign a Music Event.
3. Choose `Play On Start`.
4. Press Play.

The music manager is created automatically at runtime.

</details>

<details>
<summary><strong>Play Music From Code</strong></summary>

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

</details>

## Programmer API

<details>
<summary><strong>Sounds</strong></summary>

```csharp
JamAudio.Play(soundEvent); // soundEvent = the Sound Event asset.
JamAudio.Play(soundEvent, gameObject); // gameObject = play from this GameObject's position.
JamAudio.Play(soundEvent, component); // component = play from this Component's Transform.
JamAudio.Play(soundEvent, transform); // transform = play from this Transform's position.
JamAudio.PlayAtPosition(soundEvent, worldPosition); // worldPosition = exact Vector3 play position.
```

</details>

<details>
<summary><strong>Music</strong></summary>

```csharp
JamAudio.Play(musicEvent); // musicEvent = the Music Event asset.
JamAudio.PlayMusic(musicEvent); // musicEvent = music to play or crossfade to.
JamAudio.PauseMusic(); // Uses the current music's fade-out duration.
JamAudio.ResumeMusic(); // Uses the current music's fade-in duration.
JamAudio.StopMusic(); // Uses the current music's fade-out duration.
```

`JamAudio.Play(musicEvent)` and `JamAudio.PlayMusic(musicEvent)` do the same
thing. Use whichever reads better in your script.

</details>

## Example Things You Can Do

<details>
<summary><strong>UI Button Click</strong></summary>

Create a Sound Event with one click clip. Set positioning to `None - 2D or UI`.
In a Unity Button `OnClick`, call `JamAudioPlayer.Play()` or call
`JamAudio.Play(clickSound)` from code.

</details>

<details>
<summary><strong>Footsteps</strong></summary>

Create a Sound Event from several footstep clips. Keep `Randomize Clip` enabled
and set `Recent Clips To Avoid` to `1` or `2`. Call
`JamAudio.Play(footstepSound, gameObject)` from the character or animation
event.

</details>

<details>
<summary><strong>Explosion Or Impact</strong></summary>

Create a Sound Event, set positioning to `3D GameObject Position`, and call:

```csharp
JamAudio.PlayAtPosition(explosionSound, hitPoint);
```

</details>

<details>
<summary><strong>Level Music</strong></summary>

Create a Music Event, assign the track, set fade times, then call:

```csharp
JamAudio.PlayMusic(levelMusic);
```

Changing to another Music Event automatically crossfades.

</details>

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

## FAQs

<details>
<summary><strong>Why Is Nothing Playing?</strong></summary>

- Make sure the Sound Event or Music Event has a clip assigned.
- Make sure the scene has one active `AudioListener`, usually on the
  `Main Camera`.
- Runtime playback only works in Play Mode.
- Check that your Audio Mixer Group is not muted.
- For 3D sounds, make sure the listener and source are positioned sensibly.

</details>

<details>
<summary><strong>Why Does Unity Say There Are No Audio Listeners?</strong></summary>

Unity needs one active `AudioListener` in the scene before any audio can be
heard. Most new Unity scenes already have one on the `Main Camera`. If yours
does not, choose:

```text
GameObject > Jam Audio > Ensure Audio Listener On Main Camera
```

You can also use the fix button shown in Jam Audio Player and Jam Music Player
inspectors when the current scene has no listener.

</details>

<details>
<summary><strong>Why Aren't Trigger Or Collision Sounds Playing?</strong></summary>

- Make sure the GameObject has the needed Collider or Collider2D.
- For collision callbacks, Unity usually needs a Rigidbody or Rigidbody2D on one
  of the colliding objects.
- Check the `JamAudioPlayer` preset and callback toggles.

</details>

<details>
<summary><strong>Do I Need A Jam Audio Player?</strong></summary>

No. `JamAudioPlayer` is for no-code Unity callback workflows. For normal
gameplay scripts, use `JamAudio.Play(...)` directly.

</details>

<details>
<summary><strong>Can I Use This In Any Project?</strong></summary>

Yes. You can use Jam Audio Toolkit in personal, commercial, jam, student, and
studio projects. The toolkit code is licensed under `0BSD`. Demo music is
credited separately in `Third-Party Notices.txt`.

</details>

## Try The Quick Demo

After installing the package, open `Window > Package Manager`, select
`Jam Audio Toolkit`, expand `Samples`, and import `Quick Demo`.

Open the imported scene at:

```text
Assets/Samples/Jam Audio Toolkit/1.0.0/Quick Demo/Scenes/Quick Demo.unity
```

Press Play, then use the UI buttons to try random SFX, music playback,
crossfades, pause, resume, and stop.

## Special Thanks

Demo music by Patrick Sullivan. Used with permission.

## Unity Version

Unity 2022.3 LTS or newer is recommended.
