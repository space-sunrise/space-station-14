# Fresh Pattern Catalog (Audio API)

| API | Destination | Freshness | Recommendation | Comment |
|---|---|---|---|---|
| `ResolveSound(...)` | Resolve `SoundSpecifier` to deterministic `ResolvedSoundSpecifier` | 2025-02-22 | Fresh-Use | Base for replays and uniform selection of tracks from the collection |
| `GetAudioPath(...)` | Get a specific path from resolved specifier | 2025-02-22 | Fresh-Use | Convenient for validation and logs |
| `GetAudioLength(...)` | Audio length for seek/timed logic | 2025-02-22 | Fresh-Use | Use before `SetPlaybackPosition` |
| `PlayGlobal(...)` (resolved/filter) | Non-positional audio for audience | 2025-02-22 | Fresh-Use | Main server broadcast API |
| `PlayEntity(...)` (resolved/filter, uid) | Positional sound from entity | 2025-02-22 | Fresh-Use | Use to snap to a moving source |
| `PlayStatic(...)` (resolved/filter, coords) | Positional sound in coordinates | 2025-02-22 | Fresh-Use | For world-point SFX |
| `PlayPvs(...)` (uid/coords) | Shorthand under PVS | 2025-02-22 | Fresh-Use | Reduces filter boilerplate |
| `PlayGlobal/PlayEntity/PlayStatic(..., recipient)` | Targeted sending to a specific player | 2025-02-22 | Fresh-Use | Useful for private UI/feedback |
| `PlayPredicted(sound, source, user, ...)` | Predicted positional sound from entity | Mixed (client old signature + server old impl) | Mixed-Use | Working contract, but implementation has legacy sections |
| `PlayPredicted(sound, coords, user, ...)` | Predicted positional sound from coords | Mixed | Mixed-Use | Use, but respect legacy signals |
| `PlayLocal(...)` | Local predicted alias | 2024-09-12 | Mixed-Use | Essentially proxying to predicted-flow |
| `SetState(...)` | Lifecycle flow management | 2024-04-17 | Fresh-Use | The correct way is pause/resume/stop |
| `SetPlaybackPosition(...)` | Seek with timeline and pause-offset | 2024-04-17 | Fresh-Use | Working runtime control |
| `SetMapAudio(...)` | Map-wide source mode | 2024-05-01 | Fresh-Use | For music/card level timers |
| `SetGridAudio(...)` | Grid-centered source mode | 2024-05-29 | Fresh-Use | For large grids and FTL sounds |
| `SetGain(...)` | Linear gain control | 2023-11-28 | Legacy-Compat | Use with caution, old area |
| `SetVolume(...)` | dB volume control | 2023-11-28 | Legacy-Compat | Priority via stable setter API, not manually |
| `Stop(...)` | Stopping and deleting an audio entity | 2023-11-27 | Legacy-Compat | Basic API, but historically old |
| `IsPlaying(...)` | Checking the active state | 2024-04-17 | Fresh-Use | Convenient for stateful gameplay |
| `PlayGlobal/PlayEntity/PlayStatic(AudioStream, ...)` | Client stream playback | 2025-02-22 | Fresh-Use | Includes fresh positional-start fixes |
| `LoadStream<T>(...)` | Loading stream into an already created audio entity | Mixed | Mixed-Use | The client path is working, the server path is marked TODO |
| `CreateEffect()/CreateAuxiliary()` | Creating EFX Entities | Shared old / OpenAL runtime fresh | Mixed-Use | The API is stable, but the shared layer is old |
| `SetEffectPreset(...)` | Using reverb preset in effect component | 2023-11-27 | Legacy-Compat | It works, but like a zone of the old shared code |
| `SetEffect(...)` + `SetAuxiliary(...)` | Chain assembly `effect -> aux -> source` | 2023-11-27 / 2025-08-18 runtime | Mixed-Use | Use with EFX Validation |
| `ProcessStreamOverride` | Complete replacement of stream processing | 2025-11-30 | Fresh-Use | Only one subscriber |
| `GetOcclusionOverride` | Custom occlusion | 2025-11-30 | Fresh-Use | Only one subscriber |
| `TryAudioLimit/RemoveAudioLimit` | Limiting competitive identical sounds | 2024-03-14 | Fresh-Use | Important anti-spam mechanism |
