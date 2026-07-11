using UnityEngine;

public class QueueAnchor : MonoBehaviour
{
    public int queueIndex;
    public float baseWidth = 1.0f;
    public float baseHeight = 1.5f;
    public BoxCollider2D boxCollider2D;

    private void Awake()
    {
        if (boxCollider2D == null)
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
        }
    }

    public void UpdateColliderSize(int maxItems, float itemSpacingY)
    {
        if (boxCollider2D == null) return;

        float totalHeight = baseHeight + ((maxItems - 1) * itemSpacingY);

        boxCollider2D.size = new Vector2(baseWidth, totalHeight);

        float centerOffsetY = ((maxItems - 1) * itemSpacingY) / 2f;
        boxCollider2D.offset = new Vector2(0, centerOffsetY);
    }
}
