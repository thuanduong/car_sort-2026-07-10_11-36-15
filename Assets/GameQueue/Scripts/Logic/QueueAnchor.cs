using UnityEngine;

public class QueueAnchor : MonoBehaviour
{
    public int queueIndex;
    public float baseWidth = 1.0f;
    public float baseHeight = 1.5f;
    public BoxCollider2D boxCollider2D;

    private void Awake()
    {
        // Tự động gán component nếu chưa kéo thả trong Inspector
        if (boxCollider2D == null)
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
        }
    }

    /// <summary>
    /// Cập nhật kích thước và vị trí của BoxCollider2D
    /// </summary>
    /// <param name="maxItems">Số lượng phần tử tối đa (M)</param>
    /// <param name="itemSpacingY">Khoảng cách dọc giữa các phần tử</param>
    public void UpdateColliderSize(int maxItems, float itemSpacingY)
    {
        if (boxCollider2D == null) return;

        // Tính tổng chiều cao: Chiều cao gốc + khoảng không gian cho các phần tử xếp chồng
        float totalHeight = baseHeight + ((maxItems - 1) * itemSpacingY);

        // Cập nhật Size của Collider
        boxCollider2D.size = new Vector2(baseWidth, totalHeight);

        // Tính toán Offset (tâm) để Collider bao trọn lên phía trên
        // Vì phần tử đầu ở Y=0, phần tử cuối ở Y=(maxItems - 1)*itemSpacingY, tâm của toàn bộ trục Y sẽ là một nửa khoảng này
        float centerOffsetY = ((maxItems - 1) * itemSpacingY) / 2f;
        boxCollider2D.offset = new Vector2(0, centerOffsetY);
    }
}
