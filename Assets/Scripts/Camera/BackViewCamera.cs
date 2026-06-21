using UnityEngine;

public class BackViewCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // 추적할 Player 트랜스폼

    [Header("Position Settings")]
    [SerializeField] private float distance = 6f; // 플레이어 등 뒤로 떨어질 거리
    [SerializeField] private float height = 3.5f;   // 살짝 위로 올릴 Y값 높이
    [SerializeField] private float smoothSpeed = 8f; // 카메라 이동 부드러움 지수

    [Header("Look Settings")]
    [SerializeField] private float lookAtHeightOffset = 1.0f; // 플레이어의 약간 위를 바라보게 함 (구도 안정감)

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 플레이어의 등 뒤 + 위쪽 Y값의 이상적인 목표 위치 계산
        // target.forward 반대 방향으로 distance만큼 가고, 위로 height만큼 이동
        Vector3 targetPosition = target.position - (target.forward * distance) + (Vector3.up * height);

        // 2. 카메라 위치를 부드럽게 보간 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // 3. 카메라가 플레이어를 바라보게 만듦 (약간 위를 보게 해서 안정감 확보)
        Vector3 lookTarget = target.position + (Vector3.up * lookAtHeightOffset);
        transform.LookAt(lookTarget);
    }
}
