# FRC Profile FeatureScript — Roadmap

## Phase 1 (current: `frc_profile.fs`)
- Box tube: 1x1 and 1x2, 1/16" and 1/8" wall (sharp corners).
- 80/20 40 series: 40-4040, 40-4040-Lite, 40-4080, 40-4080-Lite, 40-8080.
- One part per selected straight line; centered / on top / below the sketch plane; rotate 90°; extend or trim each end.
- Part names include the cut length (inches for tube, mm for 80/20).
- Material set: 6061-T6 for tube; 80/20 density adjusted so mass matches the catalog lb/in.

### 80/20 geometry notes
The slot shape uses the exact dimensions that matter for fit: 40/80 mm outside, 8.13 mm opening, 4.06/4.32 mm lip, 20.57 mm cavity, 12.32 mm depth, Ø6.81 bore, 3.30/4.32 mm web.
The rest is simplified: no rounded corners, square corner pockets, octagon center cavity on 40-4080.
The 15.36 / 14.85 / 8.00 mm drawing dimensions are not used directly.
40-8080 has no drawing yet: it reuses the standard 40-series slot (two per face, 40 mm apart) and an approximate center cavity. Mass matches the catalog (0.3563 lb/in). Replace with real dimensions when a drawing is available.

## Later additions
- **Holes (deferred on purpose):** 0.5" pitch #10 clearance (0.196") holes on box tube, chosen per face, with end margins. Possibly 80/20 end tapping and access holes.
- Outside corner radius on box tube.
- 40-8080: exact internal geometry once a drawing is available.
- More profiles: 1/2" hex shaft, round tube, angle, other tube vendors.
- Icon, a student how-to page.

## Testing checklist (in Onshape)
1. Box tube, each size/wall, on lines in a Top-plane sketch.
2. Each 80/20 profile — check slot opening with a measure tool (8.13 mm).
3. Rotate 90° and all three alignments on a 1x2 and a 40-4080.
4. Extend/trim both ends; a trim larger than the line should give a clear error.
5. Several lines at once (whole frame perimeter); edit the sketch afterwards and confirm parts update.
6. Mass properties: a 1000 mm 40-4040 should weigh about 5.2 lb (0.1321 lb/in).
