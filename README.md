# Jam Audio Toolkit

Ridiculously fast Unity audio implementation for game jams.

Jam Audio Toolkit is a lightweight Unity-native package for adding common game
audio behavior quickly, without middleware or third-party dependencies.

## Current Status

This package is at the start of development. The first runtime pieces,
`JamSoundEvent` and `JamAudioPlayer`, are in place with event-based clip
selection, pitch multiplier randomization, linear volume randomization, Wwise-style
avoid-repeat clip history, mixer group routing data, no-code playback from
GameObjects, and a small programmer API for one-line playback. GameObject
playback uses a simple positioning mode so designers can choose non-positional
audio or 3D playback, plus an optional debug view for the generated AudioSource.
Music events, scene-driven music playback, persistence, crossfades, inspector
warnings, and editor preview buttons are now in progress.

## Programmer API

```csharp
JamAudio.Play(footstepEvent);
JamAudio.PlayAtPosition(explosionEvent, transform.position);
JamMusicManager.Instance.PlayMusic(levelMusicEvent);
```

For most gameplay scripts, add one or more serialized `JamSoundEvent` fields and
call `JamAudio.Play(...)` directly. Use `JamAudioPlayer.Play()` when a designer
wants Inspector toggles or no-argument UnityEvent wiring, or
`JamAudioPlayer.PlaySound(mySoundEvent)` when one GameObject should play different
sounds through the same player.

## Units

- `Volume (0-1)` uses Unity's linear AudioSource volume scale, not dB.
- `Pitch (x)` uses Unity's AudioSource pitch multiplier. `1` is normal pitch/speed.
- `Fade In (s)` and `Fade Out (s)` are measured in seconds.

## Unity Version

Unity 2022.3 LTS or newer is recommended.
