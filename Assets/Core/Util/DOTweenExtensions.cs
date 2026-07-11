using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening.Core;
using System;
using System.Net.Mime;
using UnityEngine.UI;

public static class DOTweenExtensions
{
    public static Sequence AddTweenArray(this Sequence sequence, Tween[] tweens, float delay, Func<Tween, Sequence> tweenFunc)
    {
        tweens.ForEach(x => tweenFunc(x.SetDelay(delay)));
        return sequence;
    }
    
    public static Sequence AsSequence(this Tween[] tweens, bool isSequence = true, float delay = 0.0f)
    {
        var sequence = DOTween.Sequence();
        return isSequence
            ? sequence.AddTweenArray(tweens, delay, sequence.Append)
            : sequence.AddTweenArray(tweens, delay, sequence.Join);
    }

    public static Tween DOAnchorPosXFrom(this RectTransform rect, float from, float duration)
    {
        var anchoredPosition = rect.anchoredPosition;
        return rect.DOAnchorPosX(from + anchoredPosition.x, anchoredPosition.x, duration);
    }
    
    public static Tween DOAnchorPosYFrom(this RectTransform rect, float from, float duration)
    {
        var anchoredPosition = rect.anchoredPosition;
        return rect.DOAnchorPosY(from + anchoredPosition.y, anchoredPosition.y, duration);
    }
    
    public static Tween DOAnchorPosXToThenReverse(this RectTransform rect, float to, float duration)
    {
        var anchoredPosition = rect.anchoredPosition;
        return rect.DOAnchorPosX(anchoredPosition.x, anchoredPosition.x + to, duration, true);
    }

    public static Tween DOAnchorPosX(this RectTransform rect, float from, float to, float duration, bool reverseOnKill = false)
    {
        rect.anchoredPosition = new Vector2(from, rect.anchoredPosition.y);
        return DOTween.To(val => rect.anchoredPosition = new Vector2(val, rect.anchoredPosition.y), from, to, duration)
                      .OnKill(() => rect.anchoredPosition = reverseOnKill ? new Vector2(from, rect.anchoredPosition.y) : new Vector2(to, rect.anchoredPosition.y));
    }
    
    public static Tween DOAnchorPosY(this RectTransform rect, float from, float to, float duration, bool reverseOnKill = false)
    {
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, from);
        return DOTween.To(val => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, val), from, to, duration)
            .OnKill(() => rect.anchoredPosition = reverseOnKill ? new Vector2(rect.anchoredPosition.x, from) : new Vector2(rect.anchoredPosition.x, to));
    }
    
    public static Tween DOAnchorPosYToThenReverse(this RectTransform rect, float to, float duration)
    {
        var anchoredPosition = rect.anchoredPosition;
        return rect.DOAnchorPosY(anchoredPosition.y, anchoredPosition.y + to, duration, true);
    }

    public static Tween DOFade(this RectTransform rectTransform, float from, float to, float duration, bool reverseOnKill = false)
    {
        var canvasGroup = rectTransform.gameObject.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = from;
        return DOTween.To(val => canvasGroup.alpha = val, from, to, duration)
                      .OnKill(() => canvasGroup.alpha = reverseOnKill ? from : to);
    }
    
    public static Tween DOFade(this UnityEngine.UI.Image image, float from, float to, float duration, bool reverseOnKill = false)
    {
        image.SetAlpha(from);
        return DOTween.To(image.SetAlpha, from, to, duration)
            .OnKill(() => image.SetAlpha(reverseOnKill ? from : to));
    }
    
    public static Tween DOFade(this CanvasGroup canvasGroup, float from, float to, float duration, bool reverseOnKill = false)
    {
        canvasGroup.alpha = from;
        return DOTween.To(val => canvasGroup.alpha = val, from, to, duration)
                      .OnKill(() => canvasGroup.alpha = (reverseOnKill ? from : to));
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }
    
    public static Tween To(Action<float> setter, float from, float to, float duration, bool isWarmUp = true, bool reverseOnKill = false)
    {
        if(isWarmUp)
        {
            setter(from);
        }
        return DOTween.To(val => setter(val), from, to, duration)
                      .OnKill(() => setter(reverseOnKill ? from : to));
    }

    public static RectTransform RectTransform(this Component component)
    {
        return (RectTransform)component.transform;
    }

    public static Tween DOScaleFrom(this Transform transform, float from, float to, float duration)
    {
        return To(val => transform.localScale = Vector3.one * val, from, to, duration);
    }
    
    public static Tween DOScaleFrom(this Transform transform, Vector3 from, Vector3 to, float duration)
    {
        transform.localScale = from;
        return transform.DOScale(to, duration);
    }

    public static Tween DOMoveFrom(this Transform transform, Vector3 from, Vector3 to, float duration, bool reverseOnKill = false)
    {
        transform.position = from;
        return transform.DOMove(to, duration);
    }
}