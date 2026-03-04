using UnityEngine;
using UnityEngine.InputSystem;

public class BallPickup : MonoBehaviour
{
    public PlayerBallHandler BallHandler;
    public PlayerShooting ShootingScript; // Add this reference in Inspector
    public float PickupRadius = 2.5f;
    public LayerMask BallLayer; 

    public void OnPickup(InputValue v)
    {
        if (v.isPressed && !BallHandler.HasBall)
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, PickupRadius, BallLayer);
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Ball"))
            {
                BallHandler.GainPossession();
                
                // RESET THE SHOOTING UI
                if (ShootingScript != null)
                {
                    ShootingScript.ResetShootingState();
                }
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, PickupRadius);
    }
}