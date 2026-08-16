namespace Minecraft.Core.Entities;

/// <summary>
/// Where the player is watched from. Cycled in this order, which is the order the key steps through them:
/// out of your own eyes, from over your shoulder, then from in front looking back at you.
/// </summary>
public enum CameraPerspective
{
    FirstPerson,
    ThirdPersonBack,
    ThirdPersonFront
}
