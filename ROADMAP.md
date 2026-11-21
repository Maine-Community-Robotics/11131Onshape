# FRC Profile FeatureScript — Roadmap

## Phase 1 (current: `frc_profile.fs`)
- Box tube: 1x1 and 1x2, 1/16" and 1/8" wall (sharp corners).
- 80/20 40 series: 40-4040, 40-4040-Lite, 40-4080, 40-4080-Lite, 40-8080.
- One part per selected straight line; centered / on top / below the sketch plane; rotate 90°; extend or trim each end.
- Part names for the BOM: `<type>x<length mm>`, e.g. `4040x100`, `8080x250.5`, `1x2x1/16x660.4`, `1x1x1/8x304.8 Drilled`.
- Part Number: `<type>x<length mm>` plus `-D` (drilled) and `-BLK` (black anodized), e.g. `4040x100-BLK`, `1x2x1/16x660.4-D`. Name, Part Number and Description are rewritten on every rebuild, so they follow sketch length changes. A value typed by hand in the Properties dialog overrides this and stops it updating.
- Part Description has the full wording for purchasing (profile, wall, holes, length in mm and inches for tube).
- Material set: 6061-T6 for tube; 80/20 density adjusted so mass matches the catalog lb/in.
- Appearance dropdown: Aluminum (bare) or Anodized aluminum (black). Material stays aluminum either way; "black anodized" is added to the Description.

## Phase 2 — box tube holes (optional checkbox)
- 1/4 in holes on 1/2 in spacing, through both walls of each pair of faces.
- 1 in face: 1 row (centered). 2 in face: 3 rows (−1/2, 0, +1/2 in).
- Along the length the pattern is centered, keeping at least the chosen end distance (default 1/2 in).
- Drilled tubes get " Drilled" at the end of the name so they don't merge with plain tube in the BOM.

### 80/20 geometry notes
The slot shape uses the exact dimensions that matter for fit: 40/80 mm outside, 8.13 mm opening, 4.06/4.32 mm lip, 20.57 mm cavity, 12.32 mm depth, Ø6.81 bore, 3.30/4.32 mm web.
Standard profiles (4040, 4080, 8080) use the solid-corner style (4.32 mm lip and web). Lite profiles use the corner-pocket style (4.06 mm lip, 3.30 mm web).
The rest is simplified: no rounded corners, square corner pockets, octagon center cavity on 40-4080.
The 15.36 / 14.85 / 8.00 mm drawing dimensions are not used directly.
40-8080 has no drawing yet: it reuses the standard 40-series slot (two per face, 40 mm apart) and an approximate center cavity. Mass matches the catalog (0.3563 lb/in). Replace with real dimensions when a drawing is available.

## Phase 3 — multiple sketches and joints
- Lines can be picked from any number of sketches; joints are detected in 3D.
- "Join where lines meet" (on by default):
  - Corner (two line ends touch): the longer line runs through to the outside of the corner, the shorter one stops against it. "Reverse joints" makes the shorter line run through instead (applies to every corner in the feature).
  - T (a line end touches the middle of another line): that end stops against the other member.
- Joint trims/extensions add to the manual Extend start/end values, and the names/part numbers use the final cut length.
- Limits: joints are sized for square (90°) joints; angled joints will leave a gap or overlap. Lines that cross in an X are left overlapping.

## Later additions
- Per-joint reverse (choose which member runs through at each corner, not just all at once).
- Miter option for angled corners.
- Holes: choose which faces get holes; 80/20 end tapping and access holes.
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
7. Holes: 1x1 shows one row on every face; 1x2 shows 1 row on the 1 in faces and 3 rows on the 2 in faces; spacing 1/2 in, hole 1/4 in; pattern centered along the length; still correct with Rotate 90°.
8. Joints: a rectangle frame sketch — longer sides run through, short sides butt inside; tick Reverse and it flips. A cross member ending on a side (T) stops at that side's face. A vertical post from a second sketch meeting a frame corner joins too.
9. BOM: insert parts into an Assembly, open the BOM — names read like `4040x100`, and equal-length parts group.
