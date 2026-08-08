using Minecraft.Core.Utilities.Spatial;

namespace Minecraft.Core.Worlds.Structures.Villages;

/// <summary>
/// A village piece that stands on a levelled plot and is reached through one way in, which the road network
/// runs a road up to.
/// </summary>
public abstract class VillageBuilding : VillagePiece
{
    /// <summary>The way in, on the outer face of the plot.</summary>
    public (int X, int Z) Door { get; }

    /// <summary>The block outside the door, which is where the road up to this building ends.</summary>
    public (int X, int Z) Doorstep { get; }

    /// <param name="plot">The outer face of the building, which is also the ground it levels.</param>
    /// <param name="facing">The side the way in is on, which is the one turned towards the village centre.</param>
    /// <param name="doorstepDistance">
    /// How far outside the door the road ends, which has to clear whatever the building sticks out past its
    /// plot.
    /// </param>
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
