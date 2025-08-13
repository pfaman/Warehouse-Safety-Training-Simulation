using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class QuizQuestion
{
    public string question;
    public string[] options;
    public int correctIndex;
}

public class QuizManager : MonoBehaviour
{
    public QuizQuestion[] questions;         // assign 3 questions in inspector
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;           // 4 buttons recommended (or match options count)
    public TextMeshProUGUI feedbackText;
    public Button submitButton;
    public GameObject certificatePanel;      // assign certificate UI panel (can be same as Scene2 CertificatePanel)
    public CertificateUI certificateUI;      // optional script to show certificate

    private int currentIndex = 0;
    private int[] selectedIndices;
    private int correctCount = 0;

    void Awake()
    {
        if (questions == null || questions.Length == 0) Debug.LogWarning("No quiz questions assigned.");
        selectedIndices = new int[questions.Length];
        for (int i = 0; i < selectedIndices.Length; i++) selectedIndices[i] = -1;
        feedbackText.text = "";
        gameObject.SetActive(false);
    }

    public void StartQuiz()
    {
        gameObject.SetActive(true);
        currentIndex = 0;
        correctCount = 0;
        ShowQuestion(currentIndex);
    }

    void ShowQuestion(int idx)
    {
        if (idx < 0 || idx >= questions.Length) return;
        var q = questions[idx];
        questionText.text = q.question;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
                int closureIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnSelectOption(closureIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
        feedbackText.text = $"Question {idx + 1} of {questions.Length}";
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmitAnswer);
    }

    void OnSelectOption(int optionIndex)
    {
        selectedIndices[currentIndex] = optionIndex;
        feedbackText.text = $"Selected option {optionIndex + 1}";
    }

    void OnSubmitAnswer()
    {
        // require selection
        if (selectedIndices[currentIndex] < 0)
        {
            feedbackText.text = "Please select an option.";
            return;
        }

        if (selectedIndices[currentIndex] == questions[currentIndex].correctIndex)
            correctCount++;

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
        gameObject.SetActive(false);
        if (correctCount >= 2)
        {
            // passed
            Debug.Log("Quiz passed: " + correctCount);
            if (certificateUI != null) certificateUI.ShowCertificate("Learner Name");
            // optionally call LMS
            if (LMSManager.Instance != null) LMSManager.Instance.SendProgress("CourseComplete", 100f);
        }
        else
        {
            // failed
            Debug.Log("Quiz failed: " + correctCount);
            // show retry prompt
            // for simplicity, re-open quiz
            ShowRetryPrompt();
        }
    }

    void ShowRetryPrompt()
    {
        // You can open a small dialog; for now restart quiz instantly
        StartQuiz();
    }
}
