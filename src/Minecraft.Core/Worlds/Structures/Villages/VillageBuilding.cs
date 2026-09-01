using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Structures.Villages;

public abstract class VillageBuilding : VillagePiece
{
    public (int X, int Z) Door { get; }

    public (int X, int Z) Doorstep { get; }

    protected VillageBuilding(StructureBounds plot, Direction facing, int doorstepDistance)
    {
        Door = facing switch
        {
            Direction.Back => (plot.CenterX, plot.MinZ),
            Direction.Front => (plot.CenterX, plot.MaxZ),
            Direction.Left => (plot.MinX, plot.CenterZ),
            _ => (plot.MaxX, plot.CenterZ),
        };

        Doorstep = facing switch
        {
            Direction.Back => (Door.X, Door.Z - doorstepDistance),
            Direction.Front => (Door.X, Door.Z + doorstepDistance),
            Direction.Left => (Door.X - doorstepDistance, Door.Z),
            _ => (Door.X + doorstepDistance, Door.Z),
        };
    }
}
