using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SoundGeneration : MonoBehaviour
{
    AudioSource audioSource;

    public List<AudioClip> sampleClips;

    private Dictionary<int, AudioClip> noteToClipMap;


    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        noteToClipMap = new Dictionary<int, AudioClip>();

        if (sampleClips == null || sampleClips.Count == 0)
        {
            Debug.LogError("No sample clips assigned in the Inspector");
            return;
        }

        foreach (AudioClip clip in sampleClips)
        {
            if (clip == null) continue;

            try
            {
                string[] nameParts = clip.name.Split('-');
                string notePart = nameParts.LastOrDefault()?.Split('.').FirstOrDefault();

                if (!string.IsNullOrEmpty(notePart) && int.TryParse(notePart, out int noteNumber))
                {
                    if (!noteToClipMap.ContainsKey(noteNumber))
                    {
                        noteToClipMap.Add(noteNumber, clip);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing clip name {clip.name}: {ex.Message}");
            }
        }
    }

    public void OnKey(int keyNumber)
    {
        if (noteToClipMap.TryGetValue(keyNumber, out AudioClip clipToPlay))
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning($"No audio clip found for note number: {keyNumber}");
        }
    }

}