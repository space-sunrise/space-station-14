namespace Content.Client.SurveillanceCamera;

[RegisterComponent]
public sealed partial class ActiveSurveillanceCameraMonitorVisualsComponent : Component
{
    public float TimeLeft = 0.5f; // Sunrise-edit

    public Action? OnFinish;
}
