using System;


namespace ApiData
{
    [Serializable]
    public class RegisterRequest
    {
        public string firstName;
        public string lastName;
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginRequest
    {
        public string email;
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
    public class UserData
    {
        public int userId;
        public string firstName;
        public string lastName;
        public string email;
        public int skillLevel;
        public string accountStatus;
        public string creationDate;
        public int points;
    }

    [Serializable]
    public class UserProfileUpdateRequest
    {
        public string firstName;
        public string lastName;
        public int? skillLevel;
    }


    [Serializable]
    public class SongData
    {
        public int songId;
        public string title;
        public string artist;
        public int difficulty;
        public string category;
        public string filePath;
        public string audioPreviewPath;

        public bool isUnlocked;
        public string completionStatus;
        public float? accuracyScore;
        public bool isFreeModeUnlocked;
    }

    [Serializable]
    public class ProgressData
    {
        public int progressId;
        public int userId;
        public int songId;
        public string completionStatus;
        public float? accuracyScore;
        public string lastPlayedDate;
        public int playCount;
    }

    [Serializable]
    public class ProgressUpdateRequest
    {
        public string completionStatus;
        public float? accuracyScore;
    }

    [Serializable]
    public class PerformanceData
    {
        public int performanceId;
        public int userId;
        public int songId;
        public string filePath;
        public float? score;
        public string recordedDate;
    }

    [Serializable]
    public class AchievementDefinition
    {
        public int achievementDefId;
        public string name;
        public string description;
        public string criteriaType;
        public int criteriaValue;
        public string badgeIconPath;
    }

    [Serializable]
    public class UserAchievement
    {
        public int userAchievementId;
        public int userId;
        public int achievementDefId;
        public string dateEarned;
        public string name;
        public string description;
        public string badgeIconPath;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string message;
    }
} 