using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float WalkSpeed = 6f;
    public float SprintSpeed = 11f;
    public float BoostMultiplier = 1.4f; 
    public float Acceleration = 0.12f; 
    public float Deceleration = 0.2f; 
    public float SprintDeceleration = 0.4f;
    public float Gravity = -30f;      

    [Header("Stamina & Balance")]
    public Slider StaminaSlider;
    public float MaxStamina = 100f;
    public float CurrentStamina;
    public float DrainRate = 20f;    
    public float RegainRate = 15f;   
    public float BoostCost = 15f;    

    [Header("References")]
    public Transform CameraTransform;
    public Transform HoopTarget; 
    public PlayerBallHandler BallHandler;
    public PlayerShooting ShootingScript; 
    
    private CharacterController _cc;
    private Vector2 _moveIn;
    private Vector2 _lookIn;
    private Vector3 _moveVelocity;
    private Vector3 _smoothV;
    private float _verticalVelocity;
    
    private bool _flickReady = true;
    private bool _isSprintInput;
    private bool _isSprinting;
    private bool _wasSprintingLastFrame;
    private float _boostWindowTimer = 0f;
    private bool _canBoost = false;
    private float _activeBoostTimer = 0f;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        CurrentStamina = MaxStamina;
    }

    void Update()
    {
        HandleStamina();
        UpdateTimers();
        ApplyGravity();
        HandleStanceMovement();
        HandleRightStickCrossover();
    }

    private void UpdateTimers()
    {
        if (_boostWindowTimer > 0) _boostWindowTimer -= Time.deltaTime;
        else _canBoost = false;
        if (_activeBoostTimer > 0) _activeBoostTimer -= Time.deltaTime;
    }

    private void HandleStamina()
    {
        _wasSprintingLastFrame = _isSprinting;
        if (_isSprintInput && _canBoost && CurrentStamina > BoostCost) TriggerSpeedBoost();

        if (_isSprintInput && _moveIn.magnitude > 0.1f && CurrentStamina > 1f)
        {
            _isSprinting = true;
            CurrentStamina -= DrainRate * Time.deltaTime;
        }
        else
        {
            _isSprinting = false;
            if (CurrentStamina < MaxStamina) CurrentStamina += RegainRate * Time.deltaTime;
        }
        
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0, MaxStamina);
        if (StaminaSlider != null) StaminaSlider.value = CurrentStamina / MaxStamina;
        if (BallHandler != null) 
        {
            BallHandler.SetSprinting(_isSprinting);
            BallHandler.UpdateStamina(CurrentStamina / MaxStamina);
        }
    }

    private void TriggerSpeedBoost() { _canBoost = false; _boostWindowTimer = 0; _activeBoostTimer = 0.6f; CurrentStamina -= BoostCost; }

    private void HandleStanceMovement()
    {
        if (ShootingScript != null && (ShootingScript.IsCharging || ShootingScript.IsDrivingLayup)) return;

        Vector3 camForward = CameraTransform.forward;
        Vector3 camRight = CameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        
        Vector3 targetDir = (camForward.normalized * _moveIn.y + camRight.normalized * _moveIn.x);
        float targetSpeed = (_isSprinting ? SprintSpeed : WalkSpeed);
        if (_activeBoostTimer > 0) targetSpeed *= BoostMultiplier;

        float lerpTime = (_moveIn.magnitude > 0.1f) ? Acceleration : (_wasSprintingLastFrame ? SprintDeceleration : Deceleration);
        _moveVelocity = Vector3.SmoothDamp(_moveVelocity, targetDir * targetSpeed, ref _smoothV, lerpTime);

        Vector3 finalMove = _moveVelocity;
        finalMove.y = _verticalVelocity;
        _cc.Move(finalMove * Time.deltaTime);

        Vector3 faceDir = (HoopTarget.position - transform.position);
        faceDir.y = 0; 
        if (faceDir.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDir), 15f * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (ShootingScript != null && ShootingScript.IsCharging) return;
        if (_cc.isGrounded) _verticalVelocity = -2f;
        else _verticalVelocity += Gravity * Time.deltaTime;
    }

    private void HandleRightStickCrossover()
    {
        if (BallHandler == null || !BallHandler.HasBall) return;
        if (_lookIn.magnitude < 0.2f) { _flickReady = true; return; }
        if (!_flickReady) return;

        if (Mathf.Abs(_lookIn.x) > 0.6f)
        {
            BallHandler.SwitchHand(_lookIn.x > 0);
            if (CurrentStamina > BoostCost) { _canBoost = true; _boostWindowTimer = 0.5f; }
            CurrentStamina -= 5f; 
            _flickReady = false;
        }
    }

    public void OnMove(InputValue v) => _moveIn = v.Get<Vector2>();
    public void OnLook(InputValue v) => _lookIn = v.Get<Vector2>();
    public void OnSprint(InputValue v) => _isSprintInput = v.isPressed;
}
