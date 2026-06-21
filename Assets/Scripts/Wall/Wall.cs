using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Wall Data")]
    [SerializeField] private string patternID = "Empty";

    public string GetPatternID() => patternID;

    public void SetPatternID(string newID)
    {
        patternID = newID;
        Debug.Log($"[{gameObject.name}] 벽면 상태가 변경됨 -> {patternID}");
    }
}
