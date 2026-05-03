using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    public static event System.Action<RoomController.RoomType> OnResourceFilled;
    public static event System.Action OnResourceChanged;
    public static event System.Action<float> OnTimerUpdate;

    // Public methods to invoke events (since events can only be invoked from within the class)
    public static void TriggerResourceFilled(RoomController.RoomType roomType)
    {
        OnResourceFilled?.Invoke(roomType);
    }

    public static void TriggerResourceChanged()
    {
        OnResourceChanged?.Invoke();
    }

    public static void TriggerTimerUpdate(float time)
    {
        OnTimerUpdate?.Invoke(time);
    }
}