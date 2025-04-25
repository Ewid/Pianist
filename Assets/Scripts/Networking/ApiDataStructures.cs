using System;
using System.Collections.Generic; // Needed for List<T>

namespace ApiData
{
    // --- Request Structures ---

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public string message;
        public UserData user;
        public string token;
    }

    [Serializable]
    public class SongData
    {
        public int songId;
        public string title;
        public string artist;
        public int difficulty;
    }

    [Serializable]
    public class ProgressData
    {
        public int songId;
        public string completionStatus;
    }

    [Serializable]
    public class ProgressUpdateRequest
    {
        public string completionStatus;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string message;
    }

    [Serializable]
    public class UserData
    {
        public int userId;
        public string username;
        public int skillLevel;
        public string accountStatus;
        public string creationDate;
    }
} 