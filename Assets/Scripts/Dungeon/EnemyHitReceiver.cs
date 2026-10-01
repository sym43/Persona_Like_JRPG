using UnityEngine;

public sealed class EnemyHitReceiver : MonoBehaviour
{
    public void ReceiveHit()
    {
        Debug.Log($"[던전] {name} 피격", this);
    }
}
