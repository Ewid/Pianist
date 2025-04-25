using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedSongData : MonoBehaviour
{
    public static int SelectedSongId { get; set; } = -1;
    public enum PlayMode { Easy, Mastery }
    public static PlayMode CurrentMode { get; set; } = PlayMode.Easy;
}
