using UnityEngine;
using UnityEngine.UI;

public class ChoreTutorial : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text stepsText;
    [SerializeField] private Button continueButton;

    private System.Action onFinished;

    private void Start()
    {
        tutorialPanel.SetActive(false);

        continueButton.onClick.AddListener(FinishTutorial);
    }

    public void ShowTutorial(string title, string steps, System.Action finished)
    {
        titleText.text = title;
        stepsText.text = steps;

        onFinished = finished;

        tutorialPanel.SetActive(true);
    }

    private void FinishTutorial()
    {
        tutorialPanel.SetActive(false);

        onFinished?.Invoke();
        onFinished = null;
    }
}