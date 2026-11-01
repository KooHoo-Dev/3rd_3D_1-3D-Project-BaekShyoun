Stage 1 optimization

Applied directly to 03. Stage 1.unity.
Small decor merged into 64 spatial cells (12m) with shared materials.
Original prefab assets, materials, Terrain, colliders and project settings are unchanged.
Original small-decor renderers remain disabled in the scene as editable source data.
If you move/edit these source objects, the combined meshes must be rebuilt to reflect the edits.

Backups:
Backups/Stage1-live-before.unity is the full scene immediately before optimization, including any unsaved changes.
Backups/Stage1-disk-before.unity.bytes preserves the prior disk file without importing another scene.
Open the live-before backup scene to inspect the original. Saving it over Stage 1 is an explicit restore operation.

Tradeoffs:
CPU renderer and shadow submission costs reduced; tiny decor no longer casts shadows.
Triangle geometry is preserved. This is not a polygon-count reduction.
Combined meshes add approximately 128.4 MiB of vertex/index GPU data relative to shared-mesh instances, plus overhead.
The combined assets have CPU read/write disabled and use 16-bit indices where possible.
For a memory-constrained mobile target, GPU instancing / a vegetation renderer should be evaluated instead.
No build-target FPS, GPU-time or total memory benchmark is claimed.

Reports: OptimizationReport.txt, COMPACT.txt, VERIFIED.txt.
Screenshots: Previews/Before-*, After-*, Final-*.
