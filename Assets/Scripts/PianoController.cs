using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using System.Linq;
using ApiData;
using UnityEngine.SceneManagement;

public class PianoController : MonoBehaviour
{
    public enum PlaybackMode
    {
        Automatic,
        InteractiveEasy,
        Mastery
    }

    public Keyboard keyboard;
    public float playbackSpeed = 1.0f;
    public Color upcomingNoteHighlightColor = Color.yellow;
    public Color nextNoteHighlightColor = Color.green;
    public Color chordNoteHighlightColor = Color.cyan;
    public Color playedNoteColor = Color.gray;
    public float masteryIdleDuration = 5.0f;

    private Coroutine playbackCoroutine;
    private List<Note> songNotes;
    private TempoMap tempoMap;
    private int currentNoteIndex = 0;
    private PlaybackMode currentMode;

    private HashSet<int> currentStepNotesSet = new HashSet<int>();
    private List<int> orderedStepNotes = new List<int>();
    private bool waitingForInput = false;

    private float lastCorrectInputTime;
    private bool isHintActive = false;
    private HashSet<int> currentMasteryStepNotes = new HashSet<int>();
    private int nextMasteryIndex = 0;

    private bool songCompleted = false;

    private const float LOOKAHEAD_TIME = 2.0f;
    private const float CHORD_TIME_THRESHOLD = 0.05f;

    void Start()
    {
        if (keyboard == null)
        {
            Debug.LogError("Keyboard reference not set in PianoController!");
            this.enabled = false;
            return;
        }
        keyboard.OnKeyPressed += OnKeyboardInput;

        PlaybackMode targetMode = SelectedSongData.CurrentMode == SelectedSongData.PlayMode.Easy ?
                                   PlaybackMode.InteractiveEasy :
                                   PlaybackMode.Mastery;

        if (!LoadSongNotes(SelectedSongData.SelectedSongId))
        {
            Debug.LogError("Failed to load song notes. Cannot start playback.");
            return;
        }

        StartPlayback(targetMode);
        songCompleted = false;
    }

    void OnDisable()
    {
        if (keyboard != null)
        {
            keyboard.OnKeyPressed -= OnKeyboardInput;
        }
        StopPlayback();
    }

    bool LoadSongNotes(int songId)
    {
        string midiFilePath = Path.Combine(Application.streamingAssetsPath, $"song_{songId}.mid");

        if (!File.Exists(midiFilePath))
        {
            Debug.LogError($"MIDI file not found at path: {midiFilePath}");
            return false;
        }

        try
        {
            MidiFile midiFile = MidiFile.Read(midiFilePath);
            tempoMap = midiFile.GetTempoMap();
            songNotes = midiFile.GetNotes().ToList();

            if (songNotes == null || songNotes.Count == 0)
            {
                Debug.LogWarning($"Parsed MIDI file {midiFilePath}, but found 0 notes.");
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading or parsing MIDI file {midiFilePath}: {ex.Message}\nStack Trace: {ex.StackTrace}");
            songNotes = null;
            tempoMap = null;
            return false;
        }
    }


    public void StartPlayback(PlaybackMode mode)
    {
        StopPlayback();

        if (songNotes == null)
        {
            Debug.LogError("Cannot start playback: Notes not loaded or failed to load.");
            return;
        }
         if (songNotes.Count == 0)
        {
             Debug.LogWarning("Starting playback with an empty song (0 notes).");
        }


        currentMode = mode;
        currentNoteIndex = 0;
        if(keyboard != null) keyboard.ResetAllHighlights();

        songCompleted = false;

        switch (currentMode)
        {
            case PlaybackMode.Automatic:
                playbackCoroutine = StartCoroutine(PlaySongAutomaticallyCoroutine());
                break;
            case PlaybackMode.InteractiveEasy:
                playbackCoroutine = StartCoroutine(InteractiveEasyModeCoroutine());
                break;
            case PlaybackMode.Mastery:
                playbackCoroutine = StartCoroutine(MasteryModeCoroutine());
                break;
        }
    }

    public void StopPlayback()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
       if (keyboard != null)
       {
            keyboard.ResetAllHighlights();
       }
        waitingForInput = false;
        isHintActive = false;
        currentStepNotesSet.Clear();
        orderedStepNotes.Clear();
        currentMasteryStepNotes.Clear();
    }

    private double GetNoteStartTimeInSeconds(Note note)
    {
        return note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
    }

     private double GetNoteDurationInSeconds(Note note)
    {
        return note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
    }

    IEnumerator PlaySongAutomaticallyCoroutine()
    {
        float playbackStartTime = Time.time;
        int lookAheadIndex = 0;

        if(keyboard != null) keyboard.OnKeyPressed -= OnKeyboardInput;

        while (currentNoteIndex < songNotes.Count)
        {
            double currentSongTimeSeconds = (Time.time - playbackStartTime) * playbackSpeed;

            while (lookAheadIndex < songNotes.Count)
            {
                 Note upcomingNote = songNotes[lookAheadIndex];
                 double upcomingNoteStartTime = GetNoteStartTimeInSeconds(upcomingNote);
                 if (upcomingNoteStartTime <= currentSongTimeSeconds + LOOKAHEAD_TIME / playbackSpeed)
                 {
                     if (keyboard.HasKeyObject(upcomingNote.NoteNumber))
                     {
                         keyboard.HighlightKey(upcomingNote.NoteNumber, upcomingNoteHighlightColor);
                     }
                     lookAheadIndex++;
                 }
                 else
                 {
                     break;
                 }
            }

            Note nextNote = songNotes[currentNoteIndex];
            double nextNoteStartTime = GetNoteStartTimeInSeconds(nextNote);

            if (currentSongTimeSeconds >= nextNoteStartTime)
            {
                double stepStartTime = nextNoteStartTime;
                int stepEndIndex = currentNoteIndex;
                while (stepEndIndex < songNotes.Count)
                {
                    Note noteInStep = songNotes[stepEndIndex];
                    double noteInStepStartTime = GetNoteStartTimeInSeconds(noteInStep);

                    if (Mathf.Abs((float)(noteInStepStartTime - stepStartTime)) < CHORD_TIME_THRESHOLD)
                    {
                        if (keyboard.HasKeyObject(noteInStep.NoteNumber))
                        {
                            keyboard.HighlightKey(noteInStep.NoteNumber, nextNoteHighlightColor);
                        }
                        stepEndIndex++;
                    }
                    else
                    {
                        break;
                    }
                }
                currentNoteIndex = stepEndIndex;
            }

            yield return null;
        }

        StopPlayback();
    }

    IEnumerator InteractiveEasyModeCoroutine()
    {
        waitingForInput = false;
        currentNoteIndex = 0;

        while (currentNoteIndex < songNotes.Count)
        {
            if (!waitingForInput)
            {
                 FindAndHighlightNextStep();
                 if (orderedStepNotes.Count > 0)
                 {
                     waitingForInput = true;
                 }
                 else
                 {
                    currentNoteIndex = songNotes.Count;
                    break;
                 }
            }

            yield return null;
        }

        HandleSongCompletion();
        yield return new WaitForSeconds(2.0f);
        StopPlayback();
    }


    void FindAndHighlightNextStep()
    {
        if (currentNoteIndex >= songNotes.Count || keyboard == null) return;

        currentStepNotesSet.Clear();
        orderedStepNotes.Clear();
        keyboard.ResetAllHighlights();

        List<KeyValuePair<int, int>> notesInStep = new List<KeyValuePair<int, int>>();

        int stepStartIndex = currentNoteIndex;
        while (stepStartIndex < songNotes.Count && !keyboard.HasKeyObject(songNotes[stepStartIndex].NoteNumber))
        {
            stepStartIndex++;
        }
        if (stepStartIndex >= songNotes.Count)
        {
            currentNoteIndex = songNotes.Count;
            return;
        }
        currentNoteIndex = stepStartIndex;
        double stepStartTime = GetNoteStartTimeInSeconds(songNotes[currentNoteIndex]);

        int searchIndex = currentNoteIndex;
        while (searchIndex < songNotes.Count)
        {
            Note noteInStep = songNotes[searchIndex];
            double noteInStepStartTime = GetNoteStartTimeInSeconds(noteInStep);

            if (Mathf.Abs((float)(noteInStepStartTime - stepStartTime)) < CHORD_TIME_THRESHOLD)
            {
                if (keyboard.HasKeyObject(noteInStep.NoteNumber))
                {
                    notesInStep.Add(new KeyValuePair<int, int>(searchIndex, noteInStep.NoteNumber));
                }
                searchIndex++;
            }
            else
            {
                break;
            }
        }

        notesInStep.Sort((a, b) => a.Key.CompareTo(b.Key));

        if (notesInStep.Count > 0)
        {
            bool firstHighlighted = false;
            foreach (var pair in notesInStep)
            {
                int noteNumber = pair.Value;
                currentStepNotesSet.Add(noteNumber);
                orderedStepNotes.Add(noteNumber);

                Color highlightColor = !firstHighlighted ? nextNoteHighlightColor : chordNoteHighlightColor;
                keyboard.HighlightKey(noteNumber, highlightColor);
                firstHighlighted = true;
            }
        }
        else
        {
             Debug.LogWarning($"Logic error: Found start of step at index {currentNoteIndex}, but no playable notes within threshold after sorting.");
             currentNoteIndex++;
        }
    }

    IEnumerator MasteryModeCoroutine()
    {
        lastCorrectInputTime = Time.time;
        currentNoteIndex = 0;
        nextMasteryIndex = 0;
        isHintActive = false;
        nextMasteryIndex = FindNextMasteryStep();

        if (currentMasteryStepNotes.Count == 0 && currentNoteIndex >= songNotes.Count)
        {
            HandleSongCompletion();
            yield return new WaitForSeconds(1.0f);
            StopPlayback();
            yield break;
        }

         while (currentNoteIndex < songNotes.Count || currentMasteryStepNotes.Count > 0)
         {
             if (!isHintActive && currentMasteryStepNotes.Count > 0 && Time.time - lastCorrectInputTime > masteryIdleDuration)
             {
                 ActivateHints();
             }

             if (currentNoteIndex >= songNotes.Count && currentMasteryStepNotes.Count == 0)
             {
                break;
             }

             yield return null;
         }

         HandleSongCompletion();
         yield return new WaitForSeconds(2.0f);
         StopPlayback();
    }

    int FindNextMasteryStep()
    {
        currentMasteryStepNotes.Clear();

        if (currentNoteIndex >= songNotes.Count) return currentNoteIndex;

        int searchIndex = currentNoteIndex;
        int firstPlayableIndexInStep = -1;
        double stepStartTime = -1;

        while (searchIndex < songNotes.Count)
        {
            if (keyboard.HasKeyObject(songNotes[searchIndex].NoteNumber))
            {
                firstPlayableIndexInStep = searchIndex;
                stepStartTime = GetNoteStartTimeInSeconds(songNotes[searchIndex]);
                break;
            }
            searchIndex++;
        }

        if (firstPlayableIndexInStep == -1)
        {
             currentNoteIndex = songNotes.Count;
             return songNotes.Count;
        }


        currentNoteIndex = firstPlayableIndexInStep;

        searchIndex = currentNoteIndex;
        int indexAfterStep = currentNoteIndex;

        while (searchIndex < songNotes.Count)
        {
             Note noteInStep = songNotes[searchIndex];
             double noteInStepStartTime = GetNoteStartTimeInSeconds(noteInStep);

             if (Mathf.Abs((float)(noteInStepStartTime - stepStartTime)) < CHORD_TIME_THRESHOLD)
             {
                 if (keyboard.HasKeyObject(noteInStep.NoteNumber))
                 {
                     currentMasteryStepNotes.Add(noteInStep.NoteNumber);
                 }
                 indexAfterStep = searchIndex + 1;
                 searchIndex++;
             }
             else
             {
                  break;
             }
        }

        if (currentMasteryStepNotes.Count == 0 && indexAfterStep < songNotes.Count)
        {
             Debug.LogWarning($"FindNextMasteryStep: Found start at {currentNoteIndex} but no playable notes in threshold. Advancing past step.");
             currentNoteIndex = indexAfterStep;
             return FindNextMasteryStep();
        }

        return indexAfterStep;
    }


    void ActivateHints()
    {
        if (isHintActive || currentMasteryStepNotes.Count == 0 || keyboard == null) return;

        isHintActive = true;
        foreach (int noteNum in currentMasteryStepNotes)
        {
            keyboard.HighlightKey(noteNum, chordNoteHighlightColor);
        }
    }

    void OnKeyboardInput(int noteNumber)
    {
        if (keyboard == null) return;

        if (currentMode == PlaybackMode.InteractiveEasy && waitingForInput)
        {
            if (orderedStepNotes.Count > 0)
            {
                if (noteNumber == orderedStepNotes[0])
                {
                    keyboard.HighlightKey(noteNumber, playedNoteColor);
                    int playedNote = orderedStepNotes[0];
                    orderedStepNotes.RemoveAt(0);
                    currentStepNotesSet.Remove(playedNote);

                    if (orderedStepNotes.Count == 0)
                    {
                        double stepStartTime = GetNoteStartTimeInSeconds(songNotes[currentNoteIndex]);
                        int indexAfterCompletedStep = currentNoteIndex;
                        while(indexAfterCompletedStep < songNotes.Count &&
                              Mathf.Abs((float)(GetNoteStartTimeInSeconds(songNotes[indexAfterCompletedStep]) - stepStartTime)) < CHORD_TIME_THRESHOLD)
                        {
                            indexAfterCompletedStep++;
                        }
                        currentNoteIndex = indexAfterCompletedStep;

                        waitingForInput = false;
                    }
                    else
                    {
                        keyboard.HighlightKey(orderedStepNotes[0], nextNoteHighlightColor);
                        for(int i = 1; i < orderedStepNotes.Count; i++)
                        {
                             keyboard.HighlightKey(orderedStepNotes[i], chordNoteHighlightColor);
                        }
                    }
                }
                 else if (currentStepNotesSet.Contains(noteNumber))
                {
                }
                else
                {
                }
            }
            else
            {
                Debug.LogWarning($"Easy Mode: Input {noteNumber} received while waiting, but orderedStepNotes was empty.");
            }
        }
        else if (currentMode == PlaybackMode.Mastery)
        {
            if (currentMasteryStepNotes.Count == 0)
            {
                 return;
            }

            if (currentMasteryStepNotes.Contains(noteNumber))
            {
                lastCorrectInputTime = Time.time;

                if (isHintActive)
                {
                    keyboard.UnhighlightKey(noteNumber);
                    currentMasteryStepNotes.Remove(noteNumber);

                    if (currentMasteryStepNotes.Count == 0)
                    {
                        isHintActive = false;
                        currentNoteIndex = nextMasteryIndex;
                        nextMasteryIndex = FindNextMasteryStep();
                    }
                }
                else
                {
                     currentNoteIndex = nextMasteryIndex;
                     nextMasteryIndex = FindNextMasteryStep();
                }
            }
            else
            {
            }
        }
    }

    void HandleSongCompletion()
    {
        if (songCompleted)
        {
            return;
        }
        songCompleted = true;

        int songId = SelectedSongData.SelectedSongId;
        if (songId == -1)
        {
            Debug.LogError("Cannot record completion: Invalid Song ID (-1).");
            GoToMainMenu();
            return;
        }

        if (ApiService.Instance == null || !ApiService.Instance.IsLoggedIn())
        {
            Debug.LogWarning("Cannot record completion: User not logged in or ApiService unavailable.");
            GoToMainMenu();
            return;
        }

        string statusToSend = null;
        bool isMasteryCompletion = false;
        if (currentMode == PlaybackMode.InteractiveEasy)
        {
            statusToSend = "completed_guided";
        }
        else if (currentMode == PlaybackMode.Mastery)
        {
            statusToSend = "completed_free";
            isMasteryCompletion = true;
        }

        if (statusToSend == null)
        {
            Debug.LogWarning($"Completion in mode {currentMode} not recorded via API.");
            GoToMainMenu();
            return;
        }

        if (!isMasteryCompletion)
        {
            ApiService.Instance.GetSongProgress(songId,
                (existingProgress) => {
                    if (existingProgress != null && existingProgress.completionStatus == "completed_free")
                    {
                        Debug.Log($"Skipping Easy mode completion update for Song ID {songId} because Mastery mode is already completed.");
                        GoToMainMenu();
                    }
                    else
                    {
                         UpdateProgressApiCall(songId, statusToSend);
                    }
                },
                (errorMsg) => {
                    Debug.LogWarning($"Could not check existing progress for Song ID {songId} before Easy mode update: {errorMsg}. Proceeding with update attempt.");
                    UpdateProgressApiCall(songId, statusToSend);
                }
            );
        }
        else
        {
            UpdateProgressApiCall(songId, statusToSend);
        }
    }

    private void UpdateProgressApiCall(int songId, string statusToSend)
    {
        ProgressUpdateRequest updateData = new ProgressUpdateRequest
        {
            completionStatus = statusToSend
        };

        ApiService.Instance.UpdateProgress(songId, updateData,
            (progressResponse) => {
                GoToMainMenu();
            },
            (errorMsg) => {
                Debug.LogError($"Failed to update progress for Song ID {songId}: {errorMsg}");
                GoToMainMenu();
            }
        );
    }

    private void GoToMainMenu()
    {
        StopPlayback();
        SceneManager.LoadScene("MainMenuScene");
    }
}