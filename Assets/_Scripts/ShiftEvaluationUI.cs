using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the shift evaluation UI with responsive layout
/// Works on any 16:9 resolution (1600x900, 1920x1080, etc)
/// </summary>
public class ShiftEvaluationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject evaluationPanel;
    [SerializeField] private TextMeshProUGUI classificationText;
    [SerializeField] private TextMeshProUGUI reportText;
    [SerializeField] private TextMeshProUGUI observationsText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("UI Settings")]
    [SerializeField] private Color excellentColor = new Color(0.2f, 1f, 0.2f);    // Green
    [SerializeField] private Color goodColor = new Color(0.4f, 0.8f, 1f);         // Cyan
    [SerializeField] private Color adequateColor = new Color(1f, 1f, 0.3f);       // Yellow
    [SerializeField] private Color poorColor = new Color(1f, 0.6f, 0.2f);         // Orange
    [SerializeField] private Color badColor = new Color(1f, 0.2f, 0.2f);          // Red

    [SerializeField] private float typewriterCharsPerSecond = 48f;
    private Coroutine typewriterRoutine;
    
    private bool isShowing = false;

    void Start()
    {
        SetupCanvas();
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (evaluationPanel != null)
        {
            evaluationPanel.SetActive(false);
        }
        
        // Unlock cursor when showing evaluation
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Setup canvas for responsive 16:9 scaling
    /// </summary>
    void SetupCanvas()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
        
        if (canvas != null)
        {
            // Ensure Canvas Scaler is set correctly
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);  // 16:9 reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;  // Balance between width and height
            scaler.referencePixelsPerUnit = 100;
            
            Debug.Log($"[ShiftEvaluationUI] Canvas configured for 16:9 scaling");
        }
    }

    /// <summary>
    /// Show the evaluation results
    /// </summary>
    public void ShowEvaluation(ShiftEvaluation evaluation)
    {
        if (evaluationPanel == null)
        {
            Debug.LogError("[ShiftEvaluationUI] Evaluation panel not assigned!");
            return;
        }

        isShowing = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;

        if (classificationText != null)
        {
            classificationText.text = evaluation.classification;
            classificationText.color = GetClassificationColor(evaluation.overallScore);
        }

        // Typewriter report text (realtime because timescale is 0 during review)
        if (reportText != null)
        {
            if (typewriterRoutine != null)
                StopCoroutine(typewriterRoutine);

            typewriterRoutine = StartCoroutine(TypewriteReport(evaluation.message));
        }

        if (observationsText != null)
        {
            if (evaluation.observations != null && evaluation.observations.Count > 0)
            {
                string obsText = "OBSERVATIONS:\n";
                foreach (string obs in evaluation.observations)
                    obsText += $"• {obs}\n";
                observationsText.text = obsText;
            }
            else
            {
                observationsText.text = "OBSERVATIONS:\nNone.";
            }
        }

        evaluationPanel.SetActive(true);
        Debug.Log($"[ShiftEvaluationUI] Showing evaluation: {evaluation.classification}");
    }

    private IEnumerator TypewriteReport(string fullText)
    {
        if (reportText == null)
            yield break;

        reportText.text = "";
        float delay = 1f / Mathf.Max(0.01f, typewriterCharsPerSecond);

        int n = fullText != null ? fullText.Length : 0;
        for (int i = 0; i <= n; i++)
        {
            reportText.text = fullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(delay);
        }

        typewriterRoutine = null;
    }

    public void HideEvaluation()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        isShowing = false;

        if (evaluationPanel != null)
            evaluationPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[ShiftEvaluationUI] Evaluation hidden");
    }

    /// <summary>
    /// Get color based on score
    /// </summary>
    Color GetClassificationColor(float score)
    {
        if (score >= 0.85f) return excellentColor;
        if (score >= 0.70f) return goodColor;
        if (score >= 0.55f) return adequateColor;
        if (score >= 0.40f) return poorColor;
        return badColor;
    }

    /// <summary>
    /// Called when continue button is clicked
    /// </summary>
    void OnContinueClicked()
    {
        HideEvaluation();
        
        // Tell StationManager to reset for next shift
        if (StationManager.Instance != null)
        {
            StationManager.Instance.ContinueToNextShift();
        }
    }

    /// <summary>
    /// Check if evaluation is currently showing
    /// </summary>
    public bool IsShowing => isShowing;
}