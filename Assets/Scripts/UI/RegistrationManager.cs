using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApiData;

public class RegistrationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField firstNameInputField;
    [SerializeField] private TMP_InputField lastNameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_InputField confirmPasswordInputField;
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private GameObject loginPanel;

    void Start()
    {
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterButtonPressed);
        }
        else
        {
            Debug.LogError("Register Button is not assigned in the Inspector!");
        }

        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    private void OnRegisterButtonPressed()
    {
        string firstName = firstNameInputField.text;
        string lastName = lastNameInputField.text;
        string email = emailInputField.text;
        string password = passwordInputField.text;
        string confirmPassword = confirmPasswordInputField.text;

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SetStatus("Please fill in all fields.", true);
            return;
        }

        if (password != confirmPassword)
        {
            SetStatus("Passwords do not match.", true);
            return;
        }

        SetStatus("Registering...", false);
        SetInteractable(false);

        ApiService.Instance.Register(firstName, lastName, email, password, HandleRegisterSuccess, HandleRegisterError);
    }

    private void HandleRegisterSuccess(AuthResponse response)
    {
        SetStatus($"Registration successful! Welcome {response.user.firstName}. You are now logged in.", false);
        SetInteractable(true);
        Debug.Log($"Registration Successful. Token: {response.token}");
    }

    private void HandleRegisterError(string errorMessage)
    {
        SetStatus($"Registration Failed: {errorMessage}", true);
        SetInteractable(true);
        Debug.LogError($"Registration Failed: {errorMessage}");
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
        if(registerButton != null) registerButton.interactable = isInteractable;
        if(firstNameInputField != null) firstNameInputField.interactable = isInteractable;
        if(lastNameInputField != null) lastNameInputField.interactable = isInteractable;
        if(emailInputField != null) emailInputField.interactable = isInteractable;
        if(passwordInputField != null) passwordInputField.interactable = isInteractable;
        if(confirmPasswordInputField != null) confirmPasswordInputField.interactable = isInteractable;
    }

    public void ShowLoginPanel()
    {
        if(registrationPanel != null) registrationPanel.SetActive(false);
        if(loginPanel != null) loginPanel.SetActive(true);
    }
} 