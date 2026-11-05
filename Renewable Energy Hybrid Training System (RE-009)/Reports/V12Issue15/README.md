# V12 issue 15: actual cable contact

Audited the Guided spline routes in 3D: 651 coexisting cable pairs, no contacts. Guided splines stay unchanged. Camera-projected crossings with clear depth separation are left alone.

FreeLabWire now starts each new Free connection from its authored route and aligns allocated plugs as before. Only a cable actually contacting another connected cable gets a smooth local detour. Plug transforms and the first/last 0.12 world units of each cable remain fixed. If an older cable obstructs the new fixed entry, its body detours instead. Active click meshes are rebuilt to match. Reconnection starts from the original route. No continuous Update work is added; a spatial grid limits the calculation to nearby segments.

Validation:
- 22,260 potential Free slot pairs audited. 2,262 collision-candidate cases tested in both connection orders through actual FreeLabWire.Configure calls and allocated plug transforms. Independent dense sampling of the exported curves finds zero tube contacts. Initially clear cases remain unchanged.
- Eight randomized sets of 46-48 simultaneous real FreeLabManager connections: zero tube contacts.
- Main Menu to Guided to Free for EXP1-4: zero runtime errors. Speed unlock, 200 RPM increments, maximum once, graph/model references, rheostat controls, cable activation/rebuild, click-mesh raycasts and removal checked.
- All 176 scientific samples match the previous baseline exactly. Missing EXP4 Guided analyzer bindings are the pre-existing check-only finding recorded under V12Issue7.

GeometrySummary.json records counts and timing. Temporary Unity runners are archived under Tools, outside Assets. Output paths use Temp/V12BatchNext. The original scene wire audit tool remains under Reports/WireVisibility/Tools.
