using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SlimeMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("바닥 판정")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("슬라임 탄성 연출 (Squash & Stretch)")]
    [SerializeField] private Transform visualTransform; // 크기가 변형될 스프라이트 오브젝트
    [SerializeField] private float stretchSpeed = 10f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private Vector3 originalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Visual 오브젝트가 별도로 지정되지 않았다면 본체의 트랜스폼 사용
        if (visualTransform == null)
            visualTransform = transform;

        originalScale = visualTransform.localScale;
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

        // 3. 점프 입력 (바닥에 있을 때만 가능)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 4. 슬라임 모양 변형(탄성) 눈속임 연출
        ApplySquashAndStretch();
    }

    private void FixedUpdate()
    {
        // 수평 물리 이동 (가감속 없이 반응성 좋게 이동)
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void ApplySquashAndStretch()
    {
        Vector3 targetScale = originalScale;

        // 점프로 위로 솟구칠 때: 세로로 길쭉하게 (X 축소, Y 확대)
        if (rb.linearVelocity.y > 0.5f && !isGrounded)
        {
            targetScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.25f, originalScale.z);
        }
        // 낙하 중일 때: 원래 크기로 서서히 복귀
        else if (rb.linearVelocity.y < -0.5f && !isGrounded)
        {
            targetScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.1f, originalScale.z);
        }
        // 바닥을 기어갈 때: 살짝 납작하게 (X 확대, Y 축소)
        else if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            targetScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z);
        }

        // 부드럽게 복원/변형
        visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, targetScale, Time.deltaTime * stretchSpeed);
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