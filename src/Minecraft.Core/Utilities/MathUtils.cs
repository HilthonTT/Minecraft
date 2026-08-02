using OpenTK.Mathematics;

namespace Minecraft.Core.Utilities;

public static class MathUtils
{
    public static Matrix4 CreateTransformationMatrix(
        Vector3 translation,
        float rx = 0, float ry = 0, float rz = 0,
        float scaleX = 1, float scaleY = 1, float scaleZ = 1)
    {
        var scaleMatrix = new Matrix4(scaleX, 0, 0, 0, 0, scaleY, 0, 0, 0, 0, scaleZ, 0, 0, 0, 0, 1);
        return scaleMatrix * CreateRotationAndTranslationMatrix(translation, new Vector3(rx, ry, rz));
    }

    public static Matrix4 CreateRotationAndTranslationMatrix(Vector3 translation, Vector3 rotation)
    {
        return Matrix4.CreateRotationX(DegreeToRadian(rotation.X)) *
            Matrix4.CreateRotationY(DegreeToRadian(rotation.Y)) *
            Matrix4.CreateRotationZ(DegreeToRadian(rotation.Z)) *
            Matrix4.CreateTranslation(translation);
    }

    /// <summary>
    /// Builds a unit direction vector from angles in radians. Yaw turns around the Y axis,
    /// pitch tilts up and down.
    /// </summary>
    public static Vector3 CreateLookAtVector(float yaw, float pitch)
    {
        double cosPitch = Math.Cos(pitch);
        return new Vector3(
            (float)(Math.Sin(yaw) * cosPitch),
            (float)Math.Sin(pitch),
            (float)(Math.Cos(yaw) * cosPitch));
    }

    public static float DegreeToRadian(double angle)
    {
        return (float)(Math.PI * angle / 180.0);
    }

    public static float RadianToDegree(double angle)
    {
        return (float)(angle * (180.0 / Math.PI));
    }

    public static Vector3 Lerp(Vector3 from, Vector3 to, float t)
    {
        return from + (to - from) * t;
    }

    /// <summary>
    /// Interpolates between two angles in radians, turning whichever way round is shorter. Interpolating
    /// them as plain numbers would send anything crossing the wrap point spinning the long way instead.
    /// </summary>
    public static float LerpAngle(float from, float to, float t)
    {
        // Folded into (-pi, pi], which is the shorter of the two ways round by definition.
        float difference = ((to - from + MathF.PI) % MathF.Tau + MathF.Tau) % MathF.Tau - MathF.PI;
        return from + difference * t;
    }

    /// <summary>
    /// Converts from one range to another. Boundaries are all inclusive.
    /// </summary>
    public static float ConvertRange(float oldMin, float oldMax, float newMin, float newMax, float oldValue)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
    }
}
