using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SlimeMotor))]
public class SlimeController : MonoBehaviour
{
    private SlimeMotor motor;
    [SerializeField] private float rotationSpeed = 10f; // 회전 속도 추가

    // 메인 카메라 트랜스폼 캐싱용
    private Transform mainCameraTransform;

    void Awake()
    {
        motor = GetComponent<SlimeMotor>();

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        Vector3 finalMoveDir = Vector3.zero;

        if (Keyboard.current != null)
        {
            float x = 0f;
            float z = 0f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z = -1f;

            // 1. 카메라가 있다면 카메라 기준으로 방향 벡터 계산
            if (mainCameraTransform != null)
            {
                // 카메라의 전방/우측 벡터 추출
                Vector3 camForward = mainCameraTransform.forward;
                Vector3 camRight = mainCameraTransform.right;

                // 중요: 카메라가 아래를 내려다보므로 Y축 이동 성분을 제거 (평평하게 만들기)
                camForward.y = 0f;
                camRight.y = 0f;

                // 정규화(Normalize)를 통해 순수 방향만 남김
                camForward.Normalize();
                camRight.Normalize();

                // 입력값(x, z)과 카메라 벡터를 조합하여 최종 이동 방향 결정
                finalMoveDir = (camForward * z) + (camRight * x);
            }
            else
            {
                // 예외 처리: 카메라가 없을 경우 기존 월드 축 기준
                finalMoveDir = new Vector3(x, 0f, z);
            }

            finalMoveDir = finalMoveDir.normalized;
        }

        // 2. 모터에 카메라 기준 이동 방향 전달
        motor.SetMoveInput(finalMoveDir);

        // 3. 이동 방향이 있다면 캐릭터를 그 방향으로 회전
        if (finalMoveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalMoveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 4. 대시(바디 슬램) 처리
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            // 현재 바라보는 정면(transform.forward) 혹은 카메라 기준 조준 방향으로 대시
            motor.AddBodySlamForce(transform.forward, 15f);
        }
    }
}