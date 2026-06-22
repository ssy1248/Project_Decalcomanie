using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Wall Data")]
    [SerializeField] private string patternID = "Empty";
    // 시각화를 위해 벽면이 가질 실제 텍스처를 등록합니다.
    [SerializeField] private Texture2D patternTexture;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateWallVisual();
    }

    public string GetPatternID() => patternID;
    public Texture2D GetTexture() => patternTexture;

    // 슬라임에게 무늬를 전달받아 벽면을 채우는 함수
    public void SetPattern(string newID, Texture2D newTexture)
    {
        patternID = newID;
        patternTexture = newTexture;

        UpdateWallVisual();
        Debug.Log($"[{gameObject.name}] 벽면 시각 상태 변경 완료 -> {patternID}");
    }

    // 런타임에 벽면 머티리얼의 텍스처를 변경하여 시각화
    private void UpdateWallVisual()
    {
        if (meshRenderer == null)
            return;

        // 에디터 standard/URP Lit 셰이더의 메인 텍스처 프로퍼티 이름은 "_BaseMap"입니다.
        if (patternTexture != null)
        {
            meshRenderer.material.SetTexture("_BaseMap", patternTexture);
        }
        else
        {
            // 빈 벽일 경우 투명하거나 하얀 기본 상태 처리 (필요시)
            meshRenderer.material.SetTexture("_BaseMap", null);
        }
    }
}
