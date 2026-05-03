using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The AI station manager that evaluates player performance.
/// Policy-driven, efficiency-focused, indifferent to player comfort.
/// Not evil, not emotional - just corporate optimization.
/// </summary>
public class AIManager : MonoBehaviour
{
    [Header("AI Personality")]
    [SerializeField] private float strictnessLevel = 1.0f;  // Increases over time
    [SerializeField] private float toleranceThreshold = 0.7f;  // Decreases over time
    
    [Header("Evaluation Weights")]
    [SerializeField] private float completionWeight = 0.4f;
    [SerializeField] private float efficiencyWeight = 0.3f;
    [SerializeField] private float timeWeight = 0.2f;
    [SerializeField] private float safetyWeight = 0.1f;
    
    [Header("Progression")]
    [SerializeField] private int shiftsCompleted = 0;
    [SerializeField] private float progressionRate = 0.05f;  // How fast standards increase
    
    // Classification thresholds (adjusted by strictness)
    private Dictionary<string, float> classificationThresholds;
    
    void Start()
    {
        InitializeThresholds();
    }
    
    void InitializeThresholds()
    {
        classificationThresholds = new Dictionary<string, float>
        {
            { "EXEMPLARY OPERATOR", 0.95f },
            { "EFFICIENT OPERATOR", 0.85f },
            { "ADEQUATE ASSET", 0.70f },
            { "ACCEPTABLE PERFORMANCE", 0.55f },
            { "SUBOPTIMAL BEHAVIOR", 0.40f },
            { "INEFFICIENT PROCESS", 0.25f },
            { "UNPRODUCTIVE SHIFT", 0.0f }
        };
    }
    
    /// <summary>
    /// Evaluate a completed shift and return classification
    /// </summary>
    public ShiftEvaluation EvaluateShift(ShiftMetrics metrics)
    {
        ShiftEvaluation evaluation = new ShiftEvaluation();
        
        // Calculate sub-scores
        float completionScore = CalculateCompletionScore(metrics);
        float efficiencyScore = CalculateEfficiencyScore(metrics);
        float timeScore = CalculateTimeScore(metrics);
        float safetyScore = CalculateSafetyScore(metrics);
        
        // Weighted overall score
        evaluation.overallScore = 
            (completionScore * completionWeight) +
            (efficiencyScore * efficiencyWeight) +
            (timeScore * timeWeight) +
            (safetyScore * safetyWeight);
        
        // Apply strictness modifier
        evaluation.overallScore *= (1.0f / strictnessLevel);
        evaluation.overallScore = Mathf.Clamp01(evaluation.overallScore);
        
        // Store sub-scores
        evaluation.completionScore = completionScore;
        evaluation.efficiencyScore = efficiencyScore;
        evaluation.timeScore = timeScore;
        evaluation.safetyScore = safetyScore;
        
        // Determine classification
        evaluation.classification = DetermineClassification(evaluation.overallScore);
        
        // Generate evaluation message
        evaluation.message = GenerateEvaluationMessage(metrics, evaluation);
        
        // Generate observations (AI notices patterns)
        evaluation.observations = GenerateObservations(metrics, evaluation);
        
        return evaluation;
    }
    
    float CalculateCompletionScore(ShiftMetrics metrics)
    {
        if (metrics.tasksAttempted <= 0) return 0f;
        
        float completionRate = (float)metrics.tasksCompleted / metrics.tasksAttempted;
        float abandonmentPenalty = metrics.tasksAbandoned * 0.1f;
        
        return Mathf.Clamp01(completionRate - abandonmentPenalty);
    }
    
    float CalculateEfficiencyScore(ShiftMetrics metrics)
    {
        if (metrics.resourcesConsumed <= 0) return 0f;
        
        // Ideal ratio is 3:1 (earn $3 for every $1 resource used)
        float ratio = metrics.moneyEarned / metrics.resourcesConsumed;
        float idealRatio = 3.0f;
        
        return Mathf.Clamp01(ratio / idealRatio);
    }
    
    float CalculateTimeScore(ShiftMetrics metrics)
    {
        // Expected shift duration (10 minutes = 600 seconds)
        float expectedDuration = 600f;
        float duration = metrics.GetShiftDuration();
        
        // Optimal is completing close to expected time
        float deviation = Mathf.Abs(duration - expectedDuration) / expectedDuration;
        
        return Mathf.Clamp01(1.0f - deviation);
    }
    
    float CalculateSafetyScore(ShiftMetrics metrics)
    {
        // Each contamination event or health loss reduces score
        float contaminationPenalty = metrics.contaminationEvents * 0.2f;
        float healthPenalty = (metrics.healthLost / 100f) * 0.3f;
        
        return Mathf.Clamp01(1.0f - contaminationPenalty - healthPenalty);
    }
    
    string DetermineClassification(float score)
    {
        // Adjust thresholds based on strictness
        float modifier = strictnessLevel;
        
        foreach (var kvp in classificationThresholds)
        {
            float adjustedThreshold = kvp.Value * modifier;
            if (score >= adjustedThreshold)
            {
                return kvp.Key;
            }
        }
        
        return "UNPRODUCTIVE SHIFT";
    }
    
    string GenerateEvaluationMessage(ShiftMetrics metrics, ShiftEvaluation eval)
    {
        string msg = $"SHIFT EVALUATION REPORT\n";
        msg += $"Classification: {eval.classification}\n\n";
        
        // Task performance
        msg += $"TASK COMPLETION\n";
        msg += $"Tasks Attempted: {metrics.tasksAttempted}\n";
        msg += $"Tasks Completed: {metrics.tasksCompleted}\n";
        
        if (metrics.tasksAbandoned > 0)
        {
            msg += $"Tasks Abandoned: {metrics.tasksAbandoned}\n";
        }
        
        // Efficiency
        msg += $"\nRESOURCE EFFICIENCY\n";
        msg += $"Resources Consumed: {metrics.resourcesConsumed:F0} units\n";
        msg += $"Revenue Generated: ${metrics.moneyEarned:F0}\n";
        msg += $"Efficiency Ratio: {(metrics.moneyEarned / Mathf.Max(1, metrics.resourcesConsumed)):F2}:1\n";
        
        // Safety
        if (metrics.contaminationEvents > 0 || metrics.healthLost > 0)
        {
            msg += $"\nSAFETY PROTOCOLS\n";
            if (metrics.contaminationEvents > 0)
                msg += $"Protocol Violations: {metrics.contaminationEvents}\n";
            if (metrics.healthLost > 0)
                msg += $"Health Damage: {metrics.healthLost:F0} units\n";
        }
        
        return msg;
    }
    
    List<string> GenerateObservations(ShiftMetrics metrics, ShiftEvaluation eval)
    {
        List<string> observations = new List<string>();
        
        // Perfectionism
        if (metrics.tasksCompleted == metrics.tasksAttempted && metrics.tasksAttempted > 0)
        {
            observations.Add("Note: 100% completion rate observed.");
        }
        
        // Over-completion behavior
        if (eval.timeScore < 0.5f && eval.completionScore > 0.9f)
        {
            observations.Add("Note: Extended shift duration for completion.");
        }
        
        // Efficiency patterns
        if (eval.efficiencyScore < 0.5f)
        {
            observations.Add("Advisory: Resource consumption exceeds production value.");
        }
        
        // Safety concerns
        if (metrics.contaminationEvents > 2)
        {
            observations.Add("Warning: Multiple safety protocol violations detected.");
        }
        
        // Abandonment patterns
        if (metrics.tasksAbandoned > metrics.tasksCompleted)
        {
            observations.Add("Note: High task abandonment ratio detected.");
        }
        
        return observations;
    }
    
    /// <summary>
    /// Called after each shift to increase difficulty
    /// </summary>
    public void IncrementShiftProgression()
    {
        shiftsCompleted++;
        
        // Gradually increase strictness
        strictnessLevel += progressionRate;
        strictnessLevel = Mathf.Min(strictnessLevel, 3.0f);  // Cap at 3x
        
        // Gradually decrease tolerance
        toleranceThreshold -= progressionRate * 0.5f;
        toleranceThreshold = Mathf.Max(toleranceThreshold, 0.3f);  // Floor at 30%
        
        Debug.Log($"[AI Manager] Shift {shiftsCompleted} complete. Strictness: {strictnessLevel:F2}");
    }
    
    /// <summary>
    /// Get current AI state for display
    /// </summary>
    public string GetAIStatus()
    {
        return $"SYSTEM STATUS\n" +
               $"Shifts Monitored: {shiftsCompleted}\n" +
               $"Performance Standards: {GetStandardsLevel()}\n" +
               $"Efficiency Requirement: {(toleranceThreshold * 100):F0}%";
    }
    
    string GetStandardsLevel()
    {
        if (strictnessLevel < 1.2f) return "Standard";
        if (strictnessLevel < 1.5f) return "Elevated";
        if (strictnessLevel < 2.0f) return "High";
        if (strictnessLevel < 2.5f) return "Maximum";
        return "Critical";
    }
    
    // Getters for external systems
    public int ShiftsCompleted => shiftsCompleted;
    public float StrictnessLevel => strictnessLevel;
    public float ToleranceThreshold => toleranceThreshold;
}

/// <summary>
/// Data structure for shift evaluation results
/// </summary>
[System.Serializable]
public class ShiftEvaluation
{
    public float overallScore;
    public float completionScore;
    public float efficiencyScore;
    public float timeScore;
    public float safetyScore;
    public string classification;
    public string message;
    public List<string> observations = new List<string>();
}