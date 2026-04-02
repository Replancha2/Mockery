using UnityEngine;
using System.Collections.Generic;

public class SongInputHandler : MonoBehaviour
{
    public static SongInputHandler Instance { get; private set; }

    public enum Dir { Up, Down, Left, Right }

    // 4-key sequences for each spell type (Helldivers-style)
    public static readonly Dictionary<ElementType, Dir[]> Songs = new()
    {
        { ElementType.Asonante,    new[]{ Dir.Up,   Dir.Up,   Dir.Right, Dir.Up   } },
        { ElementType.Discordante, new[]{ Dir.Left,  Dir.Down, Dir.Down,  Dir.Left } },
        { ElementType.Consonante,  new[]{ Dir.Down,  Dir.Right, Dir.Right, Dir.Down } },
    };

    public ElementType ActiveSong    { get; private set; }
    public int         CurrentIndex  { get; private set; }
    public float       TimeRemaining { get; private set; }
    private bool       isActive;

    public event System.Action<int> OnKeyCorrect; // passes new index
    public event System.Action      OnSuccess;
    public event System.Action      OnFail;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartInput(ElementType song)
    {
        ActiveSong    = song;
        CurrentIndex  = 0;
        TimeRemaining = FloorManager.Instance.GetInputTimeLimit();
        isActive      = true;
    }

    public void StopInput() => isActive = false;

    void Update()
    {
        if (!isActive) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0) { Fail(); return; }

        Dir? pressed = GetPressedDir();
        if (pressed == null) return;

        Dir[] sequence = Songs[ActiveSong];
        if (pressed == sequence[CurrentIndex])
        {
            CurrentIndex++;
            OnKeyCorrect?.Invoke(CurrentIndex);
            if (CurrentIndex >= sequence.Length) Success();
        }
        else
        {
            Fail();
        }
    }

    void Success() { isActive = false; OnSuccess?.Invoke(); }
    void Fail()    { isActive = false; OnFail?.Invoke(); }

    Dir? GetPressedDir()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    return Dir.Up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  return Dir.Down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  return Dir.Left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Dir.Right;
        return null;
    }
}
