using UnityEngine;

public class RimFocusedCamera : MonoBehaviour
{
    public Transform Player;
    public Transform Rim;
    public float Distance = 6f;
    public float Height = 3.5f;

    void LateUpdate()
    {
        if (Player == null || Rim == null) return;

        Vector3 dir = (Player.position - Rim.position).normalized;
        dir.y = 0;

        Vector3 targetPos = Player.position + (dir * Distance);
        targetPos.y += Height;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        transform.LookAt(Rim.position + Vector3.up * 0.5f);
    }
}