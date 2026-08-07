using Minecraft.Core.Entities;
using Minecraft.Core.Physics;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

/// <summary>
/// Representation of a view frustum used for culling objects.
/// </summary>
public sealed class ViewFrustum
{
    //Implementation based on: https://cgvr.cs.uni-bremen.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

    internal struct FrustumPlane
    {
        public Vector3 normal;
        public float distanceToOrigin;

        public readonly float GetSignedDistance(Vector3 point)
        {
            return Vector3.Dot(point, normal) - distanceToOrigin;
        }
    }

    private float _nearWidth, _nearHeight;
    private readonly FrustumPlane[] _frustumPlanes = new FrustumPlane[6];

    public ViewFrustum(ProjectionMatrixInfo projectionInfo)
    {
        CalculateNearWidthHeight(projectionInfo);
    }

    private void CalculateNearWidthHeight(ProjectionMatrixInfo pInfo)
    {
        float aspectRatio = pInfo.WindowPixelWidth / (float)pInfo.WindowPixelHeight;
        const float extesion = 2;
        float tan = (float)Math.Tan(pInfo.FieldOfView * 0.5F) * extesion;
        _nearHeight = tan * pInfo.DistanceNearPlane;
        _nearWidth = _nearHeight * aspectRatio;
    }

    public void UpdateFrustumPoints(Camera camera)
    {
        Vector3 zAxis = -camera.Forward;
        Vector3 xAxis = camera.Right;
        Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

        Vector3 nearCenter = camera.Position - zAxis * camera.CurrentProjection.DistanceNearPlane;
        Vector3 farCenter = camera.Position - zAxis * camera.CurrentProjection.DistanceFarPlane;

        _frustumPlanes[0].normal = -zAxis; //near plane
        _frustumPlanes[0].distanceToOrigin = Vector3.Dot(_frustumPlanes[0].normal, nearCenter);

        _frustumPlanes[1].normal = zAxis; //far plane
        _frustumPlanes[1].distanceToOrigin = Vector3.Dot(_frustumPlanes[1].normal, farCenter);

        Vector3 temporary, normal;

        temporary = (nearCenter + yAxis * _nearHeight) - camera.Position;
        temporary.Normalize();
        normal = Vector3.Cross(temporary, xAxis);
        _frustumPlanes[2].normal = normal; //top plane
        _frustumPlanes[2].distanceToOrigin = Vector3.Dot(_frustumPlanes[2].normal, nearCenter + yAxis * _nearHeight);

        temporary = (nearCenter - yAxis * _nearHeight) - camera.Position;
        temporary.Normalize();
        normal = Vector3.Cross(xAxis, temporary);
        _frustumPlanes[3].normal = normal; //bottom plane
        _frustumPlanes[3].distanceToOrigin = Vector3.Dot(_frustumPlanes[3].normal, nearCenter - yAxis * _nearHeight);

        temporary = (nearCenter - xAxis * _nearWidth) - camera.Position;
        temporary.Normalize();
        normal = Vector3.Cross(temporary, yAxis);
        _frustumPlanes[4].normal = normal; //left plane
        _frustumPlanes[4].distanceToOrigin = Vector3.Dot(_frustumPlanes[4].normal, nearCenter - xAxis * _nearWidth);

        temporary = (nearCenter + xAxis * _nearWidth) - camera.Position;
        temporary.Normalize();
        normal = Vector3.Cross(yAxis, temporary);
        _frustumPlanes[5].normal = normal; //right plane
        _frustumPlanes[5].distanceToOrigin = Vector3.Dot(_frustumPlanes[5].normal, nearCenter + xAxis * _nearWidth);
    }

    public bool IsAABBInFrustum(AxisAlignedBox aabb)
    {
        Vector3[] corners = aabb.GetAllCorners();
        for (int i = 0; i < 6; i++)
        {
            int inside = 0;
            foreach (Vector3 corner in corners)
            {
                if (_frustumPlanes[i].GetSignedDistance(corner) >= 0)
                {
                    inside++;
                }
            }
            if (inside == 0)
            {
                return false;
            }
        }
        return true;
    }
}
