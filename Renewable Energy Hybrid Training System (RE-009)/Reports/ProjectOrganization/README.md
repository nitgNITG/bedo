# BEDO organization pass

Scope: project organization first, as requested. The original acceptance standard is preserved in `Documentation/Standards`.

| Rule | Result in this pass |
| --- | --- |
| A-01 | Unity and package versions preserved; version exception/migration remains separate |
| A-03 / A-04 | Existing model prefab instances, nested instances and variants preserved; no unpacking; full equipment prefab conversion deferred |
| A-05 | Both delivery scenes grouped by responsibility; persistent/experiment root ownership preserved |
| A-06 | Production assets consolidated under `_Project`; legacy/reference content separated; vendor and Unity-special folders retained |
| A-07 | Explicit editor asset paths and build-scene locations updated; runtime architecture unchanged and documented |
| I-05 / I-06 | Organization evidence and outstanding issues documented; full acceptance evidence remains outside this pass |

## Structural checks

- See `GuidValidation.json`: every tracked asset file's original GUID resolves after the moves.
- See `AssetMoves.tsv`: original/current asset path ledger.
- See `HierarchyValidation.txt`: all original object world transforms and prefab-instance counts preserved in both scenes; missing-script counts unchanged.
- The four PV Lab `StoryboardSteps` serialized components remain unchanged against the pre-organization repository revision.
- Scientific controller, graph and rheostat implementation files remain unchanged; production data file contents are preserved.

These checks cover organization/reference integrity. They do not replace a full Guided/Free E2E test, scientific approval, WebGL build, reset ×20 or performance acceptance run.
