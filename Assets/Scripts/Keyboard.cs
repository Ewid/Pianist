using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class Keyboard : MonoBehaviour
{
    public GameObject blackKey, whiteKey;
    
    public GameObject content;

    public int numberOfOctaves;

    public Color defaultWhiteKeyColor = Color.white;
    public Color defaultBlackKeyColor = Color.black;
    public Color highlightColor = Color.yellow;

    private Dictionary<int, GameObject> keyObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, Color> originalKeyColors = new Dictionary<int, Color>();

    public event Action<int> OnKeyPressed;

    void Awake()
    {
        keyObjects.Clear();
        originalKeyColors.Clear();
        int startingNote = 24;
        for (int i = 0; i < numberOfOctaves; i++)
        {
            createOctave(startingNote + i * 12, i);
        }
    }

    private void createOctave(int startingNote, int octave)
    {
        float width = content.GetComponent<RectTransform>().rect.width;
        float keyWidthPerOctave = width / numberOfOctaves;
        float widthPerNote = keyWidthPerOctave / 7;

        for (int i = 0; i < 7; i++)
        {
            int actualNoteIndex = getWhiteKeyIndex(i);
            int noteNumber = startingNote + actualNoteIndex;
            GameObject note = instantiateNote(whiteKey, actualNoteIndex, startingNote);
            registerEvents(note);

            note.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote - 1, note.GetComponent<RectTransform>().sizeDelta.y);
            note.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(keyWidthPerOctave * octave + widthPerNote * i + widthPerNote/2, -whiteKey.GetComponent<RectTransform>().rect.height/2, 0);
            
            keyObjects[noteNumber] = note;
            originalKeyColors[noteNumber] = defaultWhiteKeyColor;
        }

        for (int i = 0; i < 5; i++){
            int actualNoteIndex = getBlackKeyIndex(i);
            int noteNumber = startingNote + actualNoteIndex;
            GameObject note = instantiateNote(blackKey, actualNoteIndex, startingNote);
            registerEvents(note);

            note.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote/2, note.GetComponent<RectTransform>().sizeDelta.y);

            int blackIndex = i;
            if(i > 1){
                blackIndex += 1;
            }
            note.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(keyWidthPerOctave * octave + widthPerNote * blackIndex + widthPerNote, -blackKey.GetComponent<RectTransform>().rect.height/2, 0);
            
            keyObjects[noteNumber] = note;
            originalKeyColors[noteNumber] = defaultBlackKeyColor;
        }
    }

    private void registerEvents(GameObject note){
        EventTrigger trigger = note.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((e) => keyOn(note.GetComponent<PianoTile>().note));
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((e) => keyOff(note.GetComponent<PianoTile>().note));
        trigger.triggers.Add(pointerUp);
    }

    public void keyOn(int noteNumber){
        GameObject.Find("SoundGeneration").GetComponent<SoundGeneration>().OnKey(noteNumber);
        OnKeyPressed?.Invoke(noteNumber); 
    }

    public void keyOff(int noteNumber){
    }

    private GameObject instantiateNote(GameObject note, int actualNoteIndex, int startingNote){
        GameObject newNote = Instantiate(note);
        newNote.transform.SetParent(content.transform, false);
        newNote.GetComponent<PianoTile>().note = startingNote + actualNoteIndex;
        return newNote;
    }

    private int getWhiteKeyIndex(int i){
        int actualNoteIndex = 0;

        if(i == 1){
            actualNoteIndex = 2;
        } else if(i == 2){
            actualNoteIndex = 4;
        } else if(i == 3){  
            actualNoteIndex = 5;
        } else if(i == 4){
            actualNoteIndex = 7;
        } else if(i == 5){
            actualNoteIndex = 9;
        } else if(i == 6){
            actualNoteIndex = 11;
        }

        return actualNoteIndex;
    }

    private int getBlackKeyIndex(int i){
        int actualNote = 1;
        if(i == 1){
            actualNote = 3;
        } else if(i == 2){
            actualNote = 6;
        } else if(i == 3){
            actualNote = 8;
        }
        return actualNote;
    }

    public void HighlightKey(int noteNumber, Color highlightColorToUse)
    {
        if (keyObjects.TryGetValue(noteNumber, out GameObject keyGO))
        {
            SetKeyColor(keyGO, highlightColorToUse);
        }
    }

    public void UnhighlightKey(int noteNumber)
    {
        if (keyObjects.TryGetValue(noteNumber, out GameObject keyGO))
        {
            Color originalColor = originalKeyColors.TryGetValue(noteNumber, out Color color)
                                ? color
                                : (IsWhiteKey(noteNumber) ? defaultWhiteKeyColor : defaultBlackKeyColor);
            SetKeyColor(keyGO, originalColor);
        }
    }

    private void SetKeyColor(GameObject keyGO, Color color)
    {
        Image keyImage = keyGO.GetComponent<Image>();
        if (keyImage != null)
        {
            keyImage.color = color;
        }
    }

    private bool IsWhiteKey(int noteNumber)
    {
        int noteInOctave = noteNumber % 12;
        return noteInOctave == 0 || noteInOctave == 2 || noteInOctave == 4 || noteInOctave == 5 || noteInOctave == 7 || noteInOctave == 9 || noteInOctave == 11;
    }

    public void ResetAllHighlights()
    {
        foreach (var pair in keyObjects)
        {
            UnhighlightKey(pair.Key);
        }
    }

    public bool HasKeyObject(int noteNumber)
    {
        return keyObjects.ContainsKey(noteNumber);
    }
}
