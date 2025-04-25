using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApiData;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Transform songListContent;
    [SerializeField] private GameObject songItemPrefab;
    [SerializeField] private Button startSongButton;
    [SerializeField] private Button playButtonInitial;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private GameObject songSelectionPanel;
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private Button closeSongPanelButton;

    [SerializeField] private Button easyModeButton;
    [SerializeField] private Button masteryModeButton;
    [SerializeField] private Color selectedModeColor = Color.grey;
    [SerializeField] private Color defaultModeColor = Color.white;

    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private TextMeshProUGUI usernameText; 
    [SerializeField] private Transform achievementsContentTransform;
    [SerializeField] private GameObject achievementItemPrefab;
    [SerializeField] private Button closeAchievementsButton;

    private int selectedSongId = -1;
    private SelectedSongData.PlayMode selectedMode = SelectedSongData.PlayMode.Easy;
    private Dictionary<int, SongData> allSongsData = new Dictionary<int, SongData>();

    void Start()
    {
        ShowMainButtonsPanel();

        if (playButtonInitial != null) playButtonInitial.onClick.AddListener(ShowSongSelectionPanel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitApplication);
        if (achievementsButton != null) achievementsButton.onClick.AddListener(ShowAchievements);

        if (startSongButton != null) startSongButton.onClick.AddListener(StartPianoScene);
        if (easyModeButton != null) easyModeButton.onClick.AddListener(SelectEasyMode);
        if (masteryModeButton != null) masteryModeButton.onClick.AddListener(SelectMasteryMode);
        if (closeSongPanelButton != null) closeSongPanelButton.onClick.AddListener(ShowMainButtonsPanel);

        if (closeAchievementsButton != null) closeAchievementsButton.onClick.AddListener(ShowMainButtonsPanel); 

        if (startSongButton != null) startSongButton.interactable = false;
        UpdateModeButtonVisuals();

        FetchAllSongsData();

        // --- Add Mode Button Listeners ---
        if (easyModeButton != null)
        {
            easyModeButton.onClick.AddListener(SelectEasyMode);
        }
        else
        {
             Debug.LogError("EasyModeButton not assigned in MainMenuManager!");
        }

        if (masteryModeButton != null)
        {
            masteryModeButton.onClick.AddListener(SelectMasteryMode);
        }
        else
        {
             Debug.LogError("MasteryModeButton not assigned in MainMenuManager!");
        }

        // -----------------------------------
    }

    void OnDestroy()
    {
        if (playButtonInitial != null) playButtonInitial.onClick.RemoveListener(ShowSongSelectionPanel);
        if (startSongButton != null) startSongButton.onClick.RemoveListener(StartPianoScene);
        if (easyModeButton != null) easyModeButton.onClick.RemoveListener(SelectEasyMode);
        if (masteryModeButton != null) masteryModeButton.onClick.RemoveListener(SelectMasteryMode);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitApplication);
        if (achievementsButton != null) achievementsButton.onClick.RemoveListener(ShowAchievements);
        if (closeAchievementsButton != null) closeAchievementsButton.onClick.RemoveListener(ShowMainButtonsPanel); 
        if (closeSongPanelButton != null) closeSongPanelButton.onClick.RemoveListener(ShowMainButtonsPanel);
        ClearSongList();
        ClearAchievementItems();
    }

    void ShowMainButtonsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (songSelectionPanel != null) songSelectionPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(false);
    }

    void ShowSongSelectionPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (songSelectionPanel != null)
        {
            songSelectionPanel.SetActive(true);
            FetchAndPopulateSongList();
        }
        if (achievementsPanel != null) achievementsPanel.SetActive(false);
    }

    void FetchAllSongsData()
    {
        if (ApiService.Instance != null)
        {
            ApiService.Instance.GetSongs(OnAllSongsReceived, OnSongsError);
        }
    }

    void OnAllSongsReceived(List<SongData> songs)
    {
        allSongsData.Clear();
        if (songs != null)
        {
            foreach (var song in songs)
            {
                if (!allSongsData.ContainsKey(song.songId))
                {
                    allSongsData.Add(song.songId, song);
                }
            }
        }
    }

    void FetchAndPopulateSongList()
    {
         if (ApiService.Instance != null)
        {
            PopulateSongList(new List<SongData>(allSongsData.Values));
        }
    }

    void PopulateSongList(List<SongData> songs)
    {
        if (songListContent == null || songItemPrefab == null)
        {
            Debug.LogError("Song List Content or Song Item Prefab not assigned in MainMenuManager!");
            return;
        }

        ClearSongList();

        foreach (SongData song in songs)
        {
            GameObject songItemGO = Instantiate(songItemPrefab, songListContent);

            var songItemUI = songItemGO.GetComponent<SongItemUI>();
            if (songItemUI != null)
            {
                 songItemUI.Initialize(song, this);
            }
            else
            {
                 Button itemButton = songItemGO.GetComponent<Button>();
                 TextMeshProUGUI itemText = songItemGO.GetComponentInChildren<TextMeshProUGUI>();

                 if (itemText != null)
                 {
                     itemText.text = $"{song.title} - {song.artist} (Diff: {song.difficulty})"; 
                 }

                 if (itemButton != null)
                 {
                     int currentSongId = song.songId; 
                     itemButton.onClick.AddListener(() => SelectSong(currentSongId)); 
                 }
             }
        }
    }

    void OnSongsError(string errorMessage)
    {
        Debug.LogError("Failed to fetch songs: " + errorMessage);
    }

    public void SelectSong(int songId)
    {
        selectedSongId = songId;
        
        if (startSongButton != null)
        {
            startSongButton.interactable = true;
        }

        foreach (Transform child in songListContent)
        {
            var item = child.GetComponent<SongItemUI>();
            if (item != null)
            {
                item.SetSelected(item.SongId == songId);
            }
        }
    }

    void SelectEasyMode()
    {
        selectedMode = SelectedSongData.PlayMode.Easy;
        UpdateModeButtonVisuals();
    }

    void SelectMasteryMode()
    {
        selectedMode = SelectedSongData.PlayMode.Mastery;
        UpdateModeButtonVisuals();
    }

    void UpdateModeButtonVisuals()
    {
        if (easyModeButton != null)
        {
            Image easyBtnImage = easyModeButton.GetComponent<Image>();
            if (easyBtnImage != null)
            {
                easyBtnImage.color = (selectedMode == SelectedSongData.PlayMode.Easy) ? selectedModeColor : defaultModeColor;
            }
        }
        if (masteryModeButton != null)
        {
            Image masteryBtnImage = masteryModeButton.GetComponent<Image>();
             if (masteryBtnImage != null)
            {
                masteryBtnImage.color = (selectedMode == SelectedSongData.PlayMode.Mastery) ? selectedModeColor : defaultModeColor;
            }
        }
    }

    void StartPianoScene()
    {
        if (selectedSongId == -1)
        {
            Debug.LogError("Cannot start, no song selected!");
            return;
        }

        SelectedSongData.SelectedSongId = selectedSongId;
        SelectedSongData.CurrentMode = selectedMode;

        SceneManager.LoadScene("PianoScene");
    }

    void ClearSongList()
    {
        if (songListContent == null) return;

        foreach (Transform child in songListContent)
        {
            Destroy(child.gameObject);
        }
    }

    void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowAchievements()
    {
        if (achievementsPanel == null || usernameText == null || achievementsContentTransform == null || achievementItemPrefab == null)
        {
            Debug.LogError("Achievements Panel UI elements (Panel, Username Text, Content Transform, Item Prefab) not fully assigned in MainMenuManager!");
            return;
        }

        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (songSelectionPanel != null) songSelectionPanel.SetActive(false);
        if (achievementsPanel != null) achievementsPanel.SetActive(true);
             
        ClearAchievementItems();

        if (ApiService.Instance == null || !ApiService.Instance.IsLoggedIn() || ApiService.Instance.CurrentUser == null)
        {
            usernameText.text = "Not Logged In";
             ShowSingleAchievementMessage("Please log in to view achievements.");
            return;
        }

        usernameText.text = "Welcome " + ApiService.Instance.CurrentUser.username;

        ApiService.Instance.GetAllProgress(OnProgressReceived, OnProgressError);
    }

    void OnProgressReceived(List<ProgressData> progressList)
    {
        ClearAchievementItems();

        if (progressList == null)
        {
            ShowSingleAchievementMessage("Could not load achievements.");
            return;
        }

        int completedCount = 0;

        foreach (var progress in progressList)
        {
            string modeText = null;
            if (progress.completionStatus == "completed_guided")
            {
                modeText = "Easy Mode";
            }
            else if (progress.completionStatus == "completed_free")
            {
                modeText = "Mastery Mode";
            }

            if (modeText != null)
            {
                string certificateText;
                if (allSongsData.TryGetValue(progress.songId, out SongData song))
                {
                    certificateText = $"- Completed {modeText} for '{song.title} - {song.artist}'";
                    completedCount++;
                }
                else
                {
                    certificateText = $"- Completed {modeText} for Song ID: {progress.songId} (Details unavailable)";
                    completedCount++;
                }
                InstantiateAchievementItem(certificateText);
            }
        }

        if (completedCount == 0)
        {
            ShowSingleAchievementMessage("No completed songs yet!");
        }
    }

    void OnProgressError(string error)
    {
        Debug.LogError($"Error fetching progress: {error}");
        ClearAchievementItems();
        ShowSingleAchievementMessage("Error loading achievements.");
    }

    void InstantiateAchievementItem(string textToShow)
    {
        if (achievementItemPrefab == null || achievementsContentTransform == null) return;

        GameObject itemGO = Instantiate(achievementItemPrefab, achievementsContentTransform);
        TextMeshProUGUI itemText = itemGO.GetComponentInChildren<TextMeshProUGUI>();
        if (itemText != null)
        {
            itemText.text = textToShow;
        }
    }

    void ShowSingleAchievementMessage(string message)
    {
         ClearAchievementItems();
         InstantiateAchievementItem(message);
    }

    void ClearAchievementItems()
    {
        if (achievementsContentTransform == null) return;

        foreach (Transform child in achievementsContentTransform)
        {
            Destroy(child.gameObject);
        }
    }
}

public class SongItemUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Button selectButton;
    public Image background;
    public int SongId { get; private set; }
    private MainMenuManager mainMenuManager;

    public void Initialize(SongData song, MainMenuManager manager)
    {
        SongId = song.songId;
        titleText.text = $"{song.title} - {song.artist}";
        mainMenuManager = manager;
        selectButton.onClick.AddListener(() => mainMenuManager.SelectSong(SongId));
    }

    public void SetSelected(bool isSelected)
    {
        if (background != null)
        {
            background.color = isSelected ? Color.cyan : Color.white;
        }
    }

    void OnDestroy()
    {
        if (selectButton != null) selectButton.onClick.RemoveAllListeners();
    }
} 