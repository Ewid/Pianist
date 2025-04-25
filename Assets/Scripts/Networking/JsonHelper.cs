using System;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJsonArray<T>(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Trim() == "[]")
        { 
            return new T[0];
        }

        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        if (wrapper == null || wrapper.array == null)
        {
            Debug.LogError($"Failed to parse JSON array or wrapper was null: {json}");
            return new T[0];
        }
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
} 