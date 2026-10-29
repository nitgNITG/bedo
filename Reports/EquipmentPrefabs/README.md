# Reusable equipment — acceptance phase 3

Scope: A-03/A-04 equipment prefab audit and safe extraction of independent assemblies. Unity remains **6000.3.21f1**. This pass changes prefab ownership, not experiment behavior or simulation architecture.

## Equipment assets

All new equipment assets are under `Assets/_Project/Prefabs/Equipment`:

| Family | Shared base | Connected variants |
| --- | --- | --- |
| Rheostats | `Rheostats/Rheostat Assembly.prefab` | 150-ohm and 800-ohm authored assemblies |
| Lamps | `Lamps/Lamp On Off Assembly.prefab` | AC halogen, AC LED, DC halogen and DC LED; two lamps of each type |
| Lamp power connectors | `Connectors/Lamp Power Connector Assembly.prefab` | DC halogen and AC LED connector assemblies; extra endpoint components remain variant overrides |

`Lamps/Wind Lamp Outputs.prefab` is a **variant of the existing Outputs FBX** and contains the eight lamp variants as nested prefab instances. Imported source meshes remain linked; nothing was unpacked.

There are 16 new prefab assets: three common bases, 12 equipment variants and one model variant. Existing scene objects were connected in place rather than replaced with new copies. Their scene names, sibling order and component references were preserved. In particular, the older scene objects named `DC Halogen Lamp 1/2` that are actually used as AC LED lamps retain those scene names; their new assets use their actual Free Mode bindings.

## Scene dependencies

Reusable assets cannot contain references to scene-owned graph/controllers/interaction objects. The 12 external bindings on the two rheostat assemblies remain explicit **scene-instance overrides**. Existing internal knob/canvas/terminal/plug/indicator references are kept inside their prefab assemblies. New instances placed in another scene need their external dependencies assigned there.

The original 100-ohm rheostat's guided controls/terminals are spread across experiment-step hierarchies rather than one independent equipment assembly. Those objects, all guided wiring/step roots and the Free Lab coordinator were left in their existing ownership. Reparenting them to manufacture a single prefab would risk their activation, reset and sequencing behavior. Larger physical assemblies already connected to imported model prefabs retain those links. No source FBX hierarchy was split or unpacked, and no default runtime control values were changed.

This establishes reusable prefabs for the suitable independent assemblies; it does not claim that every scene-owned interaction system is now portable. Further separation of cross-cutting scene ownership belongs to the simulation-state/architecture work, with its own regression checks.

## Validation evidence

- `Conversion.txt`: per-assembly preservation and save/reopen verification.
- `Baseline.json` inside `VerificationBaseline.zip`: canonical serialized hashes for all **65,787 objects/components** in the pre-conversion PV Lab scene. References are compared by scene hierarchy/component identity or asset GUID/fileID; only prefab ownership metadata is excluded. Transforms, active states, event targets, mathematical values, wire geometry, UI settings and component fields remain part of the comparison.
- Baseline: **15,494 transforms and 175 pre-existing missing components**. Conversion and reopened-scene comparisons retain these counts and all compared serialized content. Comparisons after reopening wait for the existing UI layout rebuild; no UI-layout fields are excluded to force a pass.
- `RuntimeSmoke.txt`: menu-launch and Guided/Free regression results. These are startup/binding/control smoke checks, not a full manual traversal of every guided wiring instruction or scientific acceptance tests.
- `PrefabAssets.json` and `StaticValidation.txt`: source prefab chains, nested variants, GUID uniqueness and unchanged Main Menu file.
- `PV Lab.before-prefabs.unity.txt` / `Main Menu.before.unity.txt` inside `VerificationBaseline.zip`: rollback copies taken immediately before this phase's scene changes. The archive also retains the initial prefab inventory. The final inventories and prefab-instance ledgers are the `.inventory.txt` / `.prefabs.tsv` reports for both delivery scenes.

One initial unsaved conversion trial was rejected by the full-scene guard because differently named children were treated as unmatched objects. It was discarded before saving. The successful conversion matches the existing children first and restores their original names as overrides, then checks the whole scene. No trial-created duplicate objects remain.

The temporary conversion/request/runtime-test scripts are retained as evidence outside Assets after validation; only the read-only equipment audit command is kept in the Editor folder. Runtime scripts, CSV tables, storyboards and experiment logic were not edited in this phase. Existing missing-script issues from phase 2 remain a separate acceptance limitation.

Final structural audit: Main Menu retains 227 connected prefab roots, and PV Lab now has 577 (565 before conversion). Main Menu is byte-for-byte unchanged. Its remaining missing-component count is two; PV Lab retains its pre-existing 175. EXP1–4 all passed Main Menu → Guided → Free checks with zero runtime errors; each Free Mode test checked all three rheostat ranges and both buttons on each adjustment canvas. This does not certify every guided step or every scientific operating point.

After removing temporary tools, the remaining Editor code compiled successfully with Unity's bundled compiler and refreshed response file (exit 0; `EditorCompiler.log`). Existing unrelated warnings remain. No player/WebGL build was part of this phase.
