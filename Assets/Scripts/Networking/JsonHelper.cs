using System;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJsonArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        if (wrapper == null)
        {
            Debug.LogError($"Failed to parse JSON array: {json}");
            return null;
        }
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
} 