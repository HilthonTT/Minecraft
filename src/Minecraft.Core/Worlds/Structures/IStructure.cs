namespace Minecraft.Core.Worlds.Structures;

public interface IStructure
{
    StructureBounds Bounds { get; }

    void PlaceInto(StructureWriter writer);
}
