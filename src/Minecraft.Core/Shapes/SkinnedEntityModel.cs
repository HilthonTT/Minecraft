using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shapes;

public sealed class SkinnedEntityModel : EntityModel
{
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
                Bottom: new Vector4(u + d + w, v, w, d),
                Top: new Vector4(u + d, v, w, d),
                Right: new Vector4(u, v + d, d, h),
                Front: new Vector4(u + d, v + d, w, h),
                Left: new Vector4(u + d + w, v + d, d, h),
                Back: new Vector4(u + d + w + d, v + d, w, h));
        }
    }

    public SkinnedEntityModel(Texture skin, SkinBox[] boxes, float modelHeightInUnits, Vector3 entitySize)
        : base(skin)
    {
        float scale = entitySize.Y / modelHeightInUnits;

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
        Vector3 min = modelOrigin + (box.Origin - new Vector3(box.Inflate)) * scale;
        Vector3 max = min + box.Size * scale;

        BoxNet net = BoxNet.For(box.TexOffset, box.TexSize);

        (Vector4 top, Vector4 bottom, Vector4 front, Vector4 back) = box.Pose == SkinBoxPose.Upright
            ? (net.Top, net.Bottom, net.Front, net.Back)
            : (net.Back, net.Front, net.Top, net.Bottom);

        bool turnSides = box.Pose == SkinBoxPose.Lying;

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

    private Vector2[] ToUVs(Vector4 rectangle, bool quarterTurn = false)
    {
        const float Inset = 0.01F;

        float uMin = (rectangle.X + Inset) / Texture.PixelWidth;
        float vMin = (rectangle.Y + Inset) / Texture.PixelHeight;
        float uMax = (rectangle.X + rectangle.Z - Inset) / Texture.PixelWidth;
        float vMax = (rectangle.Y + rectangle.W - Inset) / Texture.PixelHeight;

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
