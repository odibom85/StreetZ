using UnityEngine;

public class HoopSensor : MonoBehaviour
{
    public Transform HoopCenter;
    public float ThreePointLine = 7.25f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb.linearVelocity.y > 0) return; // Must be falling

        BallState state = other.GetComponent<BallState>();
        if (state != null)
        {
            float dist = Vector3.Distance(new Vector3(state.ShotOrigin.x, 0, state.ShotOrigin.z), 
                                          new Vector3(HoopCenter.position.x, 0, HoopCenter.position.z));
            
            int score = (dist > ThreePointLine) ? 3 : 2;
            Debug.Log($"Score: {score} points!");
        }
    }
}