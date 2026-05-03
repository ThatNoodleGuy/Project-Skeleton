using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks all metrics during a single shift for AI evaluation
/// </summary>
[System.Serializable]
public class ShiftMetrics
{
    [Header("Task Performance")]
    public int tasksAttempted = 0;
    public int tasksCompleted = 0;
    public int tasksAbandoned = 0;
    
    [Header("Resource Management")]
    public float resourcesConsumed = 0f;
    public float moneyEarned = 0f;
    
    [Header("Time Tracking")]
    public float shiftStartTime = 0f;
    public float shiftEndTime = 0f;
    
    [Header("Safety & Risk")]
    public int contaminationEvents = 0;
    public float healthLost = 0f;
    public int timerExpirations = 0;  // Times timer hit 0
    
    // Internal tracking
    private HashSet<RoomController.RoomType> roomsEntered = new HashSet<RoomController.RoomType>();
    private HashSet<RoomController.RoomType> roomsCompleted = new HashSet<RoomController.RoomType>();
    private Dictionary<RoomController.RoomType, float> roomTimeSpent = new Dictionary<RoomController.RoomType, float>();
    private Dictionary<RoomController.RoomType, float> roomEntryTime = new Dictionary<RoomController.RoomType, float>();
    
    /// <summary>
    /// Initialize a new shift
    /// </summary>
    public void StartShift()
    {
        shiftStartTime = Time.time;
        shiftEndTime = 0f;
        
        tasksAttempted = 0;
        tasksCompleted = 0;
        tasksAbandoned = 0;
        
        resourcesConsumed = 0f;
        moneyEarned = 0f;
        
        contaminationEvents = 0;
        healthLost = 0f;
        timerExpirations = 0;
        
        roomsEntered.Clear();
        roomsCompleted.Clear();
        roomTimeSpent.Clear();
        roomEntryTime.Clear();
        
        Debug.Log("[ShiftMetrics] Shift initialized");
    }
    
    /// <summary>
    /// Finalize shift timing
    /// </summary>
    public void EndShift()
    {
        shiftEndTime = Time.time;
        
        // Calculate final abandoned count
        // (rooms entered but not completed)
        tasksAbandoned = roomsEntered.Count - roomsCompleted.Count;
        
        Debug.Log($"[ShiftMetrics] Shift ended: {GetShiftDuration():F1}s, Score: {GetCompletionRate():P0}");
    }
    
    /// <summary>
    /// Record when player enters a room
    /// </summary>
    public void RecordRoomEntered(RoomController.RoomType roomType)
    {
        if (!roomsEntered.Contains(roomType))
        {
            roomsEntered.Add(roomType);
            roomEntryTime[roomType] = Time.time;
            tasksAttempted++;
            
            Debug.Log($"[ShiftMetrics] Room entered: {roomType} (Total: {tasksAttempted})");
        }
    }
    
    /// <summary>
    /// Record when player completes a room
    /// </summary>
    public void RecordRoomCompleted(RoomController.RoomType roomType)
    {
        if (!roomsCompleted.Contains(roomType))
        {
            roomsCompleted.Add(roomType);
            tasksCompleted++;
            
            // Calculate time spent in this room
            if (roomEntryTime.ContainsKey(roomType))
            {
                float timeSpent = Time.time - roomEntryTime[roomType];
                roomTimeSpent[roomType] = timeSpent;
            }
            
            Debug.Log($"[ShiftMetrics] Room completed: {roomType} (Total: {tasksCompleted})");
        }
    }
    
    /// <summary>
    /// Record when player exits room without completing
    /// </summary>
    public void RecordRoomExited(RoomController.RoomType roomType, bool wasCompleted)
    {
        if (!wasCompleted && roomsEntered.Contains(roomType) && !roomsCompleted.Contains(roomType))
        {
            // Calculate time spent before abandoning
            if (roomEntryTime.ContainsKey(roomType))
            {
                float timeSpent = Time.time - roomEntryTime[roomType];
                roomTimeSpent[roomType] = timeSpent;
            }
            
            Debug.Log($"[ShiftMetrics] Room abandoned: {roomType}");
        }
    }
    
    /// <summary>
    /// Record contamination event (timer reached 0)
    /// </summary>
    public void RecordContaminationEvent(RoomController.RoomType roomType)
    {
        contaminationEvents++;
        timerExpirations++;
        Debug.Log($"[ShiftMetrics] Contamination event in {roomType}! Total: {contaminationEvents}");
    }
    
    /// <summary>
    /// Record health damage
    /// </summary>
    public void RecordHealthLoss(float damage)
    {
        healthLost += damage;
    }
    
    /// <summary>
    /// Record resources consumed (power + oxygen)
    /// </summary>
    public void RecordResourcesConsumed(float power, float oxygen)
    {
        float total = power + oxygen;
        resourcesConsumed += total;
    }
    
    /// <summary>
    /// Record money earned from work
    /// </summary>
    public void RecordMoneyEarned(float amount)
    {
        moneyEarned += amount;
    }
    
    // ===== CALCULATIONS =====
    
    /// <summary>
    /// Get total shift duration in seconds
    /// </summary>
    public float GetShiftDuration()
    {
        if (shiftEndTime > 0)
            return shiftEndTime - shiftStartTime;
        else
            return Time.time - shiftStartTime;  // Ongoing
    }
    
    /// <summary>
    /// Get completion rate (0-1)
    /// </summary>
    public float GetCompletionRate()
    {
        if (tasksAttempted <= 0) return 0f;
        return (float)tasksCompleted / tasksAttempted;
    }
    
    /// <summary>
    /// Get abandonment rate (0-1)
    /// </summary>
    public float GetAbandonmentRate()
    {
        if (tasksAttempted <= 0) return 0f;
        return (float)tasksAbandoned / tasksAttempted;
    }
    
    /// <summary>
    /// Get resource efficiency ratio
    /// </summary>
    public float GetEfficiencyRatio()
    {
        if (resourcesConsumed <= 0) return 0f;
        return moneyEarned / resourcesConsumed;
    }
    
    /// <summary>
    /// Get average time per completed task
    /// </summary>
    public float GetAverageTaskTime()
    {
        if (tasksCompleted <= 0) return 0f;
        
        float totalTime = 0f;
        foreach (var time in roomTimeSpent.Values)
        {
            totalTime += time;
        }
        
        return totalTime / tasksCompleted;
    }
    
    /// <summary>
    /// Check if shift was perfect (no violations)
    /// </summary>
    public bool IsPerfectShift()
    {
        return contaminationEvents == 0 && 
               healthLost == 0 && 
               tasksAbandoned == 0 &&
               GetCompletionRate() == 1.0f;
    }
    
    /// <summary>
    /// Get detailed summary string
    /// </summary>
    public string GetSummary()
    {
        string summary = "=== SHIFT SUMMARY ===\n";
        summary += $"Duration: {GetShiftDuration():F1}s\n";
        summary += $"Tasks: {tasksCompleted}/{tasksAttempted} completed\n";
        summary += $"Abandoned: {tasksAbandoned}\n";
        summary += $"Resources: {resourcesConsumed:F0} consumed\n";
        summary += $"Revenue: ${moneyEarned:F0}\n";
        summary += $"Efficiency: {GetEfficiencyRatio():F2}:1\n";
        summary += $"Safety: {contaminationEvents} violations, {healthLost:F0} damage\n";
        summary += $"Perfect: {IsPerfectShift()}\n";
        
        return summary;
    }
}