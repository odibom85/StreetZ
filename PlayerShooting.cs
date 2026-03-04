using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Cinemachine; // Required for the Camera Zoom logic

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    public PlayerBallHandler BallHandler;
    public Transform HoopTarget; 
    public Transform OpponentTransform; // Drag the AI here (and Player here for the AI)
    public CinemachineVirtualCamera VCam; // Drag the "Cinemachine Camera" here
    private CharacterController _cc;
    private TrailRenderer _ballTrail;

    [Header("Dunk & Sprint Settings")]
    public float MaxDunkDistance = 4.0f;
    public float RequiredSpeedForDunk = 4.0f; 
    public float DunkJumpHeight = 2.4f;
    public float DunkSpeed = 8f;
    public float DunkPerfectTime = 0.45f;
    public Transform DunkSlamPoint; 

    [Header("Defense & Contest")]
    public bool IsDefending = false; 
    public float MaxContestDist = 5.0f; 
    [Range(0, 1)] public float MaxWindowShrink = 0.6f; 
    public float BaseBlockChance = 0.25f; 
    private float _currentContestLevel = 0f;

    [Header("Cinematic Zoom")]
    public float NormalFOV = 60f;
    public float ChargeZoomFOV = 52f;
    public float PerfectSwishFOV = 45f;
    public float DunkZoomFOV = 42f; 
    public float ZoomSpeed = 5f;

    [Header("UI Shot Meter")]
    public GameObject MeterRoot; 
    public Image MeterFill;      
    public TextMeshProUGUI FeedbackText; 
    public Color NormalColor = Color.yellow;
    public Color LayupBaseColor = Color.cyan;
    public Color DunkBaseColor = Color.magenta;
    public Color PerfectColor = Color.green;
    public Color BadColor = Color.red;

    [Header("Jump Shot Settings")]
    public float PerfectReleaseTime = 0.35f; 
    public float JumpHeight = 1.3f;     
    public float GlobalGravity = -18f; 
    public float HangTimeFactor = 1.1f; 

    [Header("Layup Settings")]
    public float LayupPerfectTime = 0.3f; 
    public float MaxLayupDistance = 6.0f;     
    public float LayupJumpHeight = 1.6f;    
    public float LayupForwardLeap = 3.5f; 

    [Header("The Green Arc")]
    public float SwishSpeed = 18f; 

    [Header("State")]
    public bool IsCharging = false;
    public bool IsDrivingLayup = false; 
    public bool IsDunking = false;
    private float _chargeTimer = 0f;
    private float _currentPerfectTime;
    private Vector3 _initialPos;
    private float _targetFOV;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (MeterRoot != null) MeterRoot.SetActive(false);
        
        if (BallHandler != null && BallHandler.Ball != null)
            _ballTrail = BallHandler.Ball.GetComponent<TrailRenderer>();
        
        _targetFOV = NormalFOV;
    }

    // --- INPUTS ---
    public void OnShoot(InputValue v) => HandleShootInput(v.isPressed);
    public void OnLayup(InputValue v) => HandleLayupInput(v.isPressed);
    
    // --- AI SIMULATION ---
    public void OnShootSimulated(bool pressed) => HandleShootInput(pressed);

    private void HandleShootInput(bool isPressed)
    {
        if (isPressed && BallHandler.HasBall)
        {
            float dist = Vector3.Distance(transform.position, HoopTarget.position);
            float currentMoveSpeed = _cc != null ? _cc.velocity.magnitude : 0f;

            if (dist <= MaxDunkDistance && currentMoveSpeed >= RequiredSpeedForDunk)
                StartCharging(false, true);
            else
                StartCharging(false, false);
        }
        else if (!isPressed && IsCharging)
        {
            ReleaseShot();
        }
    }

    private void HandleLayupInput(bool isPressed)
    {
        if (isPressed && BallHandler.HasBall)
        {
            float dist = Vector3.Distance(transform.position, HoopTarget.position);
            StartCharging(dist <= MaxLayupDistance, false); 
        }
        else if (!isPressed && IsCharging) ReleaseShot(); 
    }

    private void StartCharging(bool forceLayup, bool forceDunk)
    {
        IsCharging = true;
        IsDrivingLayup = forceLayup;
        IsDunking = forceDunk;
        _chargeTimer = 0f;
        _initialPos = transform.position;

        if (IsDunking) _currentPerfectTime = DunkPerfectTime;
        else if (IsDrivingLayup) _currentPerfectTime = LayupPerfectTime;
        else _currentPerfectTime = PerfectReleaseTime;

        if (MeterRoot != null)
        {
            MeterRoot.SetActive(true);
            MeterFill.fillAmount = 0;
            if (IsDunking) MeterFill.color = DunkBaseColor;
            else MeterFill.color = IsDrivingLayup ? LayupBaseColor : NormalColor;
        }

        if (_ballTrail != null) _ballTrail.emitting = false;
        _targetFOV = IsDunking ? DunkZoomFOV : ChargeZoomFOV;
    }

    private float CalculateContest()
    {
        if (OpponentTransform == null) return 0f;

        PlayerShooting opponentScript = OpponentTransform.GetComponent<PlayerShooting>();
        // Only contest if the opponent is actually holding their hands up (Shift)
        if (opponentScript != null && !opponentScript.IsDefending) return 0f;

        float dist = Vector3.Distance(transform.position, OpponentTransform.position);
        if (dist > MaxContestDist) return 0f;

        return Mathf.Clamp01(1f - (dist / MaxContestDist));
    }

    private void ReleaseShot()
    {
        IsCharging = false;
        if (MeterRoot != null) MeterRoot.SetActive(false);

        _currentContestLevel = CalculateContest();
        
        bool isBlocked = false;
        if (_currentContestLevel > 0.4f && Random.value < (_currentContestLevel * BaseBlockChance)) 
            isBlocked = true;

        float timingDiff = Mathf.Abs(_chargeTimer - _currentPerfectTime);
        bool isPerfect = timingDiff < 0.06f; 

        if (IsDunking)
        {
            bool dunkSuccess = !isBlocked && timingDiff < 0.25f; 
            StartCoroutine(ExecuteDunkSequence(dunkSuccess));
        }
        else
        {
            BallHandler.ReleaseBallForShot();
            if (_ballTrail != null)
            {
                _ballTrail.emitting = true;
                _ballTrail.startColor = isBlocked ? BadColor : (isPerfect ? PerfectColor : NormalColor);
            }

            if (isBlocked) ApplyBlockPhysics();
            else if (isPerfect) StartCoroutine(PerfectArcRoutine(BallHandler.Ball));
            else
            {
                Rigidbody rb = BallHandler.Ball.GetComponent<Rigidbody>();
                rb.linearVelocity = CalculatePhysicsVelocity(timingDiff < 0.12f, IsDrivingLayup ? 70f : 58f, _currentContestLevel * 0.45f);
                rb.AddTorque(transform.right * 8f, ForceMode.Impulse);
            }
        }

        if (FeedbackText != null)
        {
            string msg = isBlocked ? "BLOCKED!" : (isPerfect ? "PERFECT!" : "Released");
            FeedbackText.text = msg + $"\n({(_currentContestLevel * 100):0}% Contested)";
        }

        IsDunking = false;
        IsDrivingLayup = false;
        _targetFOV = NormalFOV;
        StartCoroutine(DisableTrailAfterTime(1.5f));
    }

    private IEnumerator ExecuteDunkSequence(bool success)
    {
        BallHandler.ReleaseBallForShot();
        Rigidbody rb = BallHandler.Ball.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        Vector3 startBallPos = BallHandler.Ball.transform.position;
        Vector3 endBallPos = DunkSlamPoint != null ? DunkSlamPoint.position : HoopTarget.position + Vector3.up * 0.8f;

        float t = 0;
        while (t < 1.0f)
        {
            t += Time.deltaTime * DunkSpeed;
            BallHandler.Ball.transform.position = Vector3.Lerp(startBallPos, endBallPos, t);
            yield return null;
        }

        rb.isKinematic = false;
        if (success)
        {
            rb.linearVelocity = Vector3.down * 18f; 
            BasketballGameManager.Instance.AddScore(2, gameObject.CompareTag("Player"));
        }
        else
        {
            rb.linearVelocity = (Vector3.up * 6f) + (transform.forward * -4f); 
        }
    }

    public void ResetShootingState()
    {
        IsCharging = false;
        IsDrivingLayup = false;
        IsDunking = false;
        _targetFOV = NormalFOV;
        StopAllCoroutines();
        if (MeterRoot != null) MeterRoot.SetActive(false);
        if (_ballTrail != null) _ballTrail.emitting = false;
    }

    private void ApplyBlockPhysics()
    {
        Rigidbody rb = BallHandler.Ball.GetComponent<Rigidbody>();
        Vector3 blockDir = (transform.position - HoopTarget.position).normalized;
        blockDir.y = 0.6f; 
        rb.linearVelocity = blockDir * 9f;
    }

    private IEnumerator DisableTrailAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_ballTrail != null && !IsCharging) _ballTrail.emitting = false;
    }

    private IEnumerator PerfectArcRoutine(GameObject ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.isKinematic = true; 
        Vector3 startPos = ball.transform.position;
        Vector3 endPos = HoopTarget.position;
        float dist = Vector3.Distance(startPos, endPos);
        Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * (dist * 0.55f);

        float t = 0;
        float duration = dist / SwishSpeed; 
        while (t < 1.0f)
        {
            t += Time.deltaTime / duration;
            Vector3 m1 = Vector3.Lerp(startPos, midPoint, t);
            Vector3 m2 = Vector3.Lerp(midPoint, endPos, t);
            ball.transform.position = Vector3.Lerp(m1, m2, t);
            yield return null;
        }
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.down * 4f; 
        
        BasketballGameManager.Instance.AddScore(2, gameObject.CompareTag("Player"));
        
        yield return new WaitForSeconds(0.5f);
        _targetFOV = NormalFOV;
    }

    private Vector3 CalculatePhysicsVelocity(bool close, float angle, float contestOffset = 0)
    {
        Vector3 target = HoopTarget.position;
        float errorDir = (_chargeTimer > _currentPerfectTime) ? 1.2f : -0.85f;
        float offset = (close ? 0.14f : 0.45f) + contestOffset; 
        Vector3 dirToPlayer = (transform.position - HoopTarget.position).normalized;
        target += dirToPlayer * (errorDir * offset);

        Vector3 origin = BallHandler.Ball.transform.position;
        float g = Mathf.Abs(GlobalGravity);
        float a = angle * Mathf.Deg2Rad;
        Vector3 dir = target - origin;
        float h = dir.y; dir.y = 0;
        float dist = dir.magnitude;
        float v2 = (g * dist * dist) / (2 * Mathf.Pow(Mathf.Cos(a), 2) * (dist * Mathf.Tan(a) - h));
        float v = Mathf.Sqrt(Mathf.Abs(v2));
        Vector3 velocity = dir.normalized * v * Mathf.Cos(a);
        velocity.y = v * Mathf.Sin(a);
        return velocity;
    }

    void Update()
    {
        // 1. CAMERA ZOOM (ONLY FOR PLAYER)
        if (gameObject.CompareTag("Player") && VCam != null)
        {
            VCam.m_Lens.FieldOfView = Mathf.Lerp(VCam.m_Lens.FieldOfView, _targetFOV, ZoomSpeed * Time.deltaTime);
        }

        // 2. DEFENSE TOGGLE (ONLY FOR PLAYER)
        if (!BallHandler.HasBall && gameObject.CompareTag("Player"))
        {
            // Holding Left Shift raises hands
            IsDefending = Keyboard.current.leftShiftKey.isPressed;
        }

        // 3. JUMP/SHOT PHYSICS
        if (IsCharging)
        {
            _chargeTimer += Time.deltaTime;
            if (MeterFill != null) MeterFill.fillAmount = Mathf.Clamp01(_chargeTimer / _currentPerfectTime);

            float progress = Mathf.Clamp01(_chargeTimer / (_currentPerfectTime * HangTimeFactor)); 
            float currentJumpHeight = IsDunking ? DunkJumpHeight : (IsDrivingLayup ? LayupJumpHeight : JumpHeight);
            float yOffset = Mathf.Sin(progress * Mathf.PI) * currentJumpHeight;
            
            Vector3 targetPos = _initialPos + (Vector3.up * yOffset);
            
            if (IsDrivingLayup || IsDunking)
            {
                Vector3 driveDir = (HoopTarget.position - _initialPos).normalized;
                driveDir.y = 0;
                float forwardLeap = IsDunking ? 2.5f : LayupForwardLeap;
                targetPos += driveDir * (progress * forwardLeap);
            }
            transform.position = targetPos;
        }
    }
}