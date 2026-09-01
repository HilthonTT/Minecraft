using Minecraft.Core.Physics;
using Minecraft.Core.Render;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class Camera
{
    private readonly ViewFrustum _viewFrustum;

    public Vector3 Position { get; private set; }

    public Vector3 Forward { get; private set; }

    public Vector3 LookDirection { get; private set; }

    public Vector3 Right { get; private set; }

    public bool IsViewReversed { get; private set; }

    public float Pitch { get; private set; }

    public float Yaw { get; private set; }

    public ProjectionMatrixInfo CurrentProjection { get; private set; }
    public Matrix4 CurrentProjectionMatrix { get; private set; }
    public Matrix4 CurrentViewMatrix { get; private set; }

    public delegate void OnProjectionChanged(ProjectionMatrixInfo info);

    public event OnProjectionChanged? OnProjectionChangedHandler;

    public Camera(ProjectionMatrixInfo projectionInfo)
    {
        DefaultFieldOfView = projectionInfo.FieldOfView;
        CurrentProjection = projectionInfo;
        CurrentProjectionMatrix = CreateProjectionMatrix();

        Position = new Vector3();

        SetPitchAndYaw(0, 0);
        _viewFrustum = new ViewFrustum(projectionInfo);
    }

    public bool IsAABBInViewFrustum(AxisAlignedBox aabb)
    {
        return _viewFrustum.IsAABBInFrustum(aabb);
    }

    public void SetFieldOfView(float fieldOfView)
    {
        if (CurrentProjection.FieldOfView != fieldOfView)
        {
            ProjectionMatrixInfo newProjectionInfo = CurrentProjection;
            newProjectionInfo.FieldOfView = fieldOfView;
            CurrentProjection = newProjectionInfo;

            CurrentProjectionMatrix = CreateProjectionMatrix();
            OnProjectionChangedHandler?.Invoke(CurrentProjection);
        }
    }

    public void SetFieldOfViewToDefault()
    {
        SetFieldOfView(DefaultFieldOfView);
    }

    public float DefaultFieldOfView { get; private set; }

    public void SetDefaultFieldOfView(float fieldOfView)
    {
        bool wasResting = CurrentProjection.FieldOfView == DefaultFieldOfView;
        DefaultFieldOfView = fieldOfView;

        if (wasResting)
        {
            SetFieldOfView(fieldOfView);
        }
    }

    public void SetWindowSize(int width, int height)
    {
        ProjectionMatrixInfo newProjectionInfo = CurrentProjection;
        newProjectionInfo.WindowPixelHeight = height;
        newProjectionInfo.WindowPixelWidth = width;
        CurrentProjection = newProjectionInfo;

        CurrentProjectionMatrix = CreateProjectionMatrix();
        OnProjectionChangedHandler?.Invoke(CurrentProjection);
    }

    private Matrix4 CreateProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            CurrentProjection.FieldOfView,
            CurrentProjection.WindowPixelWidth / (float)CurrentProjection.WindowPixelHeight,
            CurrentProjection.DistanceNearPlane,
            CurrentProjection.DistanceFarPlane);
    }

    private Matrix4 CreateViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Forward, Vector3.UnitY);
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void SetPitchAndYaw(float pitch, float yaw)
    {
        Pitch = pitch;
        Yaw = yaw;

        LookDirection = MathUtils.CreateLookAtVector(yaw, pitch);
        UpdateViewAxes();
    }

    public void SetViewReversed(bool reversed)
    {
        if (IsViewReversed == reversed)
        {
            return;
        }

        IsViewReversed = reversed;
        UpdateViewAxes();
    }

    private void UpdateViewAxes()
    {
        Forward = IsViewReversed ? -LookDirection : LookDirection;
        Right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Forward));
    }

    public void Update()
    {
        _viewFrustum.UpdateFrustumPoints(this);
        CurrentViewMatrix = CreateViewMatrix();
    }
}
