using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AIDefender : MonoBehaviour
{
    [Header("References")]
    public Transform Player;
    public PlayerBallHandler PlayerBall;
    public Transform Hoop;

    [Header("Flow Movement")]
    public float MaxMoveSpeed = 5.0f;
    public float SmoothTime = 0.2f; // Increased for a more natural "lag" in reaction
    private Vector3 _currentVelocity;

    [Header("Defensive Logic")]
    public float DefensiveCushion = 3.0f; // Gap between AI and Player
    public float Gravity = -30f;
    
    [Header("Steal Settings")]
    public float StealReach = 1.6f;        
    public float StealCooldown = 2.0f;
    [Range(0, 100)] public float StealChance = 35f;

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _stealTimer;

    void Awake() => _cc = GetComponent<CharacterController>();

    void Update()
    {
        if (Player == null || Hoop == null) return;

        ApplyGravity();
        HandleFlowMovement();
        
        if (_stealTimer > 0) _stealTimer -= Time.deltaTime;
        else CheckForStealOpportunity();
    }

    private void ApplyGravity()
    {
        if (_cc.isGrounded) _verticalVelocity = -2f;
        else _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void HandleFlowMovement()
    {
        // 1. Calculate the ideal defensive spot (between ball and hoop)
        Vector3 ballPos = PlayerBall.HasBall ? PlayerBall.Ball.transform.position : Player.position;
        Vector3 dirToHoop = (Hoop.position - ballPos).normalized;
        Vector3 targetPos = ballPos + (dirToHoop * DefensiveCushion);
        
        // 2. Shading Logic
        float shadeOffset = PlayerBall.IsRightHanded ? -0.8f : 0.8f;
        Vector3 rightDir = Vector3.Cross(Vector3.up, dirToHoop);
        targetPos += rightDir * shadeOffset;
        targetPos.y = transform.position.y; 

        // 3. SmoothDamp (Prevents the "Janky" sticking behavior)
        Vector3 nextPos = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, SmoothTime, MaxMoveSpeed);
        Vector3 moveDelta = nextPos - transform.position;
        
        // 4. Combine with Gravity
        moveDelta.y = _verticalVelocity * Time.deltaTime;
        _cc.Move(moveDelta);

        // 5. Look Rotation
        Vector3 lookDir = ballPos - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
        }
    }

    private void CheckForStealOpportunity()
    {
        if (!PlayerBall.HasBall) return;

        float ballDist = Vector3.Distance(transform.position, PlayerBall.Ball.transform.position);
        
        if (ballDist <= StealReach)
        {
            // FIXED: CharacterController.velocity is the correct property
            bool isStandingStill = Player.GetComponent<CharacterController>().velocity.magnitude < 0.2f;
            
            Vector3 dirToDefender = (transform.position - Player.position).normalized;
            float sideDot = Vector3.Dot(Player.right, dirToDefender);
            bool isExposed = (PlayerBall.IsRightHanded && sideDot > 0.25f) || (!PlayerBall.IsRightHanded && sideDot < -0.25f);

            if (isExposed || isStandingStill)
            {
                if (Random.Range(0, 100) < StealChance) PerformSteal();
                else _stealTimer = 0.6f; // AI hesitation
            }
        }
    }

    private void PerformSteal()
    {
        _stealTimer = StealCooldown;
        PlayerBall.ReleaseBallForShot(); 
        
        Rigidbody rb = PlayerBall.Ball.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 pokeDir = (transform.forward + (Random.insideUnitSphere * 0.3f)).normalized;
        rb.AddForce(pokeDir * 6f, ForceMode.Impulse);
        Debug.Log("Steal Success!");
    }
}