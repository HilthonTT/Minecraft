using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

/// <summary>
/// A mob built out of cuboids wearing a Minecraft skin sheet, the same way the models in the game itself are.
/// <para>
/// The parts are described in model units, sixteen to a block, which is the grid the artwork is drawn on. The
/// whole model is then scaled so that it stands exactly as tall as the entity's hitbox, which keeps a mob from
/// sinking into the ground or poking through a ceiling it is meant to fit under.
/// </para>
/// </summary>
public sealed class SkinnedEntityModel : EntityModel
{
    /// <summary>
    /// The rectangles of the sheet the six faces of one box are cut from, in texels.
    /// </summary>
    /// <remarks>
    /// A box is unwrapped as a cross laid flat: the two caps sit side by side on the top row, and below them
    /// the four sides run left to right as right, front, left, back. Every rectangle is placed relative to the
    /// box's own corner on the sheet, so the same net can be reused wherever it was drawn.
    /// </remarks>
    private readonly record struct BoxNet(
        Vector4 Bottom,
        Vector4 Top,
        Vector4 Right,
        Vector4 Front,
        Vector4 Left,
        Vector4 Back)
    {
        public static BoxNet For(Vector2i offset, Vector3i size)
        {
            float u = offset.X;
            float v = offset.Y;
            float w = size.X;
            float h = size.Y;
            float d = size.Z;

            return new BoxNet(
                Bottom: new Vector4(u + d, v, w, d),
                Top: new Vector4(u + d + w, v, w, d),
                Right: new Vector4(u, v + d, d, h),
                Front: new Vector4(u + d, v + d, w, h),
                Left: new Vector4(u + d + w, v + d, d, h),
                Back: new Vector4(u + d + w + d, v + d, w, h));
        }
    }

    /// <param name="skin">The sheet the parts are cut from. Its size decides how texels map onto UVs.</param>
    /// <param name="boxes">The parts, in model units.</param>
    /// <param name="modelHeightInUnits">
    /// How tall the model stands in its own units. Everything is scaled by the entity's height over this, so
    /// the drawn mob ends up the height of its hitbox however the parts were laid out.
    /// </param>
    /// <param name="entitySize">The width, height and length of the entity's hitbox, in blocks.</param>
    public SkinnedEntityModel(Texture skin, SkinBox[] boxes, float modelHeightInUnits, Vector3 entitySize)
        : base(skin)
    {
        float scale = entitySize.Y / modelHeightInUnits;

        // An entity mesh is drawn from a corner of its hitbox rather than around its middle, so the model,
        // which is built around its own vertical axis, is walked over to the middle of that footprint.
        var modelOrigin = new Vector3(entitySize.X / 2.0F, 0, entitySize.Z / 2.0F);

        var faces = new List<BlockFace>(boxes.Length * 6);
        foreach (SkinBox box in boxes)
        {
            AddBoxFaces(faces, box, scale, modelOrigin);
        }

        EntityFaces = [.. faces];
    }

    private void AddBoxFaces(List<BlockFace> faces, SkinBox box, float scale, Vector3 modelOrigin)
    {
        // The origin names the corner the artwork was drawn at, so an inflated box grows back past it by as
        // much as it grows forward at the far end, leaving the part centred where it was.
        Vector3 min = modelOrigin + (box.Origin - new Vector3(box.Inflate)) * scale;
        Vector3 max = min + box.Size * scale;

        BoxNet net = BoxNet.For(box.TexOffset, box.TexSize);

        // Tipping the box onto its back brings the back of the net up to the top, drops the front of it onto
        // the underside, and swings the two caps round to face front and rear. It is the back rather than the
        // front that ends up on top: a quadruped's body is unwrapped as though the animal were reared up on
        // its hind legs, so the face drawn towards the bottom of the sheet is its belly.
        (Vector4 top, Vector4 bottom, Vector4 front, Vector4 back) = box.Pose == SkinBoxPose.Upright
            ? (net.Top, net.Bottom, net.Front, net.Back)
            : (net.Back, net.Front, net.Bottom, net.Top);

        // The two sides keep their own rectangles wherever the box is pointed, but a tipped box carries them
        // round with it, so they go on a quarter turn from the way they were drawn.
        bool turnSides = box.Pose == SkinBoxPose.Lying;

        // Wound counter clockwise seen from outside the box, so back face culling keeps the outside.
        faces.Add(new BlockFace(
            [new(max.X, min.Y, min.Z), new(min.X, min.Y, min.Z), new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z)],
            ToUVs(back)));

        faces.Add(new BlockFace(
            [new(max.X, min.Y, max.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(max.X, max.Y, max.Z)],
            ToUVs(net.Right, turnSides)));

        faces.Add(new BlockFace(
            [new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)],
            ToUVs(front)));

        faces.Add(new BlockFace(
            [new(min.X, min.Y, min.Z), new(min.X, min.Y, max.Z), new(min.X, max.Y, max.Z), new(min.X, max.Y, min.Z)],
            ToUVs(net.Left, turnSides)));

        faces.Add(new BlockFace(
            [new(min.X, max.Y, max.Z), new(max.X, max.Y, max.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z)],
            ToUVs(top)));

        faces.Add(new BlockFace(
            [new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, min.Y, max.Z), new(min.X, min.Y, max.Z)],
            ToUVs(bottom)));
    }

    /// <summary>
    /// Turns a rectangle of the sheet into the four UVs of a face, corner for corner with the vertex order the
    /// faces above are wound in.
    /// </summary>
    /// <param name="rectangle">Left, top, width and height, in texels.</param>
    /// <param name="quarterTurn">
    /// Whether the rectangle goes on a quarter turn from the way it was drawn. Needed by the sides of a box
    /// that has been tipped over: the artwork was drawn for it standing up, so its long axis runs across the
    /// part rather than along it until it is turned to follow.
    /// </param>
    private Vector2[] ToUVs(Vector4 rectangle, bool quarterTurn = false)
    {
        // Pulled a hair inside the rectangle. Texels are sampled at their centre, and without the inset a
        // face landing exactly on the boundary can pick up the neighbouring part of the sheet along its edge.
        const float Inset = 0.01F;

        float uMin = (rectangle.X + Inset) / Texture.PixelWidth;
        float vMin = (rectangle.Y + Inset) / Texture.PixelHeight;
        float uMax = (rectangle.X + rectangle.Z - Inset) / Texture.PixelWidth;
        float vMax = (rectangle.Y + rectangle.W - Inset) / Texture.PixelHeight;

        // The corners of the rectangle walked round in the order the faces above are wound in. Turning the
        // artwork a quarter is the same as handing every vertex the corner its neighbour would have had.
        return quarterTurn
            ?
            [
                new Vector2(uMax, vMin),
                new Vector2(uMax, vMax),
                new Vector2(uMin, vMax),
                new Vector2(uMin, vMin),
            ]
            :
            [
                new Vector2(uMax, vMax),
                new Vector2(uMin, vMax),
                new Vector2(uMin, vMin),
                new Vector2(uMax, vMin),
            ];
    }
}
