# Tutorial woodland decoration

All new decoration assets, editor tooling, backups and review images live in this folder.

## Art direction

- A/B: the same sunlit meadow woodland, broadleaf canopies, some pines, warm sandy track, flower and grass patches.
- C: a separate pine-heavy hollow with larger rocks, fallen timber and fewer flowers.
- Both styles use the Low Poly Nature prefab models and Stage 1's woodland palette.

## Gameplay boundaries

- Original character, camera, finish, SleepTree and SleepSpot transforms are preserved.
- The race corridor remains clear of entire decoration bounds, including tree canopies.
- A/B reserve the same empty sleep glade, including in A where the mechanic is absent.
- Spawn, finish and mechanic clearings are reserved before placement.
- Terrain sampling checks the full footprint plus a margin: steep faces, cliff edges and raised plateaus are excluded.
- Woodland decoration has no colliders. A parent NavMeshModifier excludes it from future NavMesh builds.
- Every scene gets its own TerrainData copy. Heightmaps are checked against the original; only texture layers/painting change.
- Existing NavMesh assets, gameplay scripts, source prefabs, source materials and project settings are untouched.

## Rendering cost

- Low polygon prefab meshes and shared, instancing-enabled material copies.
- Groundcover is combined by material in 12 metre cells for local frustum culling.
- Groundcover does not cast shadows; trees have a final screen-size culling LOD.
- Disabled source prefab instances remain available for editing.
- All generation runs in the Editor; no runtime scatter or mesh combining.
- Reports contain structural renderer/material/triangle counts, not measured player-build FPS.

## Review and rollback

- `Previews`: terrain slope maps and before/after overview and trackside renders.
- `Reports`: terrain inspection, original-object protection results and generation counts.
- `Backups/*.unity.txt`: exact pre-decoration scene files. To fully roll back a tutorial, close that scene first, copy the backup over the corresponding original `.unity` file, and keep the original `.unity.meta` file.
- To hide decoration temporarily, disable the scene's `Tutorial Woodland` root. This leaves the separate terrain painting and lighting in place.
- A/B reference one decoration prefab; C references another. TerrainData assets are separate for all three scenes.
- The editor tool runs only with an explicit request marker and no completion report. Request markers are removed after successful generation.

## Trackside fences

- Each tutorial has a separate `Tutorial Fence` root and prefab.
- Fences follow the straight track at x = -6.4 and +6.4 metres, with open ends.
- The right-hand SleepTree approach has a 16 metre opening, including in A to match B.
- Visuals use merged cube geometry, 13 renderers and 13 static BoxColliders per scene. No Rigidbody.
- Fence height is 1.28 metres: it blocks ground movement but does not guarantee containing airborne explosions.
- Original scene components were checked unchanged before saving. `Reports/FenceReport.txt` contains the counts and gap coordinates.
- `Backups/Before Fences` holds the decorated scenes immediately before fence placement.
- Disabling `Tutorial Fence` disables both visuals and fence collision.

## Stage 1 fences

- `Stage 1 Fence` follows the existing inline/outline course guides, 6.2 metres inside/outside the track centre.
- Closed inner/outer loops, with the start/finish crossing left unobstructed across the track.
- 384 visual cubes are merged into 16 renderers; 128 short static BoxColliders follow the curved perimeter. No Rigidbody.
- Meshes use 16-bit indices and release their CPU-readable data after generation.
- All lane waypoints, spawn markers and goal markers were checked at least 4.13 metres from fence segment centres.
- Original component snapshots, Terrain and NavMesh references were checked unchanged. Actual racing/explosion behaviour still requires a Play-mode check.
- Disable `Stage 1 Fence` to disable the entire fence and its collision. The pre-fence scene backup is in `Backups/Before Fences/03. Stage 1.unity.txt`.

## Fence / nature clearance

- Decorative rock, tree, log and stump meshes were checked against the fence boxes using triangle/box intersection, including a small clearance margin.
- Tutorials A/B/C had no intersections. Stage 1 had 20 intersecting objects, moved toward the forest/infield and away from the racing corridor.
- Final checks found zero remaining intersections among the inspected objects. SleepTree and gameplay objects were excluded from relocation.
- Source mesh Read/Write settings were preserved, using editor-only mesh access where needed.
- `Reports/FenceClearanceReport.txt` records all moved positions; `Backups/Before Fence Clearance` holds the scenes immediately before relocation.

## Tutorial goal stripes

- A/B/C each have `Stage/Tutorial Goal Stripe`, a 9.8 metre white chalk stripe aligned with the existing finish trigger.
- The chalk material is copied from Stage 1 into this folder. No collider is added; the visual is excluded from NavMesh builds.
- Existing finish detection and gameplay scripts are preserved.
- `Backups/Before Goal Stripes` holds pre-stripe scenes, and `Previews/*-Goal-Stripe.png` contains isolated review renders.

## 4-2 tree collision

- All standing decorative trees and SleepTrees have static, non-convex mesh collision matching the existing low-poly trunk, branches and canopy. Counts: A 81, B 82, C 80, Stage 1 360.
- Existing Stage 1 trunk-only proxies are disabled to avoid duplicate contacts. No tree Rigidbody or source prefab edits.
- `Tree Sliding.physicMaterial` uses zero friction and zero bounce, allowing ragdolls to slide naturally from foliage.
- HareController enables ContinuousDynamic only on active ragdoll bodies and returns to Discrete before making them kinematic.
- If recovery starts while supported by a tree, the ragdoll moves toward its return point while staying in physics, then waits for landing before restoring navigation.
- Physics tests: 35m/s canopy capsule drops, 25m/s trunk strikes and actual 11-body hare drops at 15m/s, across five representative C/Stage 1/SleepTree cases. All 15 passed.
- Two additional live Play-mode tests verified C and Stage 1 return to ground NavMesh in Returning state after a canopy drop: 17 checks passed in total. These are representative tests, not a guarantee for arbitrary velocities or every possible landing.
- Tests use isolated physics scenes; test-only runtime values are discarded when leaving Play mode. Automatic test requests are removed after completion.
- `Reports/TreePhysicsReport.txt` records results; `Backups/Before Tree Collision` contains scene and HareController backups.
