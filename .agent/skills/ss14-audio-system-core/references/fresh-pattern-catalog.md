# Fresh Pattern Catalog (Audio Core)

| Class/method | Pattern | Why is it useful | Layer | Date by blame | Status |
|---|---|---|---|---|---|
| `SharedAudioSystem.SetPlaybackPosition(...)` | Time shift taking into account pause/start and despawn lifetime | Correct seek without timing out of sync | Shared | 2024-04-17 | Use |
| `SharedAudioSystem.SetMapAudio(...)` | Marking a map sound as global + undetachable | Reliable map-wide sound | Shared | 2024-05-01 | Use |
| `SharedAudioSystem.SetGridAudio(...)` | Auto-tuning grid-position/range + `NoOcclusion` | Consistent sound for large meshes | Shared | 2024-05-29 | Use |
| `SharedAudioSystem.SetState(...)` | Explicit state machine `Playing/Paused/Stopped` | Safe Sound Loop Control | Shared | 2024-04-17 | Use |
| `SharedAudioSystem.SetupAudio(...)` | Single factory audio-entity + timed despawn + variation | Predictable life cycle | Shared | 2025-02-22 | Use |
| `SharedAudioSystem.ResolveSound(...)` + `GetAudioPath(...)` | Determined resolve path/collection | Same playback between clients | Shared | 2025-02-22 | Use |
| `AudioSystem.OnAudioState(...)` | Reusing parameters and seek logic after state update | Client source is kept in sync | Client | 2025-02-26 | Use |
| `AudioSystem.SetupSource(...)` | Offset-aware start, end-buffer guard, primary initialization source | Fewer clips and false starts | Client | 2024-03-16 | Use |
| `AudioSystem.ProcessStreamOverride` | Single-target hook to completely replace stream processing | Extensibility without kernel fork | Client | 2025-11-30 | Use |
| `AudioSystem.GetOcclusionOverride` | Single-target hook for custom occlusion | Custom acoustics of maps/modes | Client | 2025-11-30 | Use |
| `AudioSystem.PlayEntity(AudioStream...)` + immediate `ProcessStream(...)` | Immediate spatial update after start stream | Removes the “crooked first tick” of positional audio | Client | 2025-09-02 | Use |
| `AudioSystem.PlayStatic(AudioStream...)` + immediate `ProcessStream(...)` | Same for static stream | Stable start of static streams | Client | 2025-09-02 | Use |
| `AudioSystem.Limits.TryAudioLimit/RemoveAudioLimit` | Concurrent limit by sound key | Protection against source-budget starvation | Client | 2024-03-14 | Use |
| `BaseAudioSource` NaN gain guard | Explicit protection against NaN in gain | Prevents AL volume/error explosion | Client/OpenAL | 2025-02-26 | Use |
| `BaseAudioSource.SetAuxiliary(...)` + `SetOcclusionEfx(...)` | EFX send + lowpass for occlusion | Real filtering, not just volume | Client/OpenAL | 2025-08-18 | Use |
| `AudioEffect` (`EaxReverb*` bridge) | Full mapping of reverb fields in EFX | Fine-tuning the sound space | Client/OpenAL | 2025-08-18 | Use |
| `AuxiliaryAudio.SetEffect(...)` | Bind/unbind effect on slot | Pure EFX chain management | Client/OpenAL | 2025-08-18 | Use |
| `AudioManager` reload hooks (`/Audio`, `*.ogg/*.wav`) | Hot reload of audio resources | Faster iterations and tests | Client/OpenAL | 2025-03-08 | Use |
| `AudioManager` extension parsing | Normal registration ALC/AL extensions | Correct feature-detection | Client/OpenAL | 2025-06-21 | Use |
| `Server AudioSystem.SetMapAudio/SetGridAudio` + global override | PVS override for map/grid special audio | The right customers hear the sound, regardless of position | Server | 2024-05-01 / 2024-05-29 | Use |
| `Shuttle FTL` (`SetGridAudio`, `SetPlaybackPosition`) | Grid audio in FTL phases + clip-continue on transition | Smooth transitional sound | Server/Gameplay | 2024-05-29 | Use |
| `Salvage countdown` (`SetMapAudio`) | Evacuation music as map audio | Guaranteed map-wide alert | Server/Gameplay | 2024-05-06 | Use |
| `Jukebox` ping-compensated `SetPlaybackPosition` | Network delay compensation in seek | Better synchronization for players | Server/Gameplay | 2024-04-17 | Use |
| `ServerGlobalSoundSystem.PlayGlobalOnStation(...)` | Explicit station audience + PVS | Controlled global broadcast | Server/Gameplay | 2025-02-23 | Use |
| `SharedGasValveSystem.OnActivate(...)` + `PlayPredicted(...WithVariation)` | Predicted Interaction SFX with Variation | Responsive UX and fewer double sounds | Shared/Gameplay | 2025-05-14 | Use |
