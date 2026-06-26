# Jam Audio Toolkit Internals

This is a maintainer note for how the Unity package is wired together. The
public README should stay user-facing; this file is for remembering the system
shape when changing runtime, editor, or Companion import code.

## Package Shape

- `Runtime/` contains everything that ships into player builds.
- `Editor/` contains inspectors, create menus, validation helpers, and the
  Companion JSON importer. Nothing in `Runtime/` should reference `UnityEditor`.
- `Samples~/Quick Demo/` is the importable demo scene and example assets.
- `Documentation/` contains offline documentation for Asset Store submission.

The package has two assemblies:

- `JamAudioToolkit.Runtime` for runtime code.
- `JamAudioToolkit.Editor` for editor-only tooling.

## Runtime Model

Jam Audio has two asset types:

- `JamSoundEvent` is a reusable recipe for SFX, UI sounds, ambience, and short
  loops. It owns clip selection, volume, pitch, randomization, filters,
  positioning, and mixer routing.
- `JamMusicEvent` is a reusable recipe for one music track. It owns the clip,
  volume, loop flag, scene persistence, filters, fades, and mixer routing.

These assets intentionally use public serialized fields. The friendly
inspectors and `InspectorName` attributes make them readable for designers, and
the runtime getter methods clamp values before playback.

## Sound Playback Flow

Code-driven SFX usually enters through:

```csharp
JamAudio.Play(soundEvent);
JamAudio.Play(soundEvent, gameObject);
JamAudio.PlayAtPosition(soundEvent, position);
```

The flow is:

1. `JamAudio` validates the event and asks it for a clip.
2. `JamSoundEvent.GetClip()` applies random clip selection and recent-clip
   avoidance.
3. `JamAudio` gets or creates the hidden `JamAudioSourcePool`.
4. The pool configures an `AudioSource` with volume, pitch, spatial blend,
   mixer group, and filters.
5. Non-looping sources return to the pool after playback. Looping sources return
   when stopped.

No scene setup is required for this pool. It is created lazily as
`Jam Audio Runtime` and marked `DontDestroyOnLoad`.

## Jam Audio Player Flow

`JamAudioPlayer` is the no-code helper for scene objects. It is useful for
designer-owned callbacks like `Awake`, `Start`, `OnEnable`, trigger enter/exit,
and collision enter.

Unlike `JamAudio.Play`, the player uses an `AudioSource` on its own GameObject.
It lazily creates that source at runtime and hides it by default. The debug
dropdown controls whether the generated source is hidden, read-only, or fully
visible at runtime.

Use `JamAudio.Play` for normal gameplay scripts. Use `JamAudioPlayer` when the
trigger itself should live in the Inspector.

## Music Playback Flow

Music enters through:

```csharp
JamAudio.PlayMusic(musicEvent);
JamAudio.PauseMusic();
JamAudio.ResumeMusic();
JamAudio.StopMusic();
```

`JamAudio.Play(musicEvent)` is syntactic sugar for `PlayMusic`.

The flow is:

1. `JamAudio` asks `JamMusicManager.GetOrCreate()` for the runtime manager.
2. The manager creates two child `AudioSource` objects.
3. When a new music event plays, the inactive source becomes the incoming track.
4. The manager crossfades incoming and outgoing sources using unscaled time.
5. Pause, resume, and stop reuse the current event fade settings unless an
   override duration is supplied by code.

The manager is created lazily as `Jam Music Manager`, marked
`DontDestroyOnLoad`, and removes duplicates during `Awake`.

## Filters

`JamAudioFilterUtility` is the single place that translates human-readable
filter percentages into Unity filter components.

- Low-pass: `0%` means clear, `100%` means strongly muffled.
- High-pass: `0%` means full, `100%` means thin/radio-like.

The utility adds `AudioLowPassFilter` and `AudioHighPassFilter` components only
when needed, disables them when the amount is effectively zero, and uses
logarithmic cutoff interpolation so the control feels more natural.

## Editor Layer

The custom inspectors are intentionally user-facing:

- `JamSoundEventEditor` draws clip management, playback, filters, positioning,
  randomization, and advanced routing.
- `JamMusicEventEditor` draws clip assignment, playback, filters, transitions,
  and advanced routing.
- `JamAudioPlayerEditor` and `JamMusicPlayerEditor` make scene helpers easier to
  configure and surface Audio Listener warnings.
- `JamMinMaxDrawer` draws percent min/max ranges as designer-friendly sliders.
- `JamAudioEditorUtility` owns shared editor actions like creating assets,
  adding players, formatting clip lengths, and ensuring an Audio Listener.

Keep validation warnings helpful and non-blocking. The package should guide
newer Unity users without making simple audio setup feel scary.

## Companion Importer

`Editor/Importers/JamAudioCompanionImporter.cs` imports JSON exported by Jam
Audio Toolkit Companion.

Menu path:

```text
Tools > Jam Audio > Import Companion Library
```

Default input path:

```text
Assets/Jam Audio/Companion/JamAudioLibrary.json
```

When the Companion app has a Unity project selected, its export button writes to
that default path so Unity can import without a file picker.

Generated output paths:

```text
Assets/Jam Audio/Generated/Sound Events
Assets/Jam Audio/Generated/Music Events
```

The importer creates or updates normal `JamSoundEvent` and `JamMusicEvent`
assets. Re-importing the same Companion event updates the generated asset at the
same path, so users should treat generated assets as owned by the Companion
library.

Companion stores sound volume and pitch as min/max ranges. Unity Sound Events
store a base value plus a variation range, so the importer converts like this:

```text
base = midpoint(min, max)
variation = (min - base, max - base)
```

Clip and mixer paths must resolve inside the current Unity project's `Assets`
folder. Missing paths become warnings; the rest of the import continues.

## Change Guidelines

- Keep runtime code independent from editor-only APIs.
- Preserve serialized field names unless a `FormerlySerializedAs` migration is
  added.
- Keep designer-facing labels explicit about units, such as `%` and `Seconds`.
- Let `JamAudio` stay the beginner-friendly code API. Avoid forcing users to
  touch managers or pools directly.
- Prefer warnings over hard failures when an asset can still be imported or
  played partially.
- After touching runtime or editor code, compile through a Unity test project.
