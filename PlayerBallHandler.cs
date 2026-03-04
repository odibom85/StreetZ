using UnityEngine;

public class PlayerBallHandler : MonoBehaviour
{
    [Header("Hand Slots")]
    public Transform LeftHandSlot;    
    public Transform RightHandSlot;   
    private Transform _activeSlot;

    [Header("Ball References")]
    public GameObject Ball;
    private Rigidbody _ballRb;
    public PlayerShooting Shooting; // Added for state resetting

    [Header("Dribble Settings")]
    public float BaseDribbleSpeed = 4f;   
    public float FatigueSpeedMultiplier = 0.6f; 
    public float HandFollowSmooth = 0.08f; 
    public LayerMask FloorLayer;        

    [Header("State")]
    public bool HasBall = true;
    public bool IsRightHanded = true;

    private float _dribbleCycle;
    private Vector3 _followVelocity; 
    private Vector3 _currentBallPos;
    private bool _isSprinting;
    private float _staminaPercent = 1f;

    void Start()
    {
        if (Ball != null)
        {
            _ballRb = Ball.GetComponent<Rigidbody>();
            _activeSlot = RightHandSlot;
            _currentBallPos = Ball.transform.position;
            
            if (Shooting == null) Shooting = GetComponent<PlayerShooting>();

            if (HasBall) GainPossession();
            else ReleaseBallForShot(); 
        }
    }

    void Update()
    {
        // Your logic: Only handle the visual dribbling if we have the ball
        if (HasBall && _activeSlot != null) HandleFatiguedDribble();
    }

    private void HandleFatiguedDribble()
    {
        float fatigueFactor = Mathf.Lerp(FatigueSpeedMultiplier, 1.0f, _staminaPercent);
        float sprintFactor = _isSprinting ? 1.4f : 1.0f;
        float currentSpeed = BaseDribbleSpeed * fatigueFactor * sprintFactor;
        
        _dribbleCycle += Time.deltaTime * currentSpeed;
        float bounceValue = Mathf.Abs(Mathf.Sin(_dribbleCycle));
        
        float floorY = 0.12f;
        if (Physics.Raycast(_activeSlot.position, Vector3.down, out RaycastHit hit, 2f, FloorLayer))
            floorY = hit.point.y + 0.12f;

        float maxHeight = Mathf.Lerp(_activeSlot.position.y - 0.2f, _activeSlot.position.y, _staminaPercent);
        float targetY = Mathf.Lerp(floorY, maxHeight, bounceValue);

        _currentBallPos = Vector3.SmoothDamp(_currentBallPos, _activeSlot.position, ref _followVelocity, HandFollowSmooth);
        Ball.transform.position = new Vector3(_currentBallPos.x, targetY, _currentBallPos.z);
    }

    // This fulfills the requirement for the GameManager
    public void GrabBall()
    {
        GainPossession();
    }

    public void GainPossession()
    {
        HasBall = true;
        if (_ballRb != null) 
        { 
            _ballRb.isKinematic = true; 
            _ballRb.useGravity = false; 
        }
        _currentBallPos = _activeSlot.position;
        
        // Ensure shooting script stops any active charging/jumping when ball is grabbed
        if (Shooting != null) Shooting.ResetShootingState();
    }

    public void ReleaseBallForShot()
    {
        HasBall = false;
        if (_ballRb != null) 
        { 
            _ballRb.isKinematic = false; 
            _ballRb.useGravity = true; 
        }
    }

    public void SwitchHand(bool toRight)
    {
        IsRightHanded = toRight;
        _activeSlot = toRight ? RightHandSlot : LeftHandSlot;
        _dribbleCycle = 0.5f; 
    }

    public void UpdateStamina(float percent) => _staminaPercent = percent;
    public void SetSprinting(bool sprinting) => _isSprinting = sprinting;
    public void HoldBallTight() 
    { 
        if (_ballRb != null) _ballRb.isKinematic = true; 
        Ball.transform.position = _activeSlot.position; 
    }
}