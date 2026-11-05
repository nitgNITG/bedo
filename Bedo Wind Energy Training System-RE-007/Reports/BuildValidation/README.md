# Step 8 — build and performance validation

Status: see `Build.txt` for the actual Windows IL2CPP attempt and target-module check. Build success alone does not certify player behavior or performance.

- Enabled delivery scenes remain Main Menu and PV Lab, in that order.
- The build diagnostic uses the installed Unity 6000.3.21f1. The user explicitly deferred migration to the required 6000.0.60f1.
- Windows build output goes to ignored `Builds/Acceptance/Windows`; binaries are not committed.
- The original scripting backend is restored after the build attempt. No WebGL module or external compiler/toolchain is downloaded or installed.
- `SourceAssetInventory.json` inventories source-file counts, sizes and the largest files. It is not imported texture memory, runtime GPU memory, final build reachability or a performance measurement.

## Outstanding acceptance evidence

1. WebGL Support is absent from this editor installation (only Android and Windows support folders are present). A working WebGL build and browser checks require that module.
2. The agreed baseline/high-end devices and quality presets have not been identified. The standard requires continuous representative ten-minute runs, Windows average >=30 FPS, p99 <=45 ms/no >200 ms hitch on baseline, and WebGL average >=25 FPS. Editor startup checks cannot substitute for these runs.
3. Current WebGL settings need a tested acceptance profile: source has 32 MB initial memory, 2048 MB maximum, exception support enabled and a 960x600 default web viewport. The standard calls for 512–768 MB starting memory, exceptions disabled, approved compression and resolution behavior. These were not changed without a working target to test them.
4. Existing Low/Very Low presets require target-device review; a mip limit is not proof that every imported texture is <=1024 or that post-processing is disabled.
5. Missing original production UI scripts and serialized reference-type warnings from phase 2 remain release blockers until their intended behavior is recovered/reviewed.

Source asset deletion, texture downscaling, geometry reduction or scene pruning was deliberately not performed based only on file size. Such changes need in-player measurement and visual regression evidence to preserve the experiment.

Reproduction: temporarily import `Tools/WindAcceptanceBuild.cs` into an Editor folder, let Unity finish compilation, then write `windows` to `Temp/acceptance-build.flag`. The diagnostic builds only outside Play Mode with saved scenes. Remove the temporary imported script after testing. Original source is retained outside Assets for review.

Actual build result: Windows IL2CPP failed immediately because this installation lacks the selected IL2CPP scripting backend. Windows target support alone is insufficient. Install the Windows IL2CPP build-support component before repeating the acceptance build. ProjectSettings was restored to its exact pre-test content after Unity serialized temporary backend/default fields. No successful acceptance executable was produced.
