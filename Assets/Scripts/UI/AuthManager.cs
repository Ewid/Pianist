using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro;        // Required for TextMeshPro elements
using ApiData;      // Required for AuthResponse
using UnityEngine.SceneManagement; // Optional: For scene switching after login

public class AuthManager : MonoBehaviour
{
    [Header("Common Panels & Buttons")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public Button switchToRegisterButton; 
    public Button switchToLoginButton;

    [Header("Login Elements")]
    public TMP_InputField usernameInputLogin;
    public TMP_InputField passwordInputLogin;
    public Button loginButton;
    public TextMeshProUGUI feedbackTextLogin;

    [Header("Register Elements")]
    public TMP_InputField usernameInputRegister;
    public TMP_InputField passwordInputRegister;
    public TMP_InputField confirmPasswordInputRegister;
    public Button registerButton;
    public TextMeshProUGUI feedbackTextRegister;

    [Header("Navigation (Optional)")]
    public string sceneToLoadOnSuccess = "MainMenuScene"; // Example: Name of the scene to load after successful login/register

    void Start()
    {
        // Ensure Login panel is active, Register is inactive at start
        SwitchToLoginPanel(); 

        // Add listeners to buttons
        loginButton.onClick.AddListener(AttemptLogin);
        registerButton.onClick.AddListener(AttemptRegister);

        // Add listeners for the switch buttons (if they are assigned)
        if (switchToRegisterButton != null)
        {
            // This button should be on the Login Panel and switch TO the Register Panel
            switchToRegisterButton.onClick.AddListener(SwitchToRegisterPanel); 
        }
        else
        {
            Debug.LogWarning("SwitchToRegisterButton is not assigned in the AuthManager inspector.");
        }

        if (switchToLoginButton != null)
        {
            // This button should be on the Register Panel and switch TO the Login Panel
            switchToLoginButton.onClick.AddListener(SwitchToLoginPanel);
        }
         else
        {
            Debug.LogWarning("SwitchToLoginButton is not assigned in the AuthManager inspector.");
        }
        
    }

    public void SwitchToLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        ClearFeedback();
    }

    public void SwitchToRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        ClearFeedback();
    }

    private void AttemptLogin()
    {
        ClearFeedback();
        string username = usernameInputLogin.text;
        string password = passwordInputLogin.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Username and Password cannot be empty.");
            return;
        }

        SetInteractable(false);
        ApiService.Instance.Login(username, password, OnLoginSuccess, ShowLoginError);
    }

    private void AttemptRegister()
    {
        ClearFeedback();
        string username = usernameInputRegister.text;
        string password = passwordInputRegister.text;
        string confirmPassword = confirmPasswordInputRegister.text;

        // Basic Validation
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowRegisterError("Username and Password are required.");
            return;
        }
        if (password != confirmPassword)
        {
            ShowRegisterError("Passwords do not match.");
            return;
        }

        SetInteractable(false);
        ApiService.Instance.Register(username, password, OnRegisterSuccess, ShowRegisterError);
    }


    private void OnLoginSuccess(AuthResponse response)
    {
        SetInteractable(true);
        feedbackTextLogin.color = Color.green;
        feedbackTextLogin.text = $"Login Successful! Welcome {response.user?.username ?? "User"}!";
        Debug.Log("Login Successful. Token: " + response.token);
        

        if (!string.IsNullOrEmpty(sceneToLoadOnSuccess))
        {
            SceneManager.LoadScene(sceneToLoadOnSuccess);
        }
        else
        {
            Debug.LogWarning("SceneToLoadOnSuccess is not set in the AuthManager inspector.");
        }
    }

     private void ShowLoginError(string message)
    {
        SetInteractable(true);
        feedbackTextLogin.color = Color.red;
        feedbackTextLogin.text = "Login Failed: " + message;
        Debug.LogError("Login Error: " + message);
    }

    private void OnRegisterSuccess(AuthResponse response)
    {
        SetInteractable(true);
        feedbackTextRegister.color = Color.green;
        feedbackTextRegister.text = "Registration Successful! You can now log in.";
        Debug.Log("Registration Successful.");

        if (!string.IsNullOrEmpty(sceneToLoadOnSuccess))
        {
            SceneManager.LoadScene(sceneToLoadOnSuccess);
        }
        else
        {
            Debug.LogWarning("SceneToLoadOnSuccess is not set in the AuthManager inspector.");
        }
    }

    private void ShowRegisterError(string message)
    {
        SetInteractable(true);
        feedbackTextRegister.color = Color.red;
        feedbackTextRegister.text = "Registration Failed: " + message;
        Debug.LogError("Registration Error: " + message);
    }


    private void ClearFeedback()
    {
        feedbackTextLogin.text = "";
        feedbackTextRegister.text = "";
    }

    private void SetInteractable(bool isInteractable)
    {
        loginButton.interactable = isInteractable;
        registerButton.interactable = isInteractable;
    }
} 