using UnityEngine;

public class CarController : MonoBehaviour
{
    public Animator car_anim;
    public Animator car_eye;    
    public ParticleSystem vfx;

    public SpriteRenderer spriteRenderer;
    public GameObject car_shadow;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    public void TurnOnSheen()
    {
        spriteRenderer.GetPropertyBlock(propBlock);
        
        propBlock.SetFloat("_Enable_Sheen", 1f); 
        
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void TurnOffSheen()
    {
        spriteRenderer.GetPropertyBlock(propBlock);
        
        propBlock.SetFloat("_Enable_Sheen", 0f); 
        
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void TurnOnShadow()
    {
        car_shadow.gameObject.SetActive(true);
    }

    public void TurnOffShadow()
    {
        car_shadow.gameObject.SetActive(false);
    }
}   
