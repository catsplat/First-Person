using System;
public static class MovementEvents
{
    public static Action OnDashStart;
    public static Action OnDashEnd;
    public static Action<float> OnDashCooldownUpdate; // remaining / cooldown
    public static Action OnStartSprint;
    public static Action OnStopSprint;
    public static Action OnSlideStart;
    public static Action OnSlideEnd;
    public static Action OnWallRunStart;
    public static Action OnWallRunEnd;
    public static Action OnLandHard; // landing roll
}