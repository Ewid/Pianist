using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Keyboard : MonoBehaviour
{
    public GameObject blackKey, whiteKey;
    
    public GameObject content;

    public int numberOfOctaves;

    void Start()
    {
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
            GameObject note = instantiateNote(whiteKey,actualNoteIndex, startingNote);
            registerEvents(note);

            note.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote - 1, note.GetComponent<RectTransform>().sizeDelta.y);
            note.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(keyWidthPerOctave * octave + widthPerNote * i + widthPerNote/2, -whiteKey.GetComponent<RectTransform>().rect.height/2, 0);
        }

        for (int i = 0; i < 5; i++){
            int actualNoteIndex = getBlackKeyIndex(i);
            GameObject note = instantiateNote(blackKey, actualNoteIndex, startingNote);
            registerEvents(note);

            note.GetComponent<RectTransform>().sizeDelta = new Vector2(widthPerNote/2, note.GetComponent<RectTransform>().sizeDelta.y);

            int blackIndex = i;
            if(i > 1){
                blackIndex += 1;
            }
            note.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(keyWidthPerOctave * octave + widthPerNote * blackIndex + widthPerNote, -blackKey.GetComponent<RectTransform>().rect.height/2, 0);
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
        Debug.Log("Key Clicked: " + noteNumber);
    }

    public void keyOff(int noteNumber){
        Debug.Log("Key Released: " + noteNumber);
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

    void Update()
    {

    }


}
