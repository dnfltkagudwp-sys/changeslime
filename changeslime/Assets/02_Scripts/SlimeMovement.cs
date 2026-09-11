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
    [SerializeField] private float wobbleStiffness = 150f; // 액체 젤리 흔들림: 목표 모양으로 당기는 힘 (클수록 빠르게 반응)
    [SerializeField] private float wobbleDamping = 10f;    // 액체 젤리 흔들림: 진동을 잦아들게 하는 감쇠 (작을수록 더 출렁임)
    [SerializeField] private float crawlCycleDistance = 0.6f; // 이 거리(유닛)를 이동할 때마다 꾸물거림 한 주기
    [SerializeField] private float crawlStretchAmount = 0.12f; // 꾸물거릴 때 늘어나고 움츠러드는 정도
    [SerializeField] private float crawlLeanAmount = 0.05f;    // 꾸물거리는 리듬에 맞춰 이동 방향으로 살짝 쏠리는 정도

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
    private Vector3 scaleVelocity = Vector3.zero; // 젤리 흔들림 스프링 계산용
    private float crawlPhase = 0f; // 이동한 거리에 비례해 진행되는 꾸물거림 주기

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D col = GetComponent<Collider2D>();

        // Visual 오브젝트가 별도로 지정되지 않았다면 본체의 트랜스폼 사용
        if (visualTransform == null)
            visualTransform = transform;

        originalScale = visualTransform.localScale;
        originalLocalPosition = visualTransform.localPosition;

        // 이미지마다 여백/트림/피벗이 달라 발밑이 콜라이더 바닥과 어긋날 수 있으므로,
        // 실제 렌더링된 스프라이트 경계를 기준으로 발밑을 콜라이더 바닥에 자동으로 맞춤
        SnapVisualToGroundBounds();

        // 벽에 붙어 눌린 상태로 점프할 때 마찰 때문에 상승 속도가 깎여 걸리는 문제 방지
        if (col.sharedMaterial == null || col.sharedMaterial.friction != 0f)
        {
            col.sharedMaterial = new PhysicsMaterial2D("SlimeNoFriction") { friction = 0f, bounciness = 0f };
        }
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
    /// 스프라이트가 바뀔 때(상태 전환 등)마다 SlimeStateController가 호출.
    /// 이미지마다 여백/트림/피벗이 달라도 실제 렌더링 경계 기준으로 발밑을 콜라이더 바닥에 맞추고,
    /// 스쿼시&스트레치가 기준으로 삼는 원점(originalLocalPosition)도 그 위치로 다시 잡음.
    /// </summary>
    public void SnapVisualToGroundBounds()
    {
        Collider2D col = GetComponent<Collider2D>();
        SpriteRenderer sr = visualTransform.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null || col == null) return;

        float bottomCorrection = col.bounds.min.y - sr.bounds.min.y;
        visualTransform.position += new Vector3(0f, bottomCorrection, 0f);
        originalLocalPosition = visualTransform.localPosition;
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
        if (useFloatyBob)
        {
            // 기체: X/Y를 서로 다른 주파수·위상으로 흔들어서, 숨쉬듯 맞물려 부푸는 "풍선" 느낌 대신
            // 제멋대로 일렁이는 기체 특유의 흐트러진 느낌을 냄
            float bobX = Mathf.Sin(Time.time * floatyBobSpeed) * floatyBobAmount;
            float bobY = Mathf.Sin(Time.time * floatyBobSpeed * 1.3f + 1.5f) * floatyBobAmount;
            Vector3 bobScale = new Vector3(originalScale.x * (1f + bobX), originalScale.y * (1f + bobY), originalScale.z);

            // 제자리에 고정되지 않고 천천히 위아래로 떠다니는 느낌 추가
            float driftY = Mathf.Sin(Time.time * floatyBobSpeed * 0.7f) * floatyBobAmount * 0.5f;
            Vector3 bobLocalPosition = originalLocalPosition + new Vector3(0f, driftY, 0f);

            visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, bobScale, Time.deltaTime * stretchSpeed);
            visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, bobLocalPosition, Time.deltaTime * stretchSpeed);

            scaleVelocity = Vector3.zero; // 다른 상태로 돌아갔을 때 잔여 스프링 속도가 남지 않도록 초기화
            return;
        }

        Vector3 deformedScale = originalScale;
        float leanX = 0f;

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
        // 바닥을 기어갈 때: 자벌레/달팽이처럼 이동한 거리에 비례해 늘어났다 움츠러들었다를 반복
        else if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            crawlPhase += Mathf.Abs(rb.linearVelocity.x) * Time.deltaTime * (2f * Mathf.PI / Mathf.Max(crawlCycleDistance, 0.01f));
            float crawl = Mathf.Sin(crawlPhase); // -1(움츠러듦) ~ 1(쭉 늘어남)

            deformedScale = new Vector3(
                originalScale.x * (1.15f + crawlStretchAmount * crawl),
                originalScale.y * (0.85f - crawlStretchAmount * 0.6f * crawl),
                originalScale.z);

            // 늘어나는 타이밍에 맞춰 이동 방향으로 살짝 쏠렸다가, 움츠러들 때 따라붙는 느낌
            leanX = crawlLeanAmount * crawl * Mathf.Sign(horizontalInput);
        }

        // squashIntensity로 변형 강도 조절 (고체는 0에 가까워 거의 안 변형되어 단단해 보임)
        Vector3 targetScale = Vector3.Lerp(originalScale, deformedScale, squashIntensity);

        // 액체 특유의 말랑한 젤리 흔들림: 목표 크기로 그냥 다가가는 대신 스프링처럼 살짝 지나쳤다 잦아들게 함
        // (고체처럼 squashIntensity가 낮으면 당기는 힘도 함께 약해져 자연히 뻣뻣하고 흔들림 없는 느낌이 됨)
        Vector3 toTarget = targetScale - visualTransform.localScale;
        scaleVelocity += toTarget * (wobbleStiffness * squashIntensity) * Time.deltaTime;
        scaleVelocity *= Mathf.Clamp01(1f - wobbleDamping * Time.deltaTime);
        visualTransform.localScale += scaleVelocity * Time.deltaTime;

        // 발밑(바닥 닿는 면) 기준으로 앵커링: Y 스케일이 줄어들거나 늘어나도(흔들림 도중에도)
        // 중심(pivot) 기준으로만 늘고 줄면 밑면이 떠 보이므로, 실제 현재 스케일 기준으로 위치를 보정해 밑면을 고정함
        float verticalAnchorOffset = (visualTransform.localScale.y - originalScale.y) * 0.5f;
        Vector3 targetLocalPosition = originalLocalPosition + new Vector3(leanX * squashIntensity, verticalAnchorOffset, 0f);
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