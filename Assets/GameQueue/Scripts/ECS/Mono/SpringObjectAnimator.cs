using UnityEngine;


public class SpringObjectAnimator : MonoBehaviour
{
    public Animator animator;

    /// <summary>
    /// Kích hoạt animation "đẩy ra" của lò xo.
    /// </summary>
    public void TriggerMoveOut()
    {
        if (animator != null)
        {
            animator.SetTrigger("move_out");
        }
    }

    /// <summary>
    /// Kích hoạt animation "thu vào" của lò xo.
    /// </summary>
    public void TriggerMoveIn()
    {
        if (animator != null)
        {
            animator.SetTrigger("move_in");
        }
    }
}