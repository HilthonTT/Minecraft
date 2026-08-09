namespace Minecraft.Core;

public static class Constants
{
    //General
    public const float CUBE_DIM = 1.0F;
    public const float HALF_CUBE_DIM = CUBE_DIM / 2.0F;
    public const int NUM_SECTIONS_IN_CHUNKS = 16;
    public const int MAX_BUILD_HEIGHT = NUM_SECTIONS_IN_CHUNKS * 16;

    //Rendering
    /// <summary>How far out from the player, in chunks, the world is loaded and drawn.</summary>
    public const int VIEW_DISTANCE_CHUNKS = 8;

    /// <summary>The same distance in blocks, which is what the fog is measured against.</summary>
    public const float VIEW_DISTANCE_BLOCKS = VIEW_DISTANCE_CHUNKS * 16;

    /// <summary>
    /// Where the distance haze starts, as a fraction of the view distance. Terrain nearer than this is
    /// drawn untouched.
    /// </summary>
    public const float FOG_START_FRACTION = 0.65F;

    /// <summary>
    /// Where the haze has closed over completely, as a fraction of the view distance. One view distance is
    /// exactly the closest the edge of the loaded world can ever be — the loaded area is a square of
    /// chunks, so along an axis it ends at the view distance and at a corner much further out. Measuring
    /// the fog as a horizontal distance therefore closes it over the corners first, and what is left
    /// visible reads as a circle rather than as four straight edges with the world stopping along them.
    /// </summary>
    public const float FOG_END_FRACTION = 1.0F;

    //Physics
    public const float GRAVITY = -475F;

    /// <summary>The downwards pull a fall builds up to, so that falling stays within what collision can follow.</summary>
    public const float MAX_FALL_SPEED = -1000F;

    /// <summary>
    /// The share of gravity that still acts on a body in water. What is left is what the water holds up, so
    /// entering one turns a fall into a sink.
    /// </summary>
    public const float WATER_GRAVITY_MULTIPLIER = 0.16F;

    /// <summary>
    /// How fast a body sinks through water at most. Far short of terminal velocity in air, so that falling
    /// into a lake is stopped by it rather than carried to the bottom.
    /// </summary>
    public const float MAX_SINK_SPEED = -55F;

    /// <summary>How hard a swimmer pulls themselves upwards, which has to beat the sinking above.</summary>
    public const float SWIM_UP_FORCE = 145F;

    /// <summary>What water does to how fast a body can be pushed through it.</summary>
    public const float WATER_MOVE_MULTIPLIER = 0.55F;

    /// <summary>
    /// How hard running water carries a body along with it. Well short of what a swimmer can do under their
    /// own power, so a current is something to be felt and swum out of rather than something to be caught in.
    /// </summary>
    public const float WATER_PUSH_FORCE = 14F;

    /// <summary>
    /// The longest frame the game simulates in one go. A frame that took longer, because of a hitch or
    /// because the window was not being drawn, is simulated as if it took this long instead: catching up on
    /// all of it at once would move everything so far in a single step that it passes through the world.
    /// </summary>
    public const float MAX_FRAME_TIME_SECONDS = 0.1F;

    //Player
    public const float PLAYER_HEIGHT = CUBE_DIM * 1.75F;
    public const float PLAYER_CAMERA_HEIGHT = CUBE_DIM * 1.5F;
    public const float PLAYER_WIDTH = HALF_CUBE_DIM; /** X direction */
    public const float PLAYER_LENGTH = HALF_CUBE_DIM; /** Z direction */

    public const float PLAYER_BASE_MOVE_SPEED = 50F;
    public const float PLAYER_SPRINT_MULTIPLIER = 1.75F;
    public const float PLAYER_CROUCH_MULTIPLIER = 0.35F;
    public const float PLAYER_JUMP_FORCE = 115F;
    public const float PLAYER_STOP_FORCE_MULTIPLIER = 0.80F;
    public const float PLAYER_MOUSE_SENSIVITY = 0.0015F;
    public const float PLAYER_IN_AIR_SLOWDOWN = 0.75F;
    public const float PLAYER_FLYING_MULTIPLIER = 4.0F;
}
