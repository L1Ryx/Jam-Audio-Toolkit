# Jam Audio Toolkit

Ridiculously fast Unity audio implementation for game jams.

Jam Audio Toolkit is a lightweight Unity-native package for adding common game
audio behavior quickly, without middleware or third-party dependencies.

## Current Status

This package is at the start of development. The first runtime pieces,
`JamSoundEvent` and `JamAudioPlayer`, are in place with event-based clip
selection, pitch and volume randomization, readable low-pass/high-pass filter
amounts, Wwise-style avoid-repeat clip history, mixer group routing data,
no-code playback from GameObjects, and a small programmer API for one-line playback. GameObject
playback uses a simple positioning mode so designers can choose non-positional
audio or 3D playback, plus an optional debug view for the generated AudioSource.
Music events, scene-driven music playback, persistence, crossfades, inspector
warnings, and editor preview buttons are now in progress.

## Programmer API

```csharp
JamAudio.Play(footstepEvent);
JamAudio.Play(footstepEvent, gameObject);
JamAudio.PlayAtPosition(explosionEvent, transform.position);
JamAudio.Play(levelMusicEvent);
JamAudio.PauseMusic();
JamAudio.ResumeMusic();
JamAudio.StopMusic();
```

For most gameplay scripts, add one or more serialized `JamSoundEvent` fields and
call `JamAudio.Play(...)` directly. Use `JamAudioPlayer.Play()` when a designer
wants Inspector toggles or no-argument UnityEvent wiring, or
`JamAudioPlayer.PlaySound(mySoundEvent)` when one GameObject should play different
sounds through the same player. For music, use `JamAudio.Play(...)` and
`JamAudio.StopMusic()`. `JamAudio.PauseMusic()` and `JamAudio.ResumeMusic()` use
the music event's fade-out and fade-in durations by default.

Sound events carry their default positioning mode. `JamAudio.Play(soundEvent)`
uses that default. `JamAudio.Play(soundEvent, gameObject)` and
`JamAudio.PlayAtPosition(soundEvent, position)` are quick 3D playback calls for
code-driven sounds.

## Units

- `Volume (%)` is shown as `0-100%` and converted to Unity's linear `0-1` AudioSource volume scale, not dB.
- `Pitch (%)` is shown as `0-300%`. `100%` is normal pitch/speed.
- `Volume Variation (%)` and `Pitch Variation (%)` use `Min (-%)` and `Max (+%)` offsets around the base value.
- `Low-Pass Filter (%)` is `0%` for clear/unfiltered audio. Higher values remove more high frequencies and sound more muffled.
- `High-Pass Filter (%)` is `0%` for full/unfiltered audio. Higher values remove more low frequencies and sound thinner or more radio-like.
- `Fade In (Seconds)` and `Fade Out (Seconds)` are measured in seconds.

## Unity Version

Unity 2022.3 LTS or newer is recommended.
