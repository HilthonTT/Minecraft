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

    public static float LerpAngle(float from, float to, float t)
    {
        float difference = ((to - from + MathF.PI) % MathF.Tau + MathF.Tau) % MathF.Tau - MathF.PI;
        return from + difference * t;
    }

    public static float ConvertRange(float oldMin, float oldMax, float newMin, float newMax, float oldValue)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
    }
}
