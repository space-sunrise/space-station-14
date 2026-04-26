# Rejected Snippets (Audio Core)

| Zone | What's found | Why not take it as a standard | Signal |
|---|---|---|---|
| `SharedAudioSystem.SetGain/SetVolume/Stop` | Basic runtime operations of the old date | They work, but cannot be used as a “fresh style” without additional ones. checks | Key Lines 2023 |
| `SharedAudioSystem.Effects` (`CreateAuxiliary/CreateEffect/SetAuxiliary/SetEffect`) | Basic shared implementation of effects | The logic is old; fresh EFX context lives in the client OpenAL layer | Base 2023 |
| `SharedAudioSystem.CalculateAudioPosition` | Direct TODO about clamp in another layer | There is an unclosed design debt at the positioning point | TODO (2026-01-19) |
| `SharedAudioSystem.AudioMessage` | TODO about bandwidth-heavy serialization | Don't build a new network audio format on top of the current message-shape | TODO about bandwidth |
| `Server AudioSystem.PlayEntity` (filter branch) | TODO about custom range/PVS override | Direct technical debt area around range filtering | TODO AUDIO |
| `Server AudioSystem.PlayPvs(coordinates)` | TODO about transform mess/optimization | Do not use as reference for new spatial architecture | TODO Transform |
| `Server AudioSystem.LoadStream<T>` | Empty TODO method | Do not consider server stream-loading as a working API | TODO remove |
| `Client AudioSystem.ProcessStream` | Comment about “replicate old behavior for now” | There is a clear temporary solution marker | TODO in method |
| `AudioManager.GetAttenuationGain` | Not all models are implemented, throws `NotImplementedException` | You can't rely on the versatility of the helper | TODO implement |
| `AudioManager.CreateAudioSource` | TODO about indexing by ClydeHandle | Indexing/matching vulnerability | TODO in comments |
| `AudioManager.DisposeAllAudio` | TODO about the need for a stop pass | Shudown behavior should be rechecked when changes | TODO in comments |
| `AmbientSoundSystem` (client sampler) | TODO about incomplete nearest-sound algorithm | Not to be used as a precision prioritization mixing template | TODO in file header |
| `InteractionPopupSystem.SharedInteract` | TODO about attempt-event/paused accumulator + problematic comment | The code is working, but not a pure baseline for the new interaction architecture | TODO + suspicious comment |
| `AudioEffectsManagerSystem.TryAddEffect` | Timer as race-condition workaround | Fragile approach; not to be transferred as a general practice | Explicit commentary on race |
| `Weather` old search sites for the nearest “weather zone” | Parts of the spatial logic are noticeably older than the latest gain edits | Take only fresh pieces of volume control, not the entire algorithm | Mixed row ages |
| Docs: "bikehorn tutorial" | The page is clearly marked as outdated | Useful only as historical onboarding context | `outdated` template marker |
