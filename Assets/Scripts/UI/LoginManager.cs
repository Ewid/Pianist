using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApiData;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private GameObject loginPanel;

    void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonPressed);
        }
        else
        {
            Debug.LogError("Login Button is not assigned in the Inspector!");
        }

        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    private void OnLoginButtonPressed()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please enter both email and password.", true);
            return;
        }

        SetStatus("Logging in...", false);
        SetInteractable(false);

        ApiService.Instance.Login(email, password, HandleLoginSuccess, HandleLoginError);
    }

    private void HandleLoginSuccess(AuthResponse response)
    {
        SetStatus($"Login successful! Welcome {response.user.firstName}", false);
        SetInteractable(true);
        Debug.Log($"Login Successful. Token: {response.token}");
    }

    private void HandleLoginError(string errorMessage)
    {
        SetStatus($"Login Failed: {errorMessage}", true);
        SetInteractable(true);
        Debug.LogError($"Login Failed: {errorMessage}");
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.black;
        }
    }

    private void SetInteractable(bool isInteractable)
    {
        if(loginButton != null) loginButton.interactable = isInteractable;
        if(emailInputField != null) emailInputField.interactable = isInteractable;
        if(passwordInputField != null) passwordInputField.interactable = isInteractable;
    }

    // Optional: Method to switch to the registration panel
    public void ShowRegistrationPanel()
    {
        if(loginPanel != null) loginPanel.SetActive(false);
        if(registrationPanel != null) registrationPanel.SetActive(true);
    }
} 