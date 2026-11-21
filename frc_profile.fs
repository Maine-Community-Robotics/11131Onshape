FeatureScript 2399;
import(path : "onshape/std/geometry.fs", version : "2399.0");

// FRC Profile — creates box tube or 80/20 T-slot parts along selected sketch lines.
//
// HOW TO INSTALL: create a Feature Studio in Onshape. Keep the two lines Onshape puts at the top
// of the new Feature Studio (its FeatureScript version + import) and paste everything below this
// comment block underneath them, replacing the two lines above.
//
// All profile dimensions below are in millimeters.

// ---------------------------------------------------------------------------------------------
// Dropdown choices
// ---------------------------------------------------------------------------------------------

export enum ProfileFamily
{
    annotation { "Name" : "Box tube" }
    BOX_TUBE,
    annotation { "Name" : "80/20 T-slot" }
    TSLOT_8020
}

export enum TubeSize
{
    annotation { "Name" : "1 x 1 in" }
    ONE_BY_ONE,
    annotation { "Name" : "1 x 2 in" }
    ONE_BY_TWO
}

export enum TubeWall
{
    annotation { "Name" : "1/16 in wall" }
    SIXTEENTH,
    annotation { "Name" : "1/8 in wall" }
    EIGHTH
}

export enum TSlotProfile
{
    annotation { "Name" : "40-4040 (40 x 40 mm)" }
    P40_4040,
    annotation { "Name" : "40-4040-Lite (40 x 40 mm)" }
    P40_4040_LITE,
    annotation { "Name" : "40-4080 (40 x 80 mm)" }
    P40_4080,
    annotation { "Name" : "40-4080-Lite (40 x 80 mm)" }
    P40_4080_LITE,
    annotation { "Name" : "40-8080 (80 x 80 mm)" }
    P40_8080
}

export enum ProfileFinish
{
    annotation { "Name" : "Aluminum" }
    ALUMINUM,
    annotation { "Name" : "Anodized aluminum" }
    ANODIZED
}

export enum ProfileAlignment
{
    annotation { "Name" : "Centered on line" }
    CENTER,
    annotation { "Name" : "On top of sketch plane" }
    ABOVE,
    annotation { "Name" : "Below sketch plane" }
    BELOW
}

// ---------------------------------------------------------------------------------------------
// Profile data — add new sizes here
// ---------------------------------------------------------------------------------------------

const TUBE_SIZES = {
        TubeSize.ONE_BY_ONE : { "label" : "1x1", "width" : 25.4, "height" : 25.4 },
        TubeSize.ONE_BY_TWO : { "label" : "1x2", "width" : 25.4, "height" : 50.8 }
    };

const TUBE_WALLS = {
        TubeWall.SIXTEENTH : { "label" : "1/16", "wall" : 1.5875 },
        TubeWall.EIGHTH : { "label" : "1/8", "wall" : 3.175 }
    };

// Weights are 80/20's catalog values (lb per inch). The modeled cross-section is simplified,
// so the part's density is adjusted to make the mass match the catalog weight.
// cellsX / cellsY are the centers of each 40 x 40 cell; each outside face of a cell gets a T-slot.
const TSLOT_PROFILES = {
        TSlotProfile.P40_4040 : { "code" : "4040", "name" : "80/20 40-4040", "width" : 40, "height" : 40, "cellsX" : [0], "cellsY" : [0], "lite" : false, "lbPerInch" : 0.1321 },
        TSlotProfile.P40_4040_LITE : { "code" : "4040-Lite", "name" : "80/20 40-4040-Lite", "width" : 40, "height" : 40, "cellsX" : [0], "cellsY" : [0], "lite" : true, "lbPerInch" : 0.0998 },
        TSlotProfile.P40_4080 : { "code" : "4080", "name" : "80/20 40-4080", "width" : 40, "height" : 80, "cellsX" : [0], "cellsY" : [-20, 20], "lite" : false, "lbPerInch" : 0.2317 },
        TSlotProfile.P40_4080_LITE : { "code" : "4080-Lite", "name" : "80/20 40-4080-Lite", "width" : 40, "height" : 80, "cellsX" : [0], "cellsY" : [-20, 20], "lite" : true, "lbPerInch" : 0.1893 },
        TSlotProfile.P40_8080 : { "code" : "8080", "name" : "80/20 40-8080", "width" : 80, "height" : 80, "cellsX" : [-20, 20], "cellsY" : [-20, 20], "lite" : false, "lbPerInch" : 0.3563 }
    };

// 80/20 40 series slot dimensions (from 80/20 drawings)
const SLOT_OPENING = 8.13;      // width of the slot opening
const SLOT_CAVITY = 20.57;      // width of the T cavity under the lips
const SLOT_DEPTH = 12.32;       // face to slot floor
const BORE_DIAMETER = 6.81;     // center bore (tap for M8)
// Two slot styles: standard profiles have solid corners; Lite profiles have hollow corner pockets
const LIP_STANDARD = 4.32;
const LIP_LITE = 4.06;
const WEB_STANDARD = 4.32;      // diagonal web thickness
const WEB_LITE = 3.30;
const CELL = 40;                // each 40 x 40 cell of a profile

const ALUMINUM_DENSITY = 2700 * kilogram / (meter * meter * meter);

// ---------------------------------------------------------------------------------------------
// Feature
// ---------------------------------------------------------------------------------------------

annotation { "Feature Type Name" : "FRC Profile",
        "Feature Type Description" : "Creates box tube or 80/20 T-slot parts along selected straight sketch lines. One part per line." }
export const frcProfile = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Lines", "Filter" : EntityType.EDGE && GeometryType.LINE }
        definition.edges is Query;

        annotation { "Name" : "Profile type", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.family is ProfileFamily;

        if (definition.family == ProfileFamily.BOX_TUBE)
        {
            annotation { "Name" : "Tube size" }
            definition.tubeSize is TubeSize;

            annotation { "Name" : "Wall thickness" }
            definition.tubeWall is TubeWall;

            annotation { "Name" : "Hole pattern (1/4 in holes, 1/2 in spacing)" }
            definition.tubeHoles is boolean;

            if (definition.tubeHoles)
            {
                annotation { "Name" : "Min. distance from ends to first hole" }
                isLength(definition.holeEndDistance, { (inch) : [0.125, 0.5, 10] } as LengthBoundSpec);
            }
        }
        else
        {
            annotation { "Name" : "80/20 profile" }
            definition.tslotProfile is TSlotProfile;
        }

        annotation { "Name" : "Appearance" }
        definition.finish is ProfileFinish;

        annotation { "Name" : "Alignment" }
        definition.alignment is ProfileAlignment;

        annotation { "Name" : "Rotate 90 degrees" }
        definition.rotate is boolean;

        annotation { "Name" : "Join where lines meet", "Default" : true }
        definition.autoJoints is boolean;

        if (definition.autoJoints)
        {
            annotation { "Name" : "Reverse joints (shorter line runs through)" }
            definition.flipJoints is boolean;
        }

        annotation { "Name" : "Extend start (negative trims)" }
        isLength(definition.startExtension, { (inch) : [-100, 0, 100] } as LengthBoundSpec);

        annotation { "Name" : "Extend end (negative trims)" }
        isLength(definition.endExtension, { (inch) : [-100, 0, 100] } as LengthBoundSpec);
    }
    {
        const spec = getProfileSpec(definition);
        const edges = evaluateQuery(context, definition.edges);
        if (size(edges) == 0)
            throw regenError("Select one or more straight sketch lines.", ["edges"]);

        // Lines can come from any number of sketches; joints are found in 3D
        var members = [];
        for (var edge in edges)
            members = append(members, getMember(context, edge, spec, definition));

        const adjust = getJointAdjustments(members, spec, definition);

        for (var i = 0; i < size(members); i += 1)
        {
            const partId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, partId, edges[i]);
            buildProfilePart(context, partId, members[i], spec, definition, adjust[2 * i], adjust[2 * i + 1]);
        }
    });

// ---------------------------------------------------------------------------------------------
// Joints
// ---------------------------------------------------------------------------------------------

const JOINT_TOLERANCE = 1e-5 * meter;

// Where one line meets another:
//   Corner (two line ends touch): one member runs through to the outside of the corner, the other
//     stops against it. By default the longer line runs through; "Reverse joints" swaps that.
//   T (a line end touches the middle of another line): the end stops against the other member.
// Returns [start, end] length adjustments for each member, flattened: positive extends, negative trims.
function getJointAdjustments(members is array, spec is map, definition is map) returns array
{
    const n = size(members);
    var trims = makeArray(2 * n, 0 * meter);
    var extensions = makeArray(2 * n, 0 * meter);

    if (definition.autoJoints)
    {
        for (var i = 0; i < n; i += 1)
        {
            for (var j = i + 1; j < n; j += 1)
            {
                const a = members[i];
                const b = members[j];
                for (var ea = 0; ea < 2; ea += 1)
                {
                    for (var eb = 0; eb < 2; eb += 1)
                    {
                        if (norm(memberEnd(a, ea) - memberEnd(b, eb)) > JOINT_TOLERANCE)
                            continue;

                        // Corner: decide which member runs through
                        var aThrough = (abs(a.length - b.length) < JOINT_TOLERANCE) || a.length > b.length;
                        if (definition.flipJoints == true)
                            aThrough = !aThrough;
                        const through = aThrough ? a : b;
                        const butting = aThrough ? b : a;
                        const throughIndex = aThrough ? 2 * i + ea : 2 * j + eb;
                        const buttingIndex = aThrough ? 2 * j + eb : 2 * i + ea;
                        extensions[throughIndex] = max(extensions[throughIndex], extentAlong(butting, through.dir, spec) / 2);
                        trims[buttingIndex] = max(trims[buttingIndex], extentAlong(through, butting.dir, spec) / 2);
                    }
                }

                // T joints: an end of one line on the middle of the other
                for (var ea = 0; ea < 2; ea += 1)
                {
                    if (isOnMiddle(memberEnd(a, ea), b))
                        trims[2 * i + ea] = max(trims[2 * i + ea], extentAlong(b, a.dir, spec) / 2);
                }
                for (var eb = 0; eb < 2; eb += 1)
                {
                    if (isOnMiddle(memberEnd(b, eb), a))
                        trims[2 * j + eb] = max(trims[2 * j + eb], extentAlong(a, b.dir, spec) / 2);
                }
            }
        }
    }

    // An end that stops against something is trimmed; otherwise it may be extended over a corner
    var result = [];
    for (var k = 0; k < 2 * n; k += 1)
        result = append(result, trims[k] > 0 * meter ? -trims[k] : extensions[k]);
    return result;
}

function memberEnd(member is map, whichEnd is number) returns Vector
{
    return whichEnd == 0 ? member.start : member.end;
}

// True if point lies on the line strictly between its ends
function isOnMiddle(point is Vector, member is map) returns boolean
{
    const t = dot(point - member.start, member.dir);
    if (t < JOINT_TOLERANCE || t > member.length - JOINT_TOLERANCE)
        return false;
    return norm(point - (member.start + member.dir * t)) < JOINT_TOLERANCE;
}

// How far a member's cross-section reaches along a direction (full width, not half)
function extentAlong(member is map, direction is Vector, spec is map) returns ValueWithUnits
{
    return abs(dot(direction, member.xAxis)) * spec.width * millimeter + abs(dot(direction, member.yAxis)) * spec.height * millimeter;
}

// ---------------------------------------------------------------------------------------------
// Building one part
// ---------------------------------------------------------------------------------------------

const ALUMINUM_COLOR = color(0.80, 0.81, 0.84);
const ANODIZED_COLOR = color(0.08, 0.08, 0.09); // black anodize

function getProfileSpec(definition is map) returns map
{
    var spec = getBaseSpec(definition);
    if (definition.finish == ProfileFinish.ANODIZED)
    {
        spec.appearance = ANODIZED_COLOR;
        spec.description = spec.description ~ ", black anodized";
        spec.partNumberSuffix = spec.partNumberSuffix ~ "-BLK";
    }
    else
    {
        spec.appearance = ALUMINUM_COLOR;
    }
    return spec;
}

function getBaseSpec(definition is map) returns map
{
    if (definition.family == ProfileFamily.BOX_TUBE)
    {
        const tube = TUBE_SIZES[definition.tubeSize];
        const wall = TUBE_WALLS[definition.tubeWall];
        var description = "Aluminum box tube " ~ tube.label ~ " in, " ~ wall.label ~ " in wall";
        if (definition.tubeHoles)
            description = description ~ ", 1/4 in holes on 1/2 in spacing";
        return {
                "kind" : "tube",
                "code" : tube.label ~ "x" ~ wall.label,
                "nameSuffix" : definition.tubeHoles ? " Drilled" : "",
                "partNumberSuffix" : definition.tubeHoles ? "-D" : "",
                "description" : description,
                "width" : tube.width,
                "height" : tube.height,
                "wall" : wall.wall,
                "holes" : definition.tubeHoles,
                "holeEndDistance" : definition.holeEndDistance,
                "materialName" : "Aluminum 6061-T6"
            };
    }
    var spec = TSLOT_PROFILES[definition.tslotProfile];
    spec.kind = "tslot";
    spec.nameSuffix = "";
    spec.partNumberSuffix = "";
    spec.holes = false;
    spec.description = spec.name ~ " T-slot extrusion";
    spec.materialName = "Aluminum 6063-T6 (80/20, mass-matched)";
    return spec;
}

// A selected line plus the orientation of the profile along it
function getMember(context is Context, edge is Query, spec is map, definition is map) returns map
{
    const tangent = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0 });
    const dir = tangent.direction;
    const edgeLength = evLength(context, { "entities" : edge });

    // "up" is the sketch plane normal, so profiles sit square to their sketch
    const up = getUpDirection(context, edge, dir);
    var xAxis = cross(up, dir);
    var extentAlongUp = spec.height * millimeter;
    if (definition.rotate)
    {
        xAxis = up;
        extentAlongUp = spec.width * millimeter;
    }

    var shift = 0 * meter;
    if (definition.alignment == ProfileAlignment.ABOVE)
        shift = extentAlongUp / 2;
    else if (definition.alignment == ProfileAlignment.BELOW)
        shift = -extentAlongUp / 2;

    return {
            "start" : tangent.origin,
            "end" : tangent.origin + dir * edgeLength,
            "dir" : dir,
            "length" : edgeLength,
            "up" : up,
            "shift" : shift,
            "xAxis" : xAxis,
            "yAxis" : cross(dir, xAxis) // profile 'height' direction
        };
}

function buildProfilePart(context is Context, id is Id, member is map, spec is map, definition is map,
    startJoint is ValueWithUnits, endJoint is ValueWithUnits)
{
    const dir = member.dir;
    const xAxis = member.xAxis;
    const startOffset = definition.startExtension + startJoint;
    const totalLength = member.length + startOffset + definition.endExtension + endJoint;
    if (totalLength < 0.01 * inch)
        throw regenError("A part would have zero or negative length. Reduce the trim, or check lines that are shorter than the tube is wide.", ["startExtension", "endExtension"]);

    const origin = member.start - dir * startOffset + member.up * member.shift;

    const sketchId = id + "sketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : plane(origin, dir, xAxis) });
    if (spec.kind == "tube")
        drawBoxTube(sketch, spec);
    else
        drawTSlot(sketch, spec);
    skSolve(sketch);

    const extrudeId = id + "extrude";
    opExtrude(context, extrudeId, {
                "entities" : qSketchRegion(sketchId, true),
                "direction" : dir,
                "endBound" : BoundingType.BLIND,
                "endDepth" : totalLength
            });
    opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(sketchId, EntityType.BODY) });

    const part = qCreatedBy(extrudeId, EntityType.BODY);
    if (spec.holes)
        cutTubeHoles(context, id, part, origin, dir, xAxis, totalLength, spec);

    setPartProperties(context, part, spec, totalLength);
}

// 1/4 in holes on a 1/2 in grid, through both walls of each pair of faces.
// Rows across a face = (face width / 1/2 in) - 1, so 1 row on a 1 in face and 3 rows on a 2 in face.
// Along the length the holes are centered, keeping at least holeEndDistance from each end.
function cutTubeHoles(context is Context, id is Id, part is Query, start is Vector, dir is Vector, xAxis is Vector,
    length is ValueWithUnits, spec is map)
{
    const pitch = 0.5 * inch;
    const holeRadius = 0.125 * inch;
    const count = floor((length - 2 * spec.holeEndDistance) / pitch + 1e-6) + 1;
    if (count < 1)
        return;
    const firstHole = (length - (count - 1) * pitch) / 2;

    const yAxis = cross(dir, xAxis);
    const passes = [
            // holes through the faces that are 'width' wide (drilled along yAxis)
            { "axis" : yAxis, "across" : spec.width * millimeter, "through" : spec.height * millimeter },
            // holes through the faces that are 'height' wide (drilled along xAxis)
            { "axis" : xAxis, "across" : spec.height * millimeter, "through" : spec.width * millimeter }
        ];

    var tools = [];
    for (var p = 0; p < size(passes); p += 1)
    {
        const pass = passes[p];
        const rows = round(pass.across / pitch) - 1;
        if (rows < 1)
            continue;

        // Sketch just outside the tube, then extrude the circles all the way through it
        const sketchId = id + ("holeSketch" ~ p);
        const sketchOrigin = start - pass.axis * (pass.through / 2 + 1 * millimeter);
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : plane(sketchOrigin, pass.axis, dir) });
        for (var i = 0; i < count; i += 1)
        {
            for (var j = 0; j < rows; j += 1)
            {
                skCircle(sketch, "hole" ~ i ~ "_" ~ j, {
                            "center" : vector(firstHole + i * pitch, (j - (rows - 1) / 2) * pitch),
                            "radius" : holeRadius
                        });
            }
        }
        skSolve(sketch);

        const toolId = id + ("holeTool" ~ p);
        opExtrude(context, toolId, {
                    "entities" : qSketchRegion(sketchId),
                    "direction" : pass.axis,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : pass.through + 2 * millimeter
                });
        opDeleteBodies(context, id + ("deleteHoleSketch" ~ p), { "entities" : qCreatedBy(sketchId, EntityType.BODY) });
        tools = append(tools, qCreatedBy(toolId, EntityType.BODY));
    }

    if (size(tools) > 0)
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : qUnion(tools),
                    "targets" : part,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
}

function getUpDirection(context is Context, edge is Query, dir is Vector) returns Vector
{
    var up = undefined;
    try silent
    {
        up = evOwnerSketchPlane(context, { "entity" : edge }).normal;
    }
    if (up == undefined)
    {
        // Not a sketch line (e.g. a model edge): use world Z, or world X for vertical lines
        var reference = vector(0, 0, 1);
        if (abs(dot(reference, dir)) > 0.99)
            reference = vector(1, 0, 0);
        up = normalize(reference - dir * dot(reference, dir));
    }
    return up;
}

function setPartProperties(context is Context, part is Query, spec is map, length is ValueWithUnits)
{
    // Name for the BOM: <type>x<length in mm>, e.g. "4040x100" or "1x2x1/16x660.4 Drilled"
    const lengthMm = toString(roundToPrecision(length / millimeter, 1));
    setProperty(context, { "entities" : part, "propertyType" : PropertyType.NAME, "value" : spec.code ~ "x" ~ lengthMm ~ spec.nameSuffix });

    // Part number: same type x length, plus -D (drilled) and -BLK (black anodized) so different SKUs never merge.
    // Set on every rebuild, so it follows the sketch when a line's length changes.
    setProperty(context, { "entities" : part, "propertyType" : PropertyType.PART_NUMBER, "value" : spec.code ~ "x" ~ lengthMm ~ spec.partNumberSuffix });

    var description = spec.description ~ ", " ~ lengthMm ~ " mm";
    if (spec.kind == "tube")
        description = description ~ " (" ~ toString(roundToPrecision(length / inch, 3)) ~ " in)";
    setProperty(context, { "entities" : part, "propertyType" : PropertyType.DESCRIPTION, "value" : description });

    var density = ALUMINUM_DENSITY;
    if (spec.lbPerInch != undefined)
    {
        const area = evVolume(context, { "entities" : part }) / length;
        density = (spec.lbPerInch * 0.45359237 * kilogram / inch) / area;
    }
    setProperty(context, { "entities" : part, "propertyType" : PropertyType.MATERIAL, "value" : material(spec.materialName, density) });
    setProperty(context, { "entities" : part, "propertyType" : PropertyType.APPEARANCE, "value" : spec.appearance });
}

// ---------------------------------------------------------------------------------------------
// Cross-section sketches (sketch x = across, sketch y = "up")
// ---------------------------------------------------------------------------------------------

function drawBoxTube(sketch is Sketch, spec is map)
{
    const w = spec.width / 2;
    const h = spec.height / 2;
    const t = spec.wall;
    skRectangle(sketch, "outer", { "firstCorner" : vector(-w, -h) * millimeter, "secondCorner" : vector(w, h) * millimeter });
    skRectangle(sketch, "inner", { "firstCorner" : vector(-w + t, -h + t) * millimeter, "secondCorner" : vector(w - t, h - t) * millimeter });
}

function drawTSlot(sketch is Sketch, spec is map)
{
    const W = spec.width;
    const H = spec.height;
    const xs = spec.cellsX; // ascending
    const ys = spec.cellsY; // ascending
    const slot = tSlotShape(spec.lite);

    // Outer boundary, walked counter-clockwise, with a T-slot cut into each outside face of each cell
    var pts = [vector(-W / 2, -H / 2)];
    for (var k = 0; k < size(xs); k += 1)                                                                   // bottom
        pts = concatenateArrays([pts, slotPoints(vector(xs[k], -H / 2), vector(1, 0), vector(0, 1), slot)]);
    pts = append(pts, vector(W / 2, -H / 2));
    for (var k = 0; k < size(ys); k += 1)                                                                   // right
        pts = concatenateArrays([pts, slotPoints(vector(W / 2, ys[k]), vector(0, 1), vector(-1, 0), slot)]);
    pts = append(pts, vector(W / 2, H / 2));
    for (var k = size(xs) - 1; k >= 0; k -= 1)                                                              // top
        pts = concatenateArrays([pts, slotPoints(vector(xs[k], H / 2), vector(-1, 0), vector(0, -1), slot)]);
    pts = append(pts, vector(-W / 2, H / 2));
    for (var k = size(ys) - 1; k >= 0; k -= 1)                                                              // left
        pts = concatenateArrays([pts, slotPoints(vector(-W / 2, ys[k]), vector(0, -1), vector(1, 0), slot)]);
    pts = append(pts, vector(-W / 2, -H / 2));
    skPolyline(sketch, "outline", { "points" : toMillimeters(pts) });

    // Center bore in each cell
    var boreCount = 0;
    for (var x in xs)
    {
        for (var y in ys)
        {
            skCircle(sketch, "bore" ~ boreCount, { "center" : vector(x, y) * millimeter, "radius" : BORE_DIAMETER / 2 * millimeter });
            boreCount += 1;
        }
    }

    // Lite profiles have hollow corners
    if (spec.lite)
    {
        const cx = W / 2 - 5;
        const cy = H / 2 - 5;
        const corners = [vector(cx, cy), vector(-cx, cy), vector(-cx, -cy), vector(cx, -cy)];
        for (var k = 0; k < size(corners); k += 1)
        {
            skRectangle(sketch, "pocket" ~ k, {
                        "firstCorner" : (corners[k] - vector(2.7, 2.7)) * millimeter,
                        "secondCorner" : (corners[k] + vector(2.7, 2.7)) * millimeter
                    });
        }
    }

    // Multi-cell profiles have a large center cavity (approximate shape)
    var cavity = undefined;
    if (size(xs) == 1 && size(ys) == 2) // 40 x 80
    {
        cavity = [vector(15, -4), vector(15, 4), vector(6, 12), vector(-6, 12), vector(-15, 4),
                vector(-15, -4), vector(-6, -12), vector(6, -12), vector(15, -4)];
    }
    else if (size(xs) == 2 && size(ys) == 2) // 80 x 80
    {
        cavity = [vector(14, -6), vector(14, 6), vector(6, 14), vector(-6, 14), vector(-14, 6),
                vector(-14, -6), vector(-6, -14), vector(6, -14), vector(14, -6)];
    }
    if (cavity != undefined)
        skPolyline(sketch, "centerCavity", { "points" : toMillimeters(cavity) });
}

// One T-slot as [along-face offset, depth] pairs, in the order they are walked
function tSlotShape(lite is boolean) returns array
{
    const a = SLOT_OPENING / 2;
    const c = SLOT_CAVITY / 2;
    const lip = lite ? LIP_LITE : LIP_STANDARD;
    const webOffset = (lite ? WEB_LITE : WEB_STANDARD) / sqrt(2);
    const straightDepth = CELL / 2 - c - webOffset;            // where the cavity's angled walls begin
    const floorHalf = (CELL / 2 - SLOT_DEPTH) - webOffset;     // half-width of the slot floor
    return [[-a, 0], [-a, lip], [-c, lip], [-c, straightDepth], [-floorHalf, SLOT_DEPTH],
            [floorHalf, SLOT_DEPTH], [c, straightDepth], [c, lip], [a, lip], [a, 0]];
}

function slotPoints(center is Vector, along is Vector, inward is Vector, slot is array) returns array
{
    var result = [];
    for (var p in slot)
        result = append(result, center + along * p[0] + inward * p[1]);
    return result;
}

function toMillimeters(points is array) returns array
{
    return mapArray(points, function(p)
        {
            return p * millimeter;
        });
}
