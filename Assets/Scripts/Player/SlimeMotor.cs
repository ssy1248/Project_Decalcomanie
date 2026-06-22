using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [SerializeField] private GameObject decalPrefab;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isGrounded;
    private float hopTimer;

    private SlimeState currentState = SlimeState.Normal;
    private string currentSlimePattern = "Empty";

    // 슬라임이 현재 흡수한 이미지 데이터
    private Texture2D currentSlimeTexture;

    // 슬라임 박스의 외형을 바꾸기 위한 렌더러
    private MeshRenderer slimeRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        slimeRenderer = GetComponent<MeshRenderer>(); // 렌더러 캐싱

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
    }

    private void FixedUpdate()
    {
        EvaluateGround();
        ApplySuspension();
        ApplyMovement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == SlimeState.Dashing)
        {
            if (collision.gameObject.TryGetComponent<Wall>(out Wall hitWall))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                ContactPoint contact = collision.contacts[0];
                Vector3 impactPoint = contact.point;
                Vector3 impactNormal = contact.normal; // 벽이 바라보는 방향 벡터

                // [상태 A] 슬라임이 비어있으면 벽의 패턴과 텍스처를 복사
                if (currentSlimePattern == "Empty" && hitWall.GetPatternID() != "Empty")
                {
                    currentSlimePattern = hitWall.GetPatternID();
                    currentSlimeTexture = hitWall.GetTexture();

                    // 체크리스트 1 달성: 슬라임 박스의 외형 텍스처를 실시간 교체
                    if (slimeRenderer != null && currentSlimeTexture != null)
                    {
                        slimeRenderer.material.SetTexture("_BaseMap", currentSlimeTexture);
                    }

                    Debug.Log($"<color=cyan>[벽지 패턴 {currentSlimePattern} 시각 데이터 복사 완료]</color>");
                }
                // [상태 B] 슬라임이 이미 패턴을 들고 있다면 빈 벽에 도장을 찍고 데이터를 넘김
                else if (currentSlimePattern != "Empty" && hitWall.GetPatternID() == "Empty")
                {
                    // 1. 벽 스크립트에 데이터 전송
                    hitWall.SetPattern(currentSlimePattern, currentSlimeTexture);

                    // 2. 체크리스트 2 & 4 달성: 정확한 좌표와 비뚤어진 충돌 각도를 반영하여 데칼 생성
                    if (decalPrefab != null)
                    {
                        // 데칼 투사 정렬: 데칼의 Forward(Z축)가 벽 내부(-impactNormal)를 향하게 정렬하되,
                        // 위쪽 축(Up)을 슬라임의 진행/몸체 방향(transform.up)과 정렬하면 비뚤게 부딪혔을 때 도장도 비뚤게 각도가 들어갑니다.
                        Quaternion decalRotation = Quaternion.LookRotation(-impactNormal, Vector3.up);

                        // 약간의 오프셋을 주어 벽면 살짝 앞에 생성 (Z-Fighting 방지)
                        Vector3 spawnPos = impactPoint + (impactNormal * 0.02f);

                        GameObject spawnedDecal = Instantiate(decalPrefab, spawnPos, decalRotation);

                        // 생성된 데칼 프로젝터에 슬라임이 가졌던 무늬 텍스처 주입
                        if (spawnedDecal.TryGetComponent<DecalProjector>(out DecalProjector projector))
                        {
                            // 인스턴스화된 머티리얼을 복사하여 독립적인 텍스처 지정
                            projector.material = new Material(projector.material);
                            projector.material.SetTexture("_BaseMap", currentSlimeTexture);
                        }
                    }

                    // 전송 후 다시 빈 상태로 리셋 및 슬라임 외형 복구
                    currentSlimePattern = "Empty";
                    currentSlimeTexture = null;
                    if (slimeRenderer != null) slimeRenderer.material.SetTexture("_BaseMap", null);
                }

                EndBodySlam();
            }
        }
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

    private void EndBodySlam()
    {
        currentState = SlimeState.Normal;
        rb.useGravity = true;
    }
}