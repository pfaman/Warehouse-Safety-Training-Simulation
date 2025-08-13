using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string question;
    public string[] options;
    public int correctIndex; // 0-based index into options
}

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    public QuizQuestion[] questions;        // assign 3 questions in inspector
    public int passThreshold = 2;           // need at least 2 correct to pass

    [Header("UI Refs")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;          // assign OptionButton0..N
    public TextMeshProUGUI feedbackText;
    public Button submitButton;
    public GameObject retryPanel;           // optional: set inactive
    public CertificateUI certificateUI;     // optional: show certificate on pass

    // runtime
    private int currentIndex = 0;
    private int selectedIndex = -1;
    private int correctCount = 0;

    // style colors
    private Color defaultButtonColor;
    private Color selectedButtonColor = new Color(0.15f, 0.6f, 0.95f);

    void Awake()
    {
        // panel should be inactive until opened by Scene2Manager
        gameObject.SetActive(false);

        if (optionButtons != null && optionButtons.Length > 0)
        {
            // cache default color (from first button)
            var img = optionButtons[0].GetComponent<Image>();
            if (img != null) defaultButtonColor = img.color;
        }

        if (submitButton != null)
            submitButton.onClick.RemoveAllListeners();
    }

    // Call this to start the quiz (Scene2Manager.OpenQuiz())
    public void StartQuiz()
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("QuizManager: no questions assigned.");
            return;
        }

        gameObject.SetActive(true);
        currentIndex = 0;
        selectedIndex = -1;
        correctCount = 0;
        feedbackText.text = $"Question 1 of {questions.Length}";
        if (retryPanel != null) retryPanel.SetActive(false);
        ShowQuestion(currentIndex);
    }

    void ShowQuestion(int idx)
    {
        if (idx < 0 || idx >= questions.Length) return;

        selectedIndex = -1;
        submitButton.interactable = false;

        var q = questions[idx];
        questionText.text = q.question;

        // populate buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                var label = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = q.options[i];

                // clear listeners then add
                int closureIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnSelectOption(closureIndex));

                // reset visual
                var img = optionButtons[i].GetComponent<Image>();
                if (img != null) img.color = defaultButtonColor;
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        feedbackText.text = $"Question {idx + 1} of {questions.Length}";
    }

    void OnSelectOption(int optionIdx)
    {
        selectedIndex = optionIdx;
        submitButton.interactable = true;

        // Visual feedback: highlight selected button
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var img = optionButtons[i].GetComponent<Image>();
            if (img != null) img.color = (i == optionIdx) ? selectedButtonColor : defaultButtonColor;
        }

        feedbackText.text = $"Selected option {optionIdx + 1}";
    }

    public void OnSubmitAnswer()
    {
        if (selectedIndex < 0)
        {
            feedbackText.text = "Please select an option.";
            return;
        }

        // evaluate
        if (selectedIndex == questions[currentIndex].correctIndex) correctCount++;

        // next or finish
        currentIndex++;
        if (currentIndex < questions.Length)
        {
            ShowQuestion(currentIndex);
        }
        else
        {
            FinishQuiz();
        }
    }

    void FinishQuiz()
    {
        Debug.Log("FinishQuiz");
        gameObject.SetActive(false);

        if (correctCount >= passThreshold)
        {
            Debug.Log($"Quiz passed ({correctCount}/{questions.Length})");
            // show certificate
            if (certificateUI != null) certificateUI.ShowCertificate("Learner Name");

            // LMS reporting (optional)
            if (LMSManager.Instance != null) LMSManager.Instance.SendProgress("QuizPassed", 100f);
        }
        else
        {
            Debug.Log($"Quiz failed ({correctCount}/{questions.Length})");
            // show retry UI or reopen quiz
            if (retryPanel != null)
            {
                retryPanel.SetActive(true);
            }
            else
            {
                // default: restart quiz immediately
                StartQuiz();
            }
        }
    }

    // Hook retry button to this
    public void OnRetry()
    {
        if (retryPanel != null) retryPanel.SetActive(false);
        StartQuiz();
    }

    // Optional exit (e.g., back to menu)
    public void OnExit()
    {
        if (retryPanel != null) retryPanel.SetActive(false);
        // implement scene change if desired, e.g. load menu
    }
}
