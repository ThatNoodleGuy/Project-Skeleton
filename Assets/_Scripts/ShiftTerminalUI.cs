using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShiftTerminalUI : MonoBehaviour
{
    [SerializeField] private Button acceptShiftButton;
    [SerializeField] private TextMeshProUGUI shiftLabel;

    void Start()
    {
        if (acceptShiftButton != null)
            acceptShiftButton.onClick.AddListener(OnAcceptShiftClicked);
    }

    void OnDestroy()
    {
        if (acceptShiftButton != null)
            acceptShiftButton.onClick.RemoveListener(OnAcceptShiftClicked);
    }

    public void OnAcceptShiftClicked()
    {
        if (StationManager.Instance != null)
            StationManager.Instance.StartShift();
    }

    public void SetShiftLabel(string text)
    {
        if (shiftLabel != null)
            shiftLabel.text = text;
    }
}