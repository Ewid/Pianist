using UnityEngine;
using UnityEngine.Networking; 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ApiData;
using Proyecto26;
using UnityEditor;
using RSG;

public class ApiService : MonoBehaviour
{
    private static ApiService _instance;
    public static ApiService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ApiService>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(nameof(ApiService));
                    _instance = singletonObject.AddComponent<ApiService>();
                }
            }
            return _instance;
        }
    }

    private string baseUrl = "http://localhost:3000"; 
    private const string AuthTokenKey = "authToken"; 

    public UserData CurrentUser { get; private set; }
    public string AuthToken { get; private set; }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        AuthToken = PlayerPrefs.GetString(AuthTokenKey, null);
        if (!string.IsNullOrEmpty(AuthToken))
        {
            Debug.Log("Auth Token loaded from PlayerPrefs.");
        }
    }

    private string GetStoredToken()
    {
        return PlayerPrefs.GetString(AuthTokenKey, null);
    }

    private void StoreToken(string token)
    {
        AuthToken = token;
        PlayerPrefs.SetString(AuthTokenKey, token);
        PlayerPrefs.Save();
        Debug.Log("Auth Token stored.");
    }

    public void ClearToken()
    {
        AuthToken = null;
        CurrentUser = null;
        PlayerPrefs.DeleteKey(AuthTokenKey);
        PlayerPrefs.Save();
        Debug.Log("Auth Token cleared.");
    }

    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(AuthToken);
    }


    private RequestHelper GetRequestOptions(bool requiresAuth = false, string body = null, List<IMultipartFormSection> formSections = null)
    {
        var options = new RequestHelper
        {
            Uri = "",
            Headers = new Dictionary<string, string>(),
            BodyString = body,
            FormSections = formSections,
            EnableDebug = true
        };

        if (requiresAuth)
        {
            string token = GetStoredToken();
            if (!string.IsNullOrEmpty(token))
            {
                options.Headers.Add("Authorization", "Bearer " + token);
            }
            else
            {
                Debug.LogWarning("Attempting authenticated request without a token.");
            }
        }
        return options;
    }

    private void HandleError(Exception exception, Action<string> onError)
    {
        string errorMsg = $"Request Error: {exception?.Message ?? "Unknown error"}";
        string serverErrorMsg = "";
        long statusCode = 0;

        if (exception is RequestException requestException)
        {
            statusCode = requestException.StatusCode;
            errorMsg = $"Error ({statusCode}): {requestException.Message}";
            
            if (requestException.Response != null && !string.IsNullOrEmpty(requestException.Response))
            {
                try
                { 
                    ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(requestException.Response); 
                    if(errorResponse != null && !string.IsNullOrEmpty(errorResponse.message)) 
                    {
                        serverErrorMsg = errorResponse.message;
                    }
                } 
                catch {}
                
                errorMsg += $" - Server: {(string.IsNullOrEmpty(serverErrorMsg) ? requestException.Response : serverErrorMsg)}";
            }
            else
            {
                 errorMsg += " (No response body)";
            }
        }
        
        Debug.LogError(errorMsg);

        if (statusCode == 401 || statusCode == 403)
        {
            ClearToken();
            onError?.Invoke("Authentication failed or token expired. Please log in again.");
        }
        else
        {
            onError?.Invoke(string.IsNullOrEmpty(serverErrorMsg) ? (exception?.Message ?? "Network or server error occurred.") : serverErrorMsg);
        }
    }


    public void Register(string firstName, string lastName, string email, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        RegisterRequest data = new RegisterRequest { firstName = firstName, lastName = lastName, email = email, password = password };
        string jsonBody = JsonUtility.ToJson(data);
        RequestHelper options = GetRequestOptions(body: jsonBody);
        options.Uri = baseUrl + "/api/auth/register";

        RestClient.Post<AuthResponse>(options).Then(response => {
            if (response != null && response.token != null)
            {
                StoreToken(response.token);
                CurrentUser = response.user;
                onSuccess?.Invoke(response);
            }
            else
            {
                 Debug.LogError("Register response or token was null.");
                 onError?.Invoke("Received invalid response from server during registration.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void Login(string email, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        LoginRequest data = new LoginRequest { email = email, password = password };
        string jsonBody = JsonUtility.ToJson(data);
        RequestHelper options = GetRequestOptions(body: jsonBody);
        options.Uri = baseUrl + "/api/auth/login";

        RestClient.Post<AuthResponse>(options).Then(response => {
             if (response != null && response.token != null)
            {
                StoreToken(response.token);
                CurrentUser = response.user;
                onSuccess?.Invoke(response);
            }
             else
            {
                 Debug.LogError("Login response or token was null.");
                 onError?.Invoke("Received invalid response from server during login.");
            }
        }).Catch(err => HandleError(err, onError));
    }


    public void GetSongs(Action<List<SongData>> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/songs";

        RestClient.Get<List<SongData>>(options).Then(responseList => { 
            if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 Debug.LogError("GetSongs response list was null. Check RestClient implementation or server response.");
                 onError?.Invoke("Received invalid song list from server.");
            }
        }).Catch(err => HandleError(err, onError));
    }


    public void GetUserProfile(Action<UserData> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/users/profile";

        RestClient.Get<UserData>(options).Then(response => {
             if(response != null) 
             {
                 CurrentUser = response;
                 onSuccess?.Invoke(response);
             }
             else
            {
                 Debug.LogError("GetUserProfile response was null.");
                 onError?.Invoke("Received invalid profile data from server.");
            }
         }).Catch(err => HandleError(err, onError));
    }

    public void UpdateUserProfile(UserProfileUpdateRequest updateData, Action<UserData> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        string jsonBody = JsonUtility.ToJson(updateData);
        RequestHelper options = GetRequestOptions(requiresAuth: true, body: jsonBody);
        options.Uri = baseUrl + "/api/users/profile";

        RestClient.Put<UserData>(options).Then(response => {
            if(response != null)
            {
                 CurrentUser = response;
                 onSuccess?.Invoke(response);
            }
             else
            {
                 Debug.LogError("UpdateUserProfile response was null.");
                 onError?.Invoke("Received invalid profile data after update.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void UpdateProgress(int songId, ProgressUpdateRequest updateData, Action<ProgressData> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        string jsonBody = JsonUtility.ToJson(updateData);
        RequestHelper options = GetRequestOptions(requiresAuth: true, body: jsonBody);
        options.Uri = baseUrl + $"/api/progress/{songId}";

        RestClient.Post<ProgressData>(options).Then(response => {
             if(response != null)
             {
                onSuccess?.Invoke(response);
             }
             else
            {
                 Debug.LogError("UpdateProgress response was null.");
                 onError?.Invoke("Received invalid progress data after update.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetAllProgress(Action<List<ProgressData>> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/progress";

        RestClient.Get<List<ProgressData>>(options).Then(responseList => {
             if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 Debug.LogError("GetAllProgress response list was null.");
                 onError?.Invoke("Received invalid progress list from server.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetSongProgress(int songId, Action<ProgressData> onSuccess, Action<string> onError)
    {
         if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + $"/api/progress/{songId}";

         RestClient.Get<ProgressData>(options).Then(response => {
            if(response != null)
             {
                onSuccess?.Invoke(response);
             }
             else
            {
                 Debug.LogError("GetSongProgress response was null.");
                 onError?.Invoke("Received invalid progress data for song.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void UploadPerformance(int songId, byte[] fileData, string fileName, float? score, Action<PerformanceData> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        
        List<IMultipartFormSection> formSections = new List<IMultipartFormSection>();
        formSections.Add(new MultipartFormDataSection("songId", songId.ToString()));
        if(score.HasValue)
        {
             formSections.Add(new MultipartFormDataSection("score", score.Value.ToString()));
        }
        formSections.Add(new MultipartFormFileSection("recordingFile", fileData, fileName, "application/octet-stream"));

        RequestHelper options = GetRequestOptions(requiresAuth: true, formSections: formSections);
        options.Uri = baseUrl + "/api/performances";
       
        RestClient.Post<PerformanceData>(options).Then(response => {
             if(response != null)
             {
                onSuccess?.Invoke(response);
             }
             else
            {
                 Debug.LogError("UploadPerformance response was null.");
                 onError?.Invoke("Received invalid response after performance upload.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetPerformances(Action<List<PerformanceData>> onSuccess, Action<string> onError)
    {
         if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/performances";

        RestClient.Get<List<PerformanceData>>(options).Then(responseList => {
             if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 Debug.LogError("GetPerformances response list was null.");
                 onError?.Invoke("Received invalid performance list from server.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void DeletePerformance(int performanceId, Action<ResponseHelper> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + $"/api/performances/{performanceId}";

        RestClient.Delete(options).Then(response => {
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                Debug.Log($"Delete Performance {performanceId} successful (Status: {response.StatusCode})");
                onSuccess?.Invoke(response);
            }
            else
            {
                string errMsg = $"Delete failed with status {response.StatusCode}";
                if (!string.IsNullOrEmpty(response.Text)) errMsg += $": {response.Text}";
                Debug.LogError(errMsg);
                onError?.Invoke(response.Error ?? errMsg);
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetAchievementDefinitions(Action<List<AchievementDefinition>> onSuccess, Action<string> onError)
    {

        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/achievements/definitions";

         RestClient.Get<List<AchievementDefinition>>(options).Then(responseList => {
            if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 Debug.LogError("GetAchievementDefinitions response list was null.");
                 onError?.Invoke("Received invalid achievement definitions from server.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetUserAchievements(Action<List<UserAchievement>> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/achievements";

         RestClient.Get<List<UserAchievement>>(options).Then(responseList => {
             if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 Debug.LogError("GetUserAchievements response list was null.");
                 onError?.Invoke("Received invalid user achievements list from server.");
            }
        }).Catch(err => HandleError(err, onError));
    }
} 