using Minecraft.Core.Render;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Player;

public sealed class PlayerCameraRig
{
    private const float RunningFieldOfViewMultiplier = 1.10F;
    private const float CrouchingFieldOfViewMultiplier = 0.97F;

    private const float ThirdPersonDistance = 4.0F;

    private const float ThirdPersonWallMargin = 0.25F;

    private const float ThirdPersonStep = 0.1F;

    public Camera Camera { get; }

    public CameraPerspective Perspective { get; private set; } = CameraPerspective.FirstPerson;

    public bool IsBodyVisible => Perspective != CameraPerspective.FirstPerson;

    public Vector3 EyePosition { get; private set; }

    public PlayerCameraRig(ProjectionMatrixInfo projectionInfo)
    {
        Camera = new Camera(projectionInfo);
    }

    public void SetDefaultFieldOfView(float fieldOfViewRadians) => Camera.SetDefaultFieldOfView(fieldOfViewRadians);

    public void OnRunningToggle(bool isRunning) => ApplyFieldOfViewMultiplier(isRunning, RunningFieldOfViewMultiplier);

    public void OnCrouchingToggle(bool isCrouching) => ApplyFieldOfViewMultiplier(isCrouching, CrouchingFieldOfViewMultiplier);

    private void ApplyFieldOfViewMultiplier(bool active, float multiplier)
    {
        if (active)
        {
            Camera.SetFieldOfView(Camera.DefaultFieldOfView * multiplier);
        }
        else
        {
            Camera.SetFieldOfViewToDefault();
        }
    }

    public void CyclePerspective()
    {
        Perspective = Perspective switch
        {
            CameraPerspective.FirstPerson => CameraPerspective.ThirdPersonBack,
            CameraPerspective.ThirdPersonBack => CameraPerspective.ThirdPersonFront,
            _ => CameraPerspective.FirstPerson,
        };
    }

    public void Reset()
    {
        Perspective = CameraPerspective.FirstPerson;
        Camera.SetViewReversed(false);
    }

    public void UpdatePosition(World world, Vector3 playerPosition)
    {
        Vector3 eye = playerPosition;
        eye.X += Constants.PLAYER_WIDTH / 2.0F;
        eye.Y += Constants.PLAYER_CAMERA_HEIGHT;
        eye.Z += Constants.PLAYER_LENGTH / 2.0F;
        EyePosition = eye;

        bool front = Perspective == CameraPerspective.ThirdPersonFront;
        Camera.SetViewReversed(front);

        if (Perspective == CameraPerspective.FirstPerson)
        {
            Camera.SetPosition(eye);
            return;
        }

        Vector3 away = front ? Camera.LookDirection : -Camera.LookDirection;
        Camera.SetPosition(eye + away * FindUnobstructedCameraDistance(world, eye, away));
    }

    private static float FindUnobstructedCameraDistance(World world, Vector3 eye, Vector3 direction)
    {
        for (float distance = ThirdPersonStep; distance <= ThirdPersonDistance; distance += ThirdPersonStep)
        {
            if (!IsPositionSolid(world, eye + direction * distance))
            {
                continue;
            }

            return Math.Max(0F, distance - ThirdPersonStep - ThirdPersonWallMargin);
        }

        return ThirdPersonDistance;
    }

    private static bool IsPositionSolid(World world, Vector3 position)
    {
        Vector3i blockPos = position.ToBlockPos();
        if (world.IsOutsideBuildHeight(blockPos.Y))
        {
            return false;
        }

        BlockState state = world.GetBlockAt(blockPos);
        return state.GetBlock().GetCollisionBox(state, blockPos).Length > 0;
    }
}
