# Rejected Snippets (Audio API)

| API/zone | Problem | Why is it dangerous for the new rules | Signal |
|---|---|---|---|
| `LoadStream<T>` (server implementation) | Empty TODO-body | You can't build a server stream API on top of a stub | TODO remove |
| `PlayEntity(..., filter, uid, ...)` (server) | TODO about custom range and extra PVS overrides | Possible range/filter expectations | TODO AUDIO |
| `PlayPvs(..., EntityCoordinates ...)` (server) | TODO about transform mess/optimization | Risk of copying temporary implementation | TODO Transform |
| `CalculateAudioPosition(...)` | TODO about moving clamp to source-layer | Unstable boundary zone between shared and OpenAL source | TODO clamp |
| `AudioMessage` (replay/network payload) | TODO about bandwidth-intensive format | Not suitable as a base for the new net-format audio | TODO bandwidth |
| `ProcessStream(...)` (client) | Direct comment “replicate old behavior for now” | Internal logic is not considered final architecture | TODO in code |
| `GetAttenuationGain(...)` helper | Some modes can throw `NotImplementedException` | helper cannot be considered a universal API | TODO implement |
| `CreateAudioSource(...)` | TODO on buffer indexing | Potentially fragile buffer-id mapping | TODO in code |
| `DisposeAllAudio(...)` | TODO about the correct stop/dispose sequence | Caution needed when refactoring shutdown | TODO in code |
| `SharedAudioSystem.Effects` core methods | The entire area is over the cutoff | Use only as a compat base, not as a “modern style” | Core Code 2023 |
| `SetGain/SetVolume/Stop` | Old lines and historical logic | For new rules, use only with test confirmation | 2023 |
| `AmbientSound` sampler-algorithm | Known incomplete/temporary state of sound selection | Do not copy as reference API approach | TODO in title |
| `Forked AudioEffectsManager subsystem` with timer workaround | Race and lax lifecycle control | Do not carry over as a general way to attach effects | Explicit race-condition comment |
| Docs tutorial "bikehorn" | Explicitly outdated and partially based on old approaches | Historical context only, not API reference | outdated marker |
