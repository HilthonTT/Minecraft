using Minecraft.Core.Physics;
using Minecraft.Core.Render;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class Camera
{
    private readonly ViewFrustum _viewFrustum;

    public Vector3 Position { get; private set; }

    /// <summary>
    /// The direction this camera is actually looking, which is what the world is drawn and culled against.
    /// The same as <see cref="LookDirection"/> everywhere except the front facing third person view, where the
    /// camera stands in front of the player and looks back along it.
    /// </summary>
    public Vector3 Forward { get; private set; }

    /// <summary>
    /// The direction the pitch and yaw point in, which is where the player is looking rather than where this
    /// camera is looking. Reaching out at a block follows this, so a reversed view still aims forwards.
    /// </summary>
    public Vector3 LookDirection { get; private set; }

    public Vector3 Right { get; private set; }

    /// <summary>Whether the view runs back along the pitch and yaw rather than along them.</summary>
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

        // Set rather than left at zero: the view matrix is built from the forward vector, and a zero one has
        // no direction to look along, which would leave the first frame with a degenerate view.
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

    /// <summary>
    /// The field of view this camera settles back to, which everything that widens or narrows it — sprinting,
    /// crouching — works out from. Settable so the player's own choice becomes the resting point rather than
    /// something layered on top of the one the camera was built with.
    /// </summary>
    public float DefaultFieldOfView { get; private set; }

    /// <summary>
    /// Changes the resting field of view, taking the camera to it if it is not currently widened or narrowed
    /// by anything. Moving it while sprinting would fight with the sprint, which puts it back on its own.
    /// </summary>
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

    /// <summary>
    /// Turns the view around so that it looks back along the pitch and yaw rather than along them, which is
    /// the front facing third person view: the camera is out in front of the player, facing them.
    /// </summary>
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
