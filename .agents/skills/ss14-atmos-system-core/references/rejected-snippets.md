# Rejected Snippets (Atmos Core)

| Zone | What's found | Why not take it as a standard | Signal |
|---|---|---|---|
| `SetSimulatedGrid(...)` | Event call without actual subscribers | The method is present, but its practical value has not been confirmed in the current code | TODO about zero subscribers + base 2022 |
| `GetAdjacentTileMixtures(...)` | `includeBlocked` and `excite` are not processed | Parameter behavior does not meet API expectations | TODO in the method itself |
| `GetContainingMixture(...)` (container chain) | No recursive traversal of parent containers | Nested containers may give an incomplete picture of the environment | TODO about recursive iterate |
| `Grid split -> overlay immediate update` | When split, a visual flicker is visible until the next overlay update | There is an unclosed technical hole at the time of split | TODO about force update overlay |
| `GridFixTileVacuum(...)` fragment remove+merge | Inside is a comment about the questionable remove/re-add operation | Unreliable area for pattern copying | TODO with obvious doubt from the author |
| `Firelock` specialized pressure logic | Uses the atmospheric API, but the underlying implementation is older than cutoff | Can be learned as legacy behavior, but not as modern style | Main block 2022 |
| `GasTileOverlay.Draw(...)` most of the loop | The main draw-loop is old, next to TODO by callback | Do not use as a sample of new client code | Base 2022-2023 + TODO |
| `SharedGasTileOverlaySystem` fire-color TODO | Logic needs further stabilization for dirty/tolerance | Risk of incorrect updates based on small temperature fluctuations | TODO about fire color / tolerance |
| `AtmosphereSystem.Gases`: `Merge/ReleaseGasTo/PumpGasTo/ScrubInto` | Key implementations historical | Work, but not used as a new architecture style without isolation/tests | Key Lines 2021-2023 |
| `LINDA/Superconductivity` public methods | Public low-level functions with a very old base | Keep as legacy compatible, not as reference template | 2021-2022 |
| `RealAtmosTime()` | Useful helper, but the underlying implementation is older than cutoff | Use carefully and do not build new invariants on it without checking | Base 2023 |
