# Dependency recovery — acceptance phase 2

Date: 2026-10-22. Scope: restore recoverable missing scripts and their dependencies. Unity remains **6000.3.21f1** at the user's request. This is a partial recovery, not a claim that every acceptance requirement is met.

## Completed

- Recovered 193 original source files (192 absent files plus the matching `LocalizationLanguage` implementation) from the project's preserved `Bedo UI V3.0.2.unitypackage`. Kept original script GUIDs so existing serialized references reconnect.
- Restored the SOAP runtime assembly and required BEDO, Heat UI, Shift UI and legacy layout/data types. The provenance ledger is `RestoredSources.json`.
- Added the official Input System **1.11.2** as a self-contained embedded package from an existing local cache. No machine-specific package path is required. The legacy input backend remains selected (`activeInputHandler: 0`); no other package version or Unity version was changed. [Official installation requirements](https://docs.unity.cn/Packages/com.unity.inputsystem%401.11/manual/Installation.html).
- Fixed the original SOAP runtime assembly's editor-only TMP reference, isolated an old tooltip class from the current class of the same name, and restored the localization table-index methods required by three recovered callers.
- Kept recovered legacy MonoBehaviour handlers disabled in existing production dependency assets, preserving their serialized data. These handlers were previously missing and had no behavior; enabling the older UI flow could compete with the current experiment/UI implementation. Added disabled-component guards to recovered `Awake`/`OnDestroy` methods where necessary. This does **not** complete a SOAP architecture migration.
- Removed the temporary Unity audit helper from Assets.

## Verification

- Missing GUID identities across all Assets: **79 → 19** (60 resolved).
- Unresolved serialized script references across all Assets, including preserved demos: **1,022 → 777** (245 resolved). These are serialized references, not live scene-instance component counts.
- All recovered files exist and their recorded GUIDs match. No duplicate asset GUIDs were found under Assets.
- Compared all 22 modified existing YAML assets against the recovery backup: only legacy component enable flags / prefab enable overrides changed (67 recorded changes). No experiment instructions, step order, event assignments, mathematical values, wire geometry or other serialized content changed in those assets.
- Unity's bundled Roslyn compiler, using the current Unity-generated editor-target response files, successfully compiled **Assembly-CSharp and Assembly-CSharp-Editor** after the localization fix (exit 0 for each). Existing obsolete-API warnings remain. This checks C# compilation; it is not a player build or a runtime test.
- Full editor scene inspection and new Guided/Free runtime smoke tests are **not verified in this pass**. An existing Unity editor process holds the project's locked `Temp/UnityLockfile`; a separate batch editor exits before loading the project. The lock was not removed and the editor was not terminated. Previous organization-phase runtime checks are historical evidence only.

Re-run the read-only serialization/GUID checks from the project root with `python Reports/DependencyAudit/validate_recovery.py`. Results are in `StaticValidation.txt`.

## Still missing: original production UI sources

These five identities have no matching source in the available local packages or searched project/history sources. They were retained, not deleted, rebound by guesswork or replaced with stubs.

| Script GUID | Affected asset / identifying serialized fields |
| --- | --- |
| `cb1dd228f5e08df428ce835804aaab0b` | Both delivery scenes; component has no custom serialized fields. |
| `55369aabfea1a8444a9edaf2005cdb6c` | Both delivery scenes; component has no custom serialized fields. |
| `565bb4d7b087f9f429f072fe9108bda2` | `_Project/Prefabs/UI/Layout/Bedo UI V3.prefab`; `IgnoreFirstStep`, `stepItemTemplate`, `scrollListContent`, `StepManager`. |
| `80f9f154c1d0b2942b47a59acef6a662` | `_Project/UI/BEDO/V3.0/Prefabs/Bedo UI V3 Variant.prefab`; `fontColor`, `backgroundColor`. |
| `b88d7273a378cfc49bac1eb75fb68f92` | `_Project/UI/BEDO/V3.0/Prefabs/Cameras Indicator Ui Canvas.prefab`; `PanelWindow`, `mWindowAnimator`, `IsWindowPanelShowen`. |

Completion requires their original scripts **and matching .meta files**, or a separately reviewed decision to remove/reimplement each specific component after inspecting its intended behavior.

## Preserved legacy/demo issues

The other 14 unresolved identities belong to retained reference/demo content. Many references use the old Unity UI DLL GUID `f5f67c52d1564df4a8936ccd202a3bd8`; a DLL GUID can represent multiple UI component types and cannot safely be globally replaced with one script. Some demo assets are reached indirectly through the legacy catalog, so the conservative production dependency graph also lists them.

One original Cinemachine event source (`3b484db397ebc0642991ce841ace14b9`) is available but only needed by reference content; it was not activated by adding an unrelated Cinemachine dependency. Original external demo scripts are unavailable for the other identities. Full remaining asset locations and serialized fields are in `MissingScriptReferences.after.json`.

## Recovery records

- `MissingScriptReferences.before.json` / `.after.json`: complete serialized-reference inventory.
- `RestoredSources.json`: source provenance and compatibility changes.
- `LegacyHandlerIsolation.json`: component/override changes.
- `BeforeLegacyHandlerIsolation.zip`: pre-recovery versions of the changed YAML assets.
- `ProductionDependencies.json`: conservative scene/catalog dependency traversal at the start of recovery.
- `RecoveryClosure.json`: recovered transitive source selection.

Next: obtain the five missing originals and re-run editor scene/runtime verification. Do not describe phase 2 as fully complete until those issues have been resolved or explicitly reviewed.
