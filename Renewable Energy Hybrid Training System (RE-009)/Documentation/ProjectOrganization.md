# Wind Energy Training System — project organization

This pass applies the project-organization portion of the BEDO acceptance standard. It is not a declaration of full acceptance or a scientific/assessment refactor.

## Open these scenes

- `Assets/_Project/Scenes/Main Menu.unity`: experiment selection and document cards.
- `Assets/_Project/Scenes/PV Lab.unity`: EXP1–4, Guided Mode and the shared Free Mode lab.

The scene filenames, scene GUIDs and build order are preserved. Only their asset-folder locations changed.

## Folder ownership

| Location | Contents |
| --- | --- |
| `Assets/_Project` | Project-owned production assets and editor tools |
| `Assets/_Project/Scenes` | The two delivery scenes |
| `Assets/_Project/Scripts` | Existing runtime framework, simulation and UI scripts |
| `Assets/_Project/Editor` | Project editor/setup utilities; editor-only compilation remains intact |
| `Assets/_Project/Models` | Trainer/environment imports, connector models and generated wire meshes |
| `Assets/_Project/Materials` | Trainer/environment materials and the approved wire materials |
| `Assets/_Project/Textures` | Trainer textures, UI images and render targets |
| `Assets/_Project/Prefabs` | Graph prefabs, interaction indicators and reusable UI/mode prefabs |
| `Assets/_Project/Data/Wind` | Serialized mathematical-model data used by the existing controllers |
| `Assets/_Project/Audio/Interactions` | The authored interaction sounds, formerly ApplicationAssets/Sounds |
| `Assets/_Project/UI/BEDO` | Existing approved BEDO UI asset families |
| `Assets/_Reference` | Other-lab/template/legacy content and original package archives, preserved rather than deleted |
| `Assets/StreamingAssets` | Runtime CSV files: retain this Unity-special folder at its existing location |
| Vendor folders under `Assets` | Pinned third-party packages; retained in place to preserve their tooling, Resources and native-plugin conventions |
| `Documentation` | Organization/architecture notes and the supplied standard |
| `Reports/ProjectOrganization` | Asset move ledger, GUID verification and hierarchy validation |

`_Reference` is a maintenance classification, not a claim that an asset is unused. Existing references to these assets remain valid. Do not delete them without a dependency audit.

## Scene hierarchy

- **Environment**: room model and existing scene/environment content.
- **Equipment**: the trainer assemblies, accessories and retained inactive equipment/reference objects.
- **Views**: existing cameras, authored viewpoints and camera movement hierarchy.
- **Interface**: existing UI roots, retaining their internal structures and prefab relationships.
- **Systems**: scene-local managers, data/audio services and EventSystem.
- **Experiments**: retained at scene root. EXP1–4, their steps, wires, event assignments and Free Lab remain in their existing internal hierarchies.
- **Dont destroy ScriptHolders**: retained at scene root. The manager uses its root for persistent lifetime; putting it under a general group would change which objects survive a reload.
- **Scene Notes**: the former decorative UI separator, renamed without changing behavior.

Grouping uses identity transforms and preserves every original object's world position, rotation, scale and active state. Equipment is not unpacked or duplicated. Existing source-model prefab instances, regular prefabs and variants remain connected.

## Runtime ownership retained

- `InteractionManager`: persistent selection, mode and scene transition ownership.
- `ExperimentSelector` and `StoryboardSteps`: selected experiment and guided procedure.
- Existing experiment controllers/CSV readers: Guided Mode mathematical models and readouts.
- `FreeLabManager` / `FreeLabWindController`: shared physical connections, controls, device selection and Free Mode calculations.
- Existing graph, scrolling-column, monitor-tab and export scripts: their current serialized assignments and behavior remain unchanged.

These components retain their behavioral ownership. The subsequent [SOAP state migration](WindSimulationState.md) gives the wind simulation explicit state assets and reset ownership without changing solvers, experiment order, questions, grading or LMS behavior.

## Moving assets later

Use Unity's Project window or `AssetDatabase.MoveAsset`, retaining each asset's `.meta` GUID. Update explicit asset paths in editor tools and build-scene settings. Keep Unity-special folders (`Editor`, `StreamingAssets`, `Plugins`, `Resources`) semantically intact. Do not move package internals or add assembly definitions without checking dependency boundaries.

The original and current asset paths are recorded in `Reports/ProjectOrganization/AssetMoves.tsv`.

## Deferred work

The subsequent [SOAP phase](WindSimulationState.md) migrates the configured wind simulation's shared values and reset ownership. Full scientific approval, exhaustive flow/reset testing, WebGL delivery, LMS work and performance acceptance remain separate tasks. The [equipment prefab phase](../Reports/EquipmentPrefabs/README.md) converts independent rheostat, lamp and connector assemblies into shared prefab/variant families, retaining source-model links and scene bindings. Complex cross-cutting interaction assemblies remain scene-owned; this is not blanket certification of A-03/A-04 for every legacy object.

The project currently uses Unity **6000.3.21f1**. The supplied standard names **6000.0.60f1**. No Unity/package upgrade or downgrade was performed; full acceptance needs a written version exception or a separately approved compatibility/migration plan.

Before dependency recovery, Unity reported existing missing-script components in the scenes (94 in Main Menu; 261 in PV Lab). Grouping left these counts unchanged. The subsequent [dependency recovery report](../Reports/DependencyAudit/README.md) records 60 restored script identities, remaining unavailable sources and current verification limits. Those historical scene counts should not be treated as a current Unity scene audit. No missing component was silently deleted.
