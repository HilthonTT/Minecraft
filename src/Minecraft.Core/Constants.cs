namespace Minecraft.Core;

public static class Constants
{
    public const float CUBE_DIM = 1.0F;
    public const float HALF_CUBE_DIM = CUBE_DIM / 2.0F;
    public const int NUM_SECTIONS_IN_CHUNKS = 16;
    public const int MAX_BUILD_HEIGHT = NUM_SECTIONS_IN_CHUNKS * 16;

    public const int VIEW_DISTANCE_CHUNKS = 8;

    public const float VIEW_DISTANCE_BLOCKS = VIEW_DISTANCE_CHUNKS * 16;

    public const float FOG_START_FRACTION = 0.65F;

    public const float FOG_END_FRACTION = 1.0F;

    public const float GRAVITY = -475F;

    public const float MAX_FALL_SPEED = -1000F;

    public const float WATER_GRAVITY_MULTIPLIER = 0.16F;

    public const float MAX_SINK_SPEED = -55F;

    public const float SWIM_UP_FORCE = 145F;

    public const float WATER_MOVE_MULTIPLIER = 0.55F;

    public const float WATER_PUSH_FORCE = 14F;

    public const float MAX_FRAME_TIME_SECONDS = 0.1F;

    public const float PLAYER_HEIGHT = CUBE_DIM * 1.75F;
    public const float PLAYER_CAMERA_HEIGHT = CUBE_DIM * 1.5F;
    public const float PLAYER_WIDTH = HALF_CUBE_DIM;
    public const float PLAYER_LENGTH = HALF_CUBE_DIM;

    public const int PLAYER_MAX_HEALTH = 20;

    public const float PLAYER_HURT_SECONDS = 0.5F;

    public const float PLAYER_REGEN_DELAY_SECONDS = 6F;
    public const float PLAYER_REGEN_SECONDS_PER_HEALTH = 4F;

    public const float PLAYER_SAFE_FALL_BLOCKS = 3F;

    public const float PLAYER_BASE_MOVE_SPEED = 50F;
    public const float PLAYER_SPRINT_MULTIPLIER = 1.75F;
    public const float PLAYER_CROUCH_MULTIPLIER = 0.35F;
    public const float PLAYER_JUMP_FORCE = 115F;
    public const float PLAYER_STOP_FORCE_MULTIPLIER = 0.80F;
    public const float PLAYER_MOUSE_SENSIVITY = 0.0015F;
    public const float PLAYER_IN_AIR_SLOWDOWN = 0.75F;
    public const float PLAYER_FLYING_MULTIPLIER = 4.0F;
}
