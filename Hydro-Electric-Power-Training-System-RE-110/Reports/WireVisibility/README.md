# Camera-aware terminal clearance correction

The click-through implementation was reverted in 95c1cb9. Free Mode again uses its original physical wire click/removal behavior; no runtime script was changed by the geometry correction.

Changed 30 existing cable splines/configurations. The audit resolves the authored camera movements at step start and after the first point click, and checks previously connected wires against the next target. Matching module camera views are used for the four default Free Mode profiles. Cable mouths and the short straight plug-entry sections stay fixed (maximum measured position roundoff 0.000053 world units). Changes are local smooth bends; largest sampled displacement 0.838 world units, with at most 3.34 percent added length. Camera poses, banana/plug positions, experiment steps, mathematical data and object references are unchanged.

Three alternate Free Mode cables now use their authored clearance splines with the existing allocated-plug alignment rather than regenerating the original straight template. Their original/second plug slots remain managed by the existing code; no click-priority logic or new runtime routing code was introduced.

Verification:
- The camera-projected central click region (3 px radius at 1920x1080) improved from 21 wire obstructions to zero in the audited step/module views. This checks actual wire width and relative depth, without raycast bypassing.
- Full camera renders before/after were inspected for lamp, rectifier, rheostat, battery, charger and inverter views. Fixed plug mouths can still project close to adjacent pin edges; they were not moved to hide that relationship, and are excluded only at their own physical joints.
- 629 relevant coexisting cable pairs were sampled at approximately 0.06 world-unit spacing. Two candidate new contacts were rejected and separated locally. Final comparison detects zero new tube contacts. Existing contacts/shared-pin regions were distinguished from new collisions; this is not an exhaustive triangle collision guarantee for every arbitrary Free Mode wiring combination.
- Main Menu -> Guided -> Free tests for EXP1-4 passed with zero runtime errors. Cable render/click mesh rebuilding, raycasts, normal removal and rheostat control bindings passed. All 176 mathematical samples match the prior baseline exactly.
- All existing scene object IDs and all recorded camera/terminal positions are retained. Existing user scene edits are preserved separately from the geometry commit.

Geometry.json records changes and click-region results; Clearance.json records the pair check; ChangedCables.txt gives hierarchy names. One-shot diagnostic tools are archived under Tools and are not compiled into the project.
