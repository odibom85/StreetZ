using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AIController : MonoBehaviour
{
    public enum AIState { Idle, Offense, Defense }
    
    [Header("State")]
    public AIState CurrentState = AIState.Idle;

    [Header("References")]
    public Transform HoopTarget;
    public Transform PlayerTarget;
    private NavMeshAgent _agent;
    private PlayerBallHandler _ballHandler;
    private PlayerShooting _shooting;

    [Header("AI Settings")]
    public float ShootChance = 0.4f;      // 0 to 1 chance of shooting when open
    public float DriveAggression = 0.7f;  // How often it prefers driving over shooting
    public float DefendDistance = 2.5f;   // How close it stays to you on defense

    private float _decisionTimer;
    private bool _isThinking = false;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _ballHandler = GetComponent<PlayerBallHandler>();
        _shooting = GetComponent<PlayerShooting>();
    }

    void Update()
    {
        if (!BasketballGameManager.Instance.BallInPlay) return;

        // Determine state based on ball possession
        if (_ballHandler.HasBall)
            CurrentState = AIState.Offense;
        else
            CurrentState = AIState.Defense;

        switch (CurrentState)
        {
            case AIState.Offense:
                HandleOffense();
                break;
            case AIState.Defense:
                HandleDefense();
                break;
        }
    }

    private void HandleOffense()
    {
        float distToHoop = Vector3.Distance(transform.position, HoopTarget.position);
        float distToPlayer = Vector3.Distance(transform.position, PlayerTarget.position);

        _decisionTimer -= Time.deltaTime;

        if (_decisionTimer <= 0 && !_isThinking)
        {
            _isThinking = true;
            StartCoroutine(MakeOffensiveDecision(distToHoop, distToPlayer));
        }
    }

    private IEnumerator MakeOffensiveDecision(float distToHoop, float distToPlayer)
    {
        // 1. If very close to hoop, try to Dunk/Layup
        if (distToHoop < _shooting.MaxDunkDistance + 1f)
        {
            _ballHandler.SetSprinting(true); // Trigger sprint for dunk
            _agent.SetDestination(HoopTarget.position);
            yield return new WaitForSeconds(0.5f);
            _shooting.OnShootSimulated(true);
            yield return new WaitForSeconds(Random.Range(0.4f, 0.5f));
            _shooting.OnShootSimulated(false);
        }
        // 2. If open at mid-range, shoot
        else if (distToPlayer > 3.5f && distToHoop < 12f && Random.value < ShootChance)
        {
            _agent.isStopped = true;
            _shooting.OnShootSimulated(true);
            yield return new WaitForSeconds(Random.Range(0.32f, 0.45f));
            _shooting.OnShootSimulated(false);
            _agent.isStopped = false;
        }
        // 3. Otherwise, Drive to hoop
        else
        {
            _ballHandler.SetSprinting(distToPlayer < 4f); // Sprint if player is close
            _agent.SetDestination(HoopTarget.position);
            
            // Randomly switch hands while driving
            if (Random.value < 0.1f) _ballHandler.SwitchHand(!_ballHandler.IsRightHanded);
        }

        _decisionTimer = Random.Range(0.8f, 1.5f);
        _isThinking = false;
    }

    private void HandleDefense()
    {
        // Calculate a point between the player and the hoop
        Vector3 guardPosition = Vector3.Lerp(PlayerTarget.position, HoopTarget.position, 0.4f);
        _agent.SetDestination(guardPosition);

        // If close to player, put hands up to contest
        float distToPlayer = Vector3.Distance(transform.position, PlayerTarget.position);
        _shooting.IsDefending = (distToPlayer < DefendDistance);

        // Speed up to keep up with player
        _agent.speed = _shooting.IsDefending ? 3.5f : 6f; 
    }
}