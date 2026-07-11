using UnityEngine;

public class AutoDestroyVfx : MonoBehaviour
{
    [SerializeField] private float duration;

    void OnEnable()
    {
        Destroy(this.gameObject, duration);
    }
}
