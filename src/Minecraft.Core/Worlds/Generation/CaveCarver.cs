using Minecraft.Core.Utilities.Noise;
using Minecraft.Core.Worlds.Blocks;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Worlds.Generation;

/// <summary>
/// Hollows tunnels and caverns out of a chunk that has already been filled with terrain.
/// <para>
/// A tunnel is carved where two independent noise fields are both near zero at the same place. On its own
/// each field is near zero across a whole winding surface; where two of those surfaces cross, what is left
/// is a line, and giving that line a thickness turns it into a tunnel that wanders rather than the blobs a
/// single field would produce.
/// </para>
/// <para>
/// Whether a block is carved depends on nothing but its world position and the seeded noise fields, so a
/// chunk is hollowed the same way no matter which of its neighbours was generated first and tunnels line up
/// across chunk borders without the carver ever having to look outside the chunk it is given.
/// </para>
/// </summary>
public sealed class CaveCarver
{
    /// <summary>
    /// The lowest block a cave can reach, leaving a few layers of stone so the world keeps a solid floor.
    /// </summary>
    private const int LowestCaveY = 3;

    /// <summary>
    /// Over how many blocks below the surface a tunnel grows to its full thickness. Tunnels are pinched shut
    /// as they approach the surface, so only the ones passing very close to it break through into an opening,
    /// and the rest stay underground.
    /// <para>
    /// The depth is measured from the first air block above the terrain rather than from the surface block
    /// itself, which leaves the surface a small fraction of the full thickness. Measured from the surface it
    /// would be pinched to nothing exactly there and no cave could ever have a mouth.
    /// </para>
    /// </summary>
    private const float SurfaceFadeDepth = 6F;

    /// <summary>
    /// Over how many blocks a layer is pinched shut below its ceiling. Without it a layer that stops at a
    /// given height would cut its caves off mid air, leaving them with a flat ceiling at exactly that height.
    /// </summary>
    private const float CeilingFadeDepth = 16F;

    /// <summary>
    /// How far apart in the shared noise field the two fields of a layer are sampled. Far enough that the
    /// two are unrelated, which is what makes their crossing a tunnel rather than a smeared out copy.
    /// </summary>
    private const float PairDomainOffset = 4271.3F;

    /// <summary>
    /// One family of tunnels. Several are layered so that the world holds both cramped passages and the
    /// occasional wide open cavern instead of the same tunnel everywhere.
    /// </summary>
    /// <param name="HorizontalDetail">
    /// How quickly the fields vary across the world. Smaller means longer, wider, straighter tunnels.
    /// </param>
    /// <param name="VerticalDetail">
    /// The same for height. Above <paramref name="HorizontalDetail"/> it squashes tunnels into passages that
    /// are wider than they are tall, which is what keeps them walkable instead of vertical shafts.
    /// </param>
    /// <param name="Thickness">
    /// How near zero both fields have to be. Sets the tunnel radius, and with it the share of the underground
    /// that ends up hollow, which grows with its square.
    /// </param>
    /// <param name="DomainOffset">
    /// Where this layer samples the shared noise field, keeping it independent of the other layers.
    /// </param>
    /// <param name="CeilingY">
    /// The highest this layer ever carves, which it approaches over <see cref="CeilingFadeDepth"/> blocks
    /// rather than at a cut.
    /// </param>
    private readonly record struct TunnelLayer(
        float HorizontalDetail,
        float VerticalDetail,
        float Thickness,
        float DomainOffset,
        int CeilingY);

    private readonly TunnelLayer[] _layers =
    [
        // Ordinary caves: narrow winding passages, found at any depth.
        new TunnelLayer(
            HorizontalDetail: 0.020F,
            VerticalDetail: 0.030F,
            Thickness: 0.075F,
            DomainOffset: 0F,
            CeilingY: Constants.MAX_BUILD_HEIGHT),

        // Deep caverns: rarer, but several times wider and taller where they do turn up.
        new TunnelLayer(
            HorizontalDetail: 0.008F,
            VerticalDetail: 0.013F,
            Thickness: 0.050F,
            DomainOffset: 1783.9F,
            CeilingY: 48),
    ];

    /// <summary>
    /// Removes every block of <paramref name="chunk"/> that falls inside a cave.
    /// </summary>
    /// <param name="chunk">A chunk that has been filled with terrain but not yet decorated.</param>
    /// <param name="surfaceHeights">
    /// The height of the terrain surface of each column of the chunk, indexed by chunk local x and z.
    /// </param>
    public void Carve(Chunk chunk, int[,] surfaceHeights)
    {
        const int chunkDim = 16;

        for (int localX = 0; localX < chunkDim; localX++)
        {
            for (int localZ = 0; localZ < chunkDim; localZ++)
            {
                int worldX = chunk.GridX * chunkDim + localX;
                int worldZ = chunk.GridZ * chunkDim + localZ;
                int surfaceY = surfaceHeights[localX, localZ];

                // Carved from the bottom up, so the topmost block of the column is only ever recomputed
                // once, on the last removal that can reach it.
                for (int y = LowestCaveY; y <= surfaceY; y++)
                {
                    if (!IsHollowAt(worldX, y, worldZ, surfaceY))
                    {
                        continue;
                    }

                    if (chunk.GetBlockAt(localX, y, localZ).GetBlock() == BlockRegistry.Air)
                    {
                        continue;
                    }

                    chunk.RemoveBlockAt(localX, y, localZ);
                }
            }
        }
    }

    /// <summary>
    /// Whether the given world position falls inside a cave of any layer.
    /// </summary>
    private bool IsHollowAt(int worldX, int worldY, int worldZ, int surfaceY)
    {
        float surfaceFade = Math.Clamp((surfaceY + 1 - worldY) / SurfaceFadeDepth, 0F, 1F);

        foreach (TunnelLayer layer in _layers)
        {
            float ceilingFade = Math.Clamp((layer.CeilingY - worldY) / CeilingFadeDepth, 0F, 1F);
            if (ceilingFade <= 0F)
            {
                continue;
            }

            float thickness = layer.Thickness * surfaceFade * ceilingFade;

            float x = worldX * layer.HorizontalDetail + layer.DomainOffset;
            float y = worldY * layer.VerticalDetail + layer.DomainOffset;
            float z = worldZ * layer.HorizontalDetail + layer.DomainOffset;

            // The second field is only worth sampling where the first one is already near enough to zero,
            // which it is nowhere near most of the time.
            float first = Noise3DPerlin.Noise(x, y, z);
            if (MathF.Abs(first) >= thickness)
            {
                continue;
            }

            float second = Noise3DPerlin.Noise(
                x + PairDomainOffset,
                y + PairDomainOffset,
                z + PairDomainOffset);

            if ((first * first) + (second * second) < thickness * thickness)
            {
                return true;
            }
        }

        return false;
    }
}
