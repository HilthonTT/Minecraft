namespace Minecraft.Core.Audio;

/// <summary>
/// What a block sounds like underfoot and under a pick. Coarser than the blocks themselves: a dozen kinds of
/// stone all break the same way, so what a block carries is which of these sets it belongs to rather than
/// sounds of its own.
/// </summary>
public enum BlockSoundMaterial
{
    Stone,
    Grass,
    Gravel,
    Sand,
    Wood,
    Snow,

    /// <summary>
    /// Soft and muffled. The cactus, which is fleshy rather than leafy and gives under a blow instead of
    /// tearing, and whatever wool the sheep eventually turn into. The rest of the greenery is not this: a
    /// flower or a stalk of wheat sounds like the grass it grows out of.
    /// </summary>
    Cloth,
}
