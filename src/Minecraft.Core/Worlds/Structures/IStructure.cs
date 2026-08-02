namespace Minecraft.Core.Worlds.Structures;

/// <summary>
/// Something built into the world that is laid out once and then written one chunk at a time.
/// <para>
/// An implementation has to be pure: given the same seed and position it must decide on the same layout every
/// time, since each chunk it covers rebuilds the whole thing and keeps only its own slice. An unmodified
/// chunk is regenerated rather than read back from disk, so a structure that drifted would come back
/// different, cut in half along a chunk border.
/// </para>
/// </summary>
public interface IStructure
{
    /// <summary>The footprint this structure occupies, used to find the chunks it reaches into.</summary>
    StructureBounds Bounds { get; }

    /// <summary>Writes the part of this structure that falls inside the writer's chunk.</summary>
    void PlaceInto(StructureWriter writer);
}
