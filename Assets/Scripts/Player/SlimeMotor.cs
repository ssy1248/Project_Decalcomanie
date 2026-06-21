using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlimeMotor : MonoBehaviour
{
    public enum SlimeState { Normal, Dashing }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxVelocity = 8f;
    [SerializeField] private float slideDeceleration = 0.15f;

    [Header("Spring Suspension (Bouncy Feel)")]
    [SerializeField] private float rideHeight = 0.6f;
    [SerializeField] private float springStrength = 250f; // 탄성을 조금 더 올리면 쫀득해집니다.
    [SerializeField] private float springDamper = 15f;   // 감쇠를 약간 낮춰 출렁임을 유도합니다.

    [Header("Slime Rhythmic Hop (이동 시 통통 튐)")]
    [SerializeField] private float hopForce = 3.5f;       // 위로 튀는 힘
    [SerializeField] private float hopFrequency = 6f;     // 튀는 속도 주기

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isGrounded;
    private float hopTimer;

    private SlimeState currentState = SlimeState.Normal;
    private string currentSlimePattern = "Empty";

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
    }
    public void FixedUpdate()
    {
        EvaluateGround();
        ApplySuspension();
        ApplyMovement();
    }

    public void SetMoveInput(Vector3 input)
    {
        if (currentState == SlimeState.Dashing) 
            return;
        moveInput = input.normalized;
    }

    private void EvaluateGround()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        // 레이 길이를 늘려 서스펜션이 공중 복귀 시에도 부드럽게 감쇠하도록 합니다.
        isGrounded = Physics.Raycast(ray, out RaycastHit hit, rideHeight * 1.5f);
    }

    private void ApplySuspension()
    {
        if(!isGrounded || currentState == SlimeState.Dashing) 
            return;

        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, rideHeight * 1.5f))
        {
            Vector3 vel = rb.linearVelocity;
            Vector3 rayDir = Vector3.down;
            float rayDirVel = Vector3.Dot(rayDir, vel);
            float relDist = hit.distance - rideHeight;
            float springForce = (relDist * -springStrength) - (rayDirVel * springDamper);

            rb.AddForce(-rayDir * springForce, ForceMode.Acceleration);
        }
    }

    private void ApplyMovement()
    {
        if (currentState == SlimeState.Dashing)
            return;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 targetVel = moveInput * moveSpeed;
            Vector3 velocityChange = targetVel - horizontalVel;
            velocityChange = Vector3.ClampMagnitude(velocityChange, maxVelocity);
            rb.AddForce(new Vector3(velocityChange.x, 0, velocityChange.z), ForceMode.VelocityChange);

            if (isGrounded)
            {
                hopTimer += Time.fixedDeltaTime * hopFrequency;
                if (hopTimer >= Mathf.PI)
                {
                    hopTimer = 0f;
                    rb.AddForce(Vector3.up * hopForce, ForceMode.Impulse);
                }
            }
        }
        else
        {
            hopTimer = 0f;
            Vector3 slowedVel = Vector3.Lerp(horizontalVel, Vector3.zero, slideDeceleration);
            rb.linearVelocity = new Vector3(slowedVel.x, rb.linearVelocity.y, slowedVel.z);
        }
    }

    // 바디 슬램 촉발 (Controller에서 호출)
    public void AddBodySlamForce(Vector3 direction, float force)
    {
        if (currentState == SlimeState.Dashing) return; // 이미 대시 중이면 무시

        currentState = SlimeState.Dashing;
        rb.useGravity = false; // 대시 중 처짐 방지를 위해 중력 잠시 끄기
        rb.linearVelocity = Vector3.zero;

        // 정면을 향해 강한 수평/직선 힘 가하기
        rb.AddForce(direction.normalized * force, ForceMode.VelocityChange);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 대시 상태일 때만 벽면 정지/상태 변경 로직을 수행함
        if (currentState == SlimeState.Dashing)
        {
            // 부딪힌 오브젝트가 Wall 컴포넌트를 가지고 있는지 확인
            if (collision.gameObject.TryGetComponent<Wall>(out Wall hitWall))
            {
                // 1. 물리 속도 즉시 제로화 (딱 멈춤)
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // 2. 충돌 지점 데이터 확보 (Phase 2 데칼 생성용 예비)
                ContactPoint contact = collision.contacts[0];
                Vector3 impactPoint = contact.point;
                Vector3 impactNormal = contact.normal;

                // 3. [복사/붙여넣기 데이터 로직]
                if (currentSlimePattern == "Empty")
                {
                    // 슬라임이 빈 상태면 벽의 패턴을 복사 (상태 A)
                    currentSlimePattern = hitWall.GetPatternID();
                    Debug.Log($"<color=cyan>[벽지 패턴 {currentSlimePattern} 복사 완료]</color>");
                }
                else
                {
                    // 슬라임이 이미 패턴을 들고 있다면 벽에 무늬를 칠함 (상태 B)
                    hitWall.SetPatternID(currentSlimePattern);
                    Debug.Log($"<color=yellow>[벽면에 {currentSlimePattern} 패턴 적용 완료]</color>");
                    currentSlimePattern = "Empty"; // 전송 후 다시 빈 상태로 리셋
                }

                // 4. 상태 복구
                EndBodySlam();
            }
        }
    }

    private void EndBodySlam()
    {
        currentState = SlimeState.Normal;
        rb.useGravity = true;
    }
}