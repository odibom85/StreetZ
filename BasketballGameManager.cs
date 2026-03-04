using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Added for collection support
using System; // Added for general system support

public class BasketballGameManager : MonoBehaviour
{
    public static BasketballGameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI ShotClockText;
    public GameObject GameOverPanel;

    [Header("Game State")]
    public int PlayerScore = 0;
    public int AIScore = 0;
    public float ShotClock = 24f;
    public bool BallInPlay = false;

    [Header("Setup")]
    public Transform PlayerSpawn;
    public Transform AISpawn;
    public PlayerShooting PlayerScript;
    public PlayerShooting AIScript; // AI uses same script
    public GameObject Basketball;
    public Rigidbody BallRb;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (BallRb == null && Basketball != null) BallRb = Basketball.GetComponent<Rigidbody>();
        UpdateUI();
        StartNewPossession(true); // Player starts first
    }

    private void Update()
    {
        if (BallInPlay)
        {
            ShotClock -= Time.deltaTime;
            if (ShotClockText != null) 
                ShotClockText.text = Mathf.Ceil(ShotClock).ToString();

            if (ShotClock <= 0)
            {
                HandleShotClockViolation();
            }
        }
    }

    public void AddScore(int points, bool isPlayer)
    {
        if (!BallInPlay) return; // Prevent double scoring

        if (isPlayer) PlayerScore += points;
        else AIScore += points;

        UpdateUI();

        if (PlayerScore >= 21 || AIScore >= 21)
        {
            EndGame();
        }
        else
        {
            // Scorer keeps the ball (Winners Ball)
            StartCoroutine(ResetAfterBucket(isPlayer));
        }
    }

    private IEnumerator ResetAfterBucket(bool scorerWasPlayer)
    {
        BallInPlay = false;
        yield return new WaitForSeconds(2f);
        StartNewPossession(scorerWasPlayer);
    }

    public void StartNewPossession(bool playerHasBall)
    {
        ShotClock = 24f;
        
        // Reset Physics
        BallRb.linearVelocity = Vector3.zero;
        BallRb.angularVelocity = Vector3.zero;

        // Reset Positions
        PlayerScript.transform.position = PlayerSpawn.position;
        AIScript.transform.position = AISpawn.position;

        // Face each other
        PlayerScript.transform.LookAt(new Vector3(AISpawn.position.x, PlayerScript.transform.position.y, AISpawn.position.z));
        AIScript.transform.LookAt(new Vector3(PlayerSpawn.position.x, AIScript.transform.position.y, PlayerSpawn.position.z));

        // Clear shooting states
        PlayerScript.ResetShootingState();
        AIScript.ResetShootingState();

        // Give ball to the correct person
        if (playerHasBall) PlayerScript.BallHandler.GrabBall();
        else AIScript.BallHandler.GrabBall();

        BallInPlay = true;
    }

    private void HandleShotClockViolation()
    {
        Debug.Log("SHOT CLOCK VIOLATION!");
        // If player had it, give to AI. If AI had it, give to player.
        bool playerHadBall = PlayerScript.BallHandler.HasBall;
        StartNewPossession(!playerHadBall);
    }

    private void UpdateUI()
    {
        if (ScoreText != null)
            ScoreText.text = $"PLAYER: {PlayerScore}  |  AI: {AIScore}";
    }

    private void EndGame()
    {
        BallInPlay = false;
        if (GameOverPanel != null) GameOverPanel.SetActive(true);
        Debug.Log("Game Finished!");
    }
}