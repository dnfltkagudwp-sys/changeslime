using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SlimeMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 9f;

    [Header("바닥 판정")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("슬라임 탄성 연출 (Squash & Stretch)")]
    [SerializeField] private Transform visualTransform; // 크기가 변형될 스프라이트 오브젝트
    [SerializeField] private float stretchSpeed = 10f;
    [SerializeField] private float floatyBobSpeed = 3f;   // 기체 상태 둥실둥실 펄스 속도
    [SerializeField] private float floatyBobAmount = 0.08f; // 기체 상태 둥실둥실 펄스 폭

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private Vector3 originalScale;
    private Vector3 originalLocalPosition;
    private float moveSpeedMultiplier = 1f;
    private bool jumpEnabled = true;
    private float squashIntensity = 1f;
    private bool useFloatyBob = false;
    private bool verticalVelocityOverrideActive = false;
    private float overriddenVerticalVelocity = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Visual 오브젝트가 별도로 지정되지 않았다면 본체의 트랜스폼 사용
        if (visualTransform == null)
            visualTransform = transform;

        originalScale = visualTransform.localScale;
        originalLocalPosition = visualTransform.localPosition;
    }

    private void Update()
    {
        // 1. 좌우 입력 받기
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. 바닥 착지 체크 (Physics2D.OverlapCircle 활용)
        if (groundCheckPoint != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);
        }

        // 3. 점프 입력 (바닥에 있고, 점프가 허용된 상태일 때만 가능)
        if (Input.GetButtonDown("Jump") && isGrounded && jumpEnabled)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 4. 슬라임 모양 변형(탄성) 눈속임 연출
        ApplySquashAndStretch();
    }

    private void FixedUpdate()
    {
        // 수직 속도: 기체 상태처럼 강제 등속 부유 중이면 매 프레임 그 값을 다시 강제하고,
        // 아니면 중력/충돌 등 평소 물리 결과(rb.linearVelocity.y)를 그대로 둠
        float verticalVelocity = verticalVelocityOverrideActive ? overriddenVerticalVelocity : rb.linearVelocity.y;

        // 수평 물리 이동 (가감속 없이 반응성 좋게 이동), 상태별 배속 적용
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed * moveSpeedMultiplier, verticalVelocity);
    }

    /// <summary>
    /// 슬라임 상태(액체/고체/기체)에 따라 이동 속도 배율과 점프 가능 여부를 조절하기 위해 SlimeStateController가 호출
    /// </summary>
    public void SetStateModifiers(float speedMultiplier, bool canJump)
    {
        moveSpeedMultiplier = speedMultiplier;
        jumpEnabled = canJump;
    }

    /// <summary>
    /// 기체 상태처럼 중력과 무관하게 일정한 수직 속도를 강제로 유지해야 할 때 SlimeStateController가 호출.
    /// active가 false면 평소처럼 중력/충돌에 의한 수직 속도를 그대로 사용함.
    /// </summary>
    public void SetVerticalVelocityOverride(bool active, float velocity)
    {
        verticalVelocityOverrideActive = active;
        overriddenVerticalVelocity = velocity;
    }

    /// <summary>
    /// 상태별로 스쿼시&스트레치 연출의 성격을 바꾸기 위해 SlimeStateController가 호출.
    /// squashIntensity: 속도 기반 변형의 강도 (0 = 거의 변형 없이 딱딱함, 1 = 원래의 말랑한 액체 느낌)
    /// floatyBob: true면 속도 기반 변형 대신 위아래로 완만하게 부풀었다 줄었다 하는 둥실둥실 펄스를 사용
    /// </summary>
    public void SetSquashStyle(float intensity, bool floatyBob)
    {
        squashIntensity = Mathf.Clamp01(intensity);
        useFloatyBob = floatyBob;
    }

    private void ApplySquashAndStretch()
    {
        Vector3 targetScale;
        Vector3 targetLocalPosition = originalLocalPosition;

        if (useFloatyBob)
        {
            // 기체: 둥실둥실 떠다니는 느낌의 완만한 펄스 애니메이션 (속도와 무관, 위치 보정 없이 제자리에서 부풀었다 줄었다 함)
            float bob = Mathf.Sin(Time.time * floatyBobSpeed) * floatyBobAmount;
            targetScale = new Vector3(originalScale.x * (1f - bob), originalScale.y * (1f + bob), originalScale.z);
        }
        else
        {
            Vector3 deformedScale = originalScale;

            // 점프로 위로 솟구칠 때: 세로로 길쭉하게 (X 축소, Y 확대)
            if (rb.linearVelocity.y > 0.5f && !isGrounded)
            {
                deformedScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.25f, originalScale.z);
            }
            // 낙하 중일 때: 원래 크기로 서서히 복귀
            else if (rb.linearVelocity.y < -0.5f && !isGrounded)
            {
                deformedScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.1f, originalScale.z);
            }
            // 바닥을 기어갈 때: 살짝 납작하게 (X 확대, Y 축소)
            else if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
            {
                deformedScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z);
            }

            // squashIntensity로 변형 강도 조절 (고체는 0에 가까워 거의 안 변형되어 단단해 보임)
            targetScale = Vector3.Lerp(originalScale, deformedScale, squashIntensity);

            // 발밑(바닥 닿는 면) 기준으로 앵커링: Y 스케일이 줄어들거나 늘어나도
            // 중심(pivot) 기준으로만 늘고 줄면 밑면이 떠 보이므로, 그만큼 위치를 보정해 밑면을 고정함
            float verticalAnchorOffset = (targetScale.y - originalScale.y) * 0.5f;
            targetLocalPosition = originalLocalPosition + new Vector3(0f, verticalAnchorOffset, 0f);
        }

        // 부드럽게 복원/변형
        visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, targetScale, Time.deltaTime * stretchSpeed);
        visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, targetLocalPosition, Time.deltaTime * stretchSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 바닥 감지 영역 확인용
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckPoint.position, checkRadius);
        }
    }
}