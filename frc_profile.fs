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
const TSLOT_PROFILES = {
// cellsX / cellsY are the centers of each 40 x 40 cell; each outside face of a cell gets a T-slot.
const TSLOT_PROFILES = {
        TSlotProfile.P40_4040 : { "name" : "80/20 40-4040", "width" : 40, "height" : 40, "cellsX" : [0], "cellsY" : [0], "lite" : false, "lbPerInch" : 0.1321 },
        TSlotProfile.P40_4040_LITE : { "name" : "80/20 40-4040-Lite", "width" : 40, "height" : 40, "cellsX" : [0], "cellsY" : [0], "lite" : true, "lbPerInch" : 0.0998 },
        TSlotProfile.P40_4080 : { "name" : "80/20 40-4080", "width" : 40, "height" : 80, "cellsX" : [0], "cellsY" : [-20, 20], "lite" : false, "lbPerInch" : 0.2317 },
        TSlotProfile.P40_4080_LITE : { "name" : "80/20 40-4080-Lite", "width" : 40, "height" : 80, "cellsX" : [0], "cellsY" : [-20, 20], "lite" : true, "lbPerInch" : 0.1893 },
        TSlotProfile.P40_8080 : { "name" : "80/20 40-8080", "width" : 80, "height" : 80, "cellsX" : [-20, 20], "cellsY" : [-20, 20], "lite" : false, "lbPerInch" : 0.3563 }
    };

// 80/20 40 series slot dimensions (from 80/20 drawings)
const SLOT_OPENING = 8.13;      // width of the slot opening
const SLOT_CAVITY = 20.57;      // width of the T cavity under the lips
const SLOT_DEPTH = 12.32;       // face to slot floor
const BORE_DIAMETER = 6.81;     // center bore (tap for M8)
const LIP_STANDARD = 4.06;
const LIP_LITE = 4.32;
const WEB_STANDARD = 3.30;      // diagonal web thickness
const WEB_LITE = 4.32;
const CELL = 40;                // each 40 x 40 cell of a profile

const ALUMINUM_DENSITY = 2700 * kilogram / meter ^ 3;

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
        }
        else
        {
            annotation { "Name" : "80/20 profile" }
            definition.tslotProfile is TSlotProfile;
        }

        annotation { "Name" : "Alignment" }
        definition.alignment is ProfileAlignment;

        annotation { "Name" : "Rotate 90 degrees" }
        definition.rotate is boolean;

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

        for (var i = 0; i < size(edges); i += 1)
        {
            const partId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, partId, edges[i]);
            buildProfilePart(context, partId, edges[i], spec, definition);
        }
    });

// ---------------------------------------------------------------------------------------------
// Building one part
// ---------------------------------------------------------------------------------------------

function getProfileSpec(definition is map) returns map
{
    if (definition.family == ProfileFamily.BOX_TUBE)
    {
        const tube = TUBE_SIZES[definition.tubeSize];
        const wall = TUBE_WALLS[definition.tubeWall];
        return {
                "kind" : "tube",
                "name" : tube.label ~ "x" ~ wall.label ~ " Box Tube",
                "width" : tube.width,
                "height" : tube.height,
                "wall" : wall.wall,
                "materialName" : "Aluminum 6061-T6",
                "appearance" : color(0.62, 0.64, 0.68)
            };
    }
    var spec = TSLOT_PROFILES[definition.tslotProfile];
    spec.kind = "tslot";
    spec.materialName = "Aluminum 6063-T6 (80/20, mass-matched)";
    spec.appearance = color(0.82, 0.83, 0.86);
    return spec;
}

function buildProfilePart(context is Context, id is Id, edge is Query, spec is map, definition is map)
{
    const tangent = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0 });
    const dir = tangent.direction;
    const edgeLength = evLength(context, { "entities" : edge });
    const totalLength = edgeLength + definition.startExtension + definition.endExtension;
    if (totalLength < 0.01 * inch)
        throw regenError("A part would have zero or negative length. Reduce the trim.", ["startExtension", "endExtension"]);

    // "up" is the sketch plane normal, so profiles sit square to the frame sketch
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

    const origin = tangent.origin - dir * definition.startExtension + up * shift;

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

    setPartProperties(context, qCreatedBy(extrudeId, EntityType.BODY), spec, totalLength);
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
    var lengthText;
    if (spec.kind == "tube")
        lengthText = toString(roundToPrecision(length / inch, 3)) ~ " in";
    else
        lengthText = toString(roundToPrecision(length / millimeter, 1)) ~ " mm";

    setProperty(context, { "entities" : part, "propertyType" : PropertyType.NAME, "value" : spec.name ~ " - " ~ lengthText });

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

    // Standard (non-Lite) profiles have hollow corners
    if (!spec.lite)
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
