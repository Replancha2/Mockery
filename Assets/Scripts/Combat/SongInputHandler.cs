using UnityEngine;
using System.Collections.Generic;

public class SongInputHandler : MonoBehaviour
{
    public static SongInputHandler Instance { get; private set; }

    public enum Dir { Up, Down, Left, Right }
    //Ahora los casteos son en tiempo real, Para lanzar un hechizo Consonant, el jugador debe presionar Q, luego Shift + W, Shift + W, Shift + A, Shift + A, Shift + D, Shift + D en ese orden y dentro del límite de tiempo. Si lo hace correctamente, se lanzará el hechizo Consonant. 
    // Si se equivoca o se le acaba el tiempo, fallará el hechizo.
    //Así mismo, para el hechizo Assonant, El jugador presiona E y la secuencia es Shift + A, Shift + S, Shift + A, Shift + S, Shift + A, Shift + S.
    //  Y para el hechizo Dissonant, El jugador presiona R y la secuencia es Shift + S, Shift + S, Shift + D, Shift + S, Shift + S, Shift + D.
    public static readonly Dictionary<SpellType, Dir[]> Songs = new() //Aqui hay 3 canciones, cada una con su propia secuencia de direcciones. Se pueden modificar para crear nuevas canciones o ajustar la dificultad.
    {
        { SpellType.Consonant, new[]{ Dir.Up, Dir.Up, Dir.Left, Dir.Left, Dir.Right, Dir.Right } },
        { SpellType.Assonant,  new[]{ Dir.Left, Dir.Down, Dir.Left, Dir.Down, Dir.Left, Dir.Down } },
        { SpellType.Dissonant, new[]{ Dir.Down, Dir.Down, Dir.Right, Dir.Down, Dir.Down, Dir.Right } },
    };

    public SpellType ActiveSong    { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        Debug.Log("Spell sequences (test):");
        foreach (var kv in Songs)
        {
            Debug.Log($" - {kv.Key}: {string.Join(" ", System.Array.ConvertAll(kv.Value, d => d.ToString().ToUpper()))}");
        }
    }

    public int       CurrentIndex  { get; private set; }
    public float   TimeRemaining { get; private set; }
    private bool   isActive;

    public event System.Action<int> OnKeyCorrect; // passes new index
    public event System.Action      OnSuccess;
    public event System.Action      OnFail;

    public void StartInput(SpellType song)
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
            Debug.Log($"[KEY INPUT] Correct! Step {CurrentIndex}/{sequence.Length} for {ActiveSong} ({pressed})");
            OnKeyCorrect?.Invoke(CurrentIndex);
            if (CurrentIndex >= sequence.Length) Success();
        }
        else
        {
            Debug.LogWarning($"[KEY INPUT] WRONG! Expected {sequence[CurrentIndex]}, got {pressed}");
            Fail();
        }
    }

    void Success() { isActive = false; Debug.Log($"[SPELL SEQUENCE] SUCCESS! {ActiveSong} sequence completed perfectly!"); OnSuccess?.Invoke(); }
    void Fail()    { isActive = false; Debug.LogError($"[SPELL SEQUENCE] FAILED! {ActiveSong} sequence was interrupted or timed out!"); OnFail?.Invoke(); }

    Dir? GetPressedDir()
    {
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return null;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))    return Dir.Up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))  return Dir.Down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  return Dir.Left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Dir.Right;
        return null;
    }
}
