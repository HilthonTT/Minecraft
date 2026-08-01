namespace Minecraft.Core;

public static class Constants
{
    //General
    public const float CUBE_DIM = 1.0F;
    public const float HALF_CUBE_DIM = CUBE_DIM / 2.0F;
    public const int NUM_SECTIONS_IN_CHUNKS = 16;
    public const int MAX_BUILD_HEIGHT = NUM_SECTIONS_IN_CHUNKS * 16;

    //Physics
    public const float GRAVITY = -475F;

    /// <summary>The downwards pull a fall builds up to, so that falling stays within what collision can follow.</summary>
    public const float MAX_FALL_SPEED = -1000F;

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
