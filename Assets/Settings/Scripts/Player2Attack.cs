using UnityEngine;

public class Player2Attack : MonoBehaviour
{
    public bool hasHit;

    private void OnEnable()
    {
        hasHit = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHit(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryHit(collision);
    }

    public void CheckCurrentOverlap()
    {
        BoxCollider2D hitbox = GetComponent<BoxCollider2D>();
        if (hitbox == null)
            return;

        Physics2D.SyncTransforms();
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(hitbox.bounds.center, hitbox.bounds.size, transform.eulerAngles.z);

        foreach (Collider2D overlap in overlaps)
            TryHit(overlap);
    }

    private void TryHit(Collider2D collision)
    {
        if (hasHit)
            return;

        if (collision.CompareTag("P1"))
        {
            hasHit = true;
            Player1Controls p1controls = collision.GetComponent<Player1Controls>();
            Player2Controls p2controls = GetComponentInParent<Player2Controls>();
            p1controls?.ReceiveHitFromPlayer2(p2controls);
        }
    }
}
