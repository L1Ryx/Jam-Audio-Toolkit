# Jam Audio Toolkit

Ridiculously fast Unity audio implementation for game jams.

Jam Audio Toolkit is a lightweight Unity-native package for adding common game
audio behavior quickly, without middleware or third-party dependencies.

## Current Status

This package is at the start of development. The first runtime pieces,
`JamAudioEvent` and `JamAudioPlayer`, are in place with event-based clip
selection, pitch randomization, volume randomization, optional no-immediate-repeat
selection, mixer group routing data, no-code playback from GameObjects, and a
small programmer API for one-line playback. GameObject playback uses a simple
positioning mode so designers can choose non-positional audio or 3D playback,
plus an optional debug view for the generated AudioSource.

## Programmer API

```csharp
JamAudio.Play(footstepEvent);
JamAudio.PlayAtPosition(explosionEvent, transform.position);
```

## Unity Version

Unity 2022.3 LTS or newer is recommended.
