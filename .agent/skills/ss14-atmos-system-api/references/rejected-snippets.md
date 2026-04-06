# Rejected Snippets (Atmos API)

| Method/Script | Why rejected as a template | Signal |
|---|---|---|
| `GetAdjacentTileMixtures(..., includeBlocked, excite)` | The parameters `includeBlocked` and `excite` are currently not processed as expected | TODO in method |
| `SetSimulatedGrid(...)` | In the current state, the working path through subscribers has not actually been confirmed | TODO that there are no subscribers |
| `GetContainingMixture(...)` (container chain) | No recursive hoisting on parent containers, risk of incorrect environment | TODO about recursive iterate |
| `ReactTile(...)` as a support for the new logic | Old event-wrapper, weak practical value for the new API layer | Base 2022 |
| `Firelock` pressure case on `GetTileMixtures(...)` | Highly specialized and historical code; do not port as a general API pattern | Main block 2022 |
| Legacy transfer API (`Merge/ReleaseGasTo/PumpGasTo/ScrubInto`) as "new style" | The methods are working, but the code base is historical; need a protective layer and tests | Base 2021-2023 |
| Direct mutation map mixture without `SetMap*` | Breaks the expected immutable semantics and refresh map tiles | Map API Contract Violation |
| `RealAtmosTime()` as a strict invariant in new subsystems | Old helper, should not be the only source of the temporary model | Base 2023 |
| Client `AtmosphereSystem.OnMapHandleState` as a reference for the “modern” client-api | Site too old for new standard API behavior | 2023-06-28 |
| Big draw-loop gas overlay as an API example | There are TODO and old blocks; use only spot fresh parts | TODO+ 2022-2023 |
| DeltaPressure internal SIMD TODO block | The current option is working, but there is a batching limitation nearby | TODO about batch operations |
