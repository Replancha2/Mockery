using UnityEngine;

public class DebugOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log($"[DebugOnEnable] frame={Time.frameCount} {gameObject.name} was enabled!\n{System.Environment.StackTrace}");
    }
}
