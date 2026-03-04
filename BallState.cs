using UnityEngine;

public class BallState : MonoBehaviour
{
    public Vector3 ShotOrigin;
    public GameObject LastShooter;

    public void SetShotData(Vector3 origin, GameObject shooter)
    {
        ShotOrigin = origin;
        LastShooter = shooter;
    }
}