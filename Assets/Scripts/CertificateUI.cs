using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CertificateUI : MonoBehaviour
{
    public GameObject certificatePanel;
    public TextMeshProUGUI learnerNameText;
    public TextMeshProUGUI courseText;
    public Button closeButton;

    void Start()
    {
        if (certificatePanel != null) certificatePanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(CloseCertificate);


    }

    public void CloseCertificate()
    {
        certificatePanel.SetActive(false);
        SceneManager.LoadScene(0);
    }
    public void ShowCertificate(string learnerName = "Learner", string course = "Warehouse Safety Training")
    {
        Debug.Log("ShowCertificate");
        if (certificatePanel == null) return;
        learnerNameText.text = learnerName;
        courseText.text = course;
        certificatePanel.SetActive(true);
        Debug.Log("ShowCertificate2");

    }
}
