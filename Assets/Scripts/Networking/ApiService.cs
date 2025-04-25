using UnityEngine;
using UnityEngine.Networking; 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ApiData;
using Proyecto26;
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
    //192.168.50.171
    private string baseUrl = "http://192.168.50.171:3000"; 
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
    }

    public void ClearToken()
    {
        AuthToken = null;
        CurrentUser = null;
        PlayerPrefs.DeleteKey(AuthTokenKey);
        PlayerPrefs.Save();
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
            EnableDebug = false
        };

        if (requiresAuth)
        {
            string token = GetStoredToken();
            if (!string.IsNullOrEmpty(token))
            {
                options.Headers.Add("Authorization", "Bearer " + token);
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
        }
        
        Debug.LogError(errorMsg);

        if (statusCode == 401 || statusCode == 403)
        {
            ClearToken();
            onError?.Invoke("Wrong username or password. Please try again.");
        }
        else
        {
            onError?.Invoke(string.IsNullOrEmpty(serverErrorMsg) ? (exception?.Message ?? "Network or server error occurred.") : serverErrorMsg);
        }
    }


    public void Register(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        RegisterRequest data = new RegisterRequest { username = username, password = password };
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
                 onError?.Invoke("Error during registration. Please try again.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void Login(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
    {
        LoginRequest data = new LoginRequest { username = username, password = password };
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
                 onError?.Invoke("Wrong username or password. Please try again.");
            }
        }).Catch(err => HandleError(err, onError));
    }


    public void GetSongs(Action<List<SongData>> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/songs";

        RestClient.GetArray<SongData>(options).Then(responseArray => { 
            List<SongData> responseList = new List<SongData>(responseArray ?? System.Array.Empty<SongData>());

            if (responseList != null)
            {
                onSuccess?.Invoke(responseList);
            }
             else
            {
                 onError?.Invoke("Error fetching songs. Please try again.");
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
                 onError?.Invoke("Error fetching profile data. Please try again.");
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
                 onError?.Invoke("Error updating progress. Please try again.");
            }
        }).Catch(err => HandleError(err, onError));
    }

    public void GetAllProgress(Action<List<ProgressData>> onSuccess, Action<string> onError)
    {
        if (!IsLoggedIn()) { onError?.Invoke("Not logged in."); return; }
        RequestHelper options = GetRequestOptions(requiresAuth: true);
        options.Uri = baseUrl + "/api/progress";

        RestClient.Get(options).Then(response => {
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                ProgressData[] progressArray = JsonHelper.FromJsonArray<ProgressData>(response.Text);
                if (progressArray != null)
                {
                    onSuccess?.Invoke(new List<ProgressData>(progressArray));
                }
                else
                {
                    onError?.Invoke("Error fetching progress data. Please try again.");
                }
            }
            else
            {
                string errMsg = $"Failed to get progress data. Status: {response.StatusCode}";
                if (!string.IsNullOrEmpty(response.Error))
                {
                    errMsg += $" Error: {response.Error}";
                }
                 else if (!string.IsNullOrEmpty(response.Text))
                {
                    errMsg += $" Response: {response.Text}";
                }
                 onError?.Invoke(errMsg);
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
                 onError?.Invoke("Error fetching progress data. Please try again.");
            }
        }).Catch(err => HandleError(err, onError));
    }
} 