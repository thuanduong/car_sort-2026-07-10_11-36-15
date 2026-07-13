using UnityEngine;
using Spine.Unity;
using Cysharp.Threading.Tasks;
using System.Threading;

public class CarController : MonoBehaviour
{
    public Animator car_anim;
    public Animator car_eye;    
    public ParticleSystem vfx;
    

    public SpriteRenderer spriteRenderer;
    public GameObject car_shadow;

    public bool UseSkeleton = false;
    [Header("Spine Components")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField, SpineAnimation] private string idleAnim = "idle";
    [SerializeField, SpineAnimation] private string idleRandomAnim = "idle_random";
    [SerializeField, SpineAnimation] private string winAnim = "win";
    [SerializeField, SpineAnimation] private string flipAnim = "flip";

    [Header("Settings")]
    [SerializeField] private float minRandomIdleWait = 3f;
    [SerializeField] private float maxRandomIdleWait = 8f;

    private MaterialPropertyBlock propBlock;
    private const int BaseTrack = 0;
    private CancellationTokenSource cts;

    private MeshRenderer meshRenderer;
    
    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (UseSkeleton && skeletonAnimation != null) {
            meshRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
            Debug.Log($"[CarController] Get meshRenderer {(meshRenderer != null)}");
        }
    }


    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        PlayIdle();
    }

    private void OnDisable()
    {
        cts.SafeCancelAndDispose();
    }


    public void TurnOnSheen()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.GetPropertyBlock(propBlock);
        
        propBlock.SetFloat("_Enable_Sheen", 1f); 
        
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    public void TurnOffSheen()
    {
        if (spriteRenderer == null) return;
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

    public void PlayIdle()
    {
        if (UseSkeleton)
        {
            var trackEntry = skeletonAnimation.AnimationState.SetAnimation(BaseTrack, idleAnim, true);
            trackEntry.TrackTime = Random.Range(0f, trackEntry.Animation.Duration);
        }
        else
            car_anim.SetTrigger("idle");
    }

    public void PlayIdleRandom()
    {
        if (UseSkeleton)
        {
            skeletonAnimation.AnimationState.SetAnimation(BaseTrack, idleRandomAnim, true);
        }
        // else
        //     car_anim.SetTrigger("idle_1");
    }

    public void PlayFlip(float flyDuration = 0)
    {
        if (UseSkeleton)
        {
            PlayFlipAndMove(flyDuration).Forget();
        }
        else
            car_anim.SetTrigger("flip");
    }
    
    public void PlayPush()
    {
        if (UseSkeleton)
        {
        }
        else 
            car_anim.SetTrigger("push");
    }

    public void PlayWin()
    {
        if (UseSkeleton)
        {
            skeletonAnimation.AnimationState.SetAnimation(BaseTrack, winAnim, false);
            skeletonAnimation.AnimationState.AddAnimation(BaseTrack, idleAnim, true, 0f);
        }
    }



    public void EyeOpen(bool enable)
    {
        if (UseSkeleton) return;
        car_eye.SetBool("is_open", enable);
    }

    public void EyeLook(bool enable)
    {
        if (UseSkeleton) return;
        car_eye.SetBool("is_look", enable);
    }

    public void EyeHappy(bool enable)
    {
        if (UseSkeleton) return;
        car_eye.SetBool("is_happy", true);
    }

    public void EyeOpenAndClose()
    {
        if (UseSkeleton) return;
        car_eye.SetTrigger("open_close");
    }

    private async UniTask PlayFlipAndMove(float flightDuration)
    {
        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(BaseTrack, flipAnim, false);
        
        float nativeDuration = trackEntry.Animation.Duration;
        trackEntry.TimeScale = nativeDuration / flightDuration;

        float elapsed = 0f;

        // 3. Sử dụng UniTask để Lerp vị trí xe
        while (elapsed < flightDuration)
        {
            if (cts.IsCancellationRequested) return;

            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;
                        
            await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
        }

        var idleEntry = skeletonAnimation.AnimationState.SetAnimation(BaseTrack, idleAnim, true);
        idleEntry.TimeScale = 1f; 
    }

    public void SetRenderOrder(int value)
    {
        if (UseSkeleton)
        {
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = value;   
            }
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = value;
            }
        }
    }
}   
