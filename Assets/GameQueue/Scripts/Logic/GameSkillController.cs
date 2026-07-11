using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Linq;

public class GameSkillController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private SkillDatabase skillDatabase;
    [SerializeField] private EcsGameBootstrap ecsBootstrap;

    private VisualElement skillsWrapper;
    private List<SkillUsageData> _playerSkills; // Dữ liệu skill của người chơi trong màn này

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        skillsWrapper = root.Q<VisualElement>("SkillsWrapper");

    }

    public void InitializeSkills()
    {
        InitSkillsAsync().Forget();
    }

    private async UniTaskVoid InitSkillsAsync()
    {
        if (skillDatabase == null) {
            var rq = Resources.LoadAsync<SkillDatabase>("SkillDatabase");
            await rq;
            if (rq.asset != null)
                skillDatabase = rq.asset as SkillDatabase;
        }

        skillsWrapper.Clear();
        _playerSkills = skillDatabase.skills.Select(skill => new SkillUsageData {
            SkillId = skill.SkillId,
            SkillIcon = skill.SkillIcon,
            UsageCount = DataManager.Instance.GetSkillUsageLeft(skill.SkillId)
        }).ToList();

        foreach (var skill in _playerSkills)
        {
            CreateSkillNode(skill);
        }
        await UniTask.Yield();
        
        skillsWrapper.style.bottom = -200f; // Bắt đầu từ vị trí ẩn sâu hơn
        await DOTween.To(() => skillsWrapper.style.bottom.value.value, 
                   x => skillsWrapper.style.bottom = x, 
                   45f, 0.5f)
               .SetEase(Ease.OutBack);
    }

    private void CreateSkillNode(SkillUsageData data)
    {
        VisualElement node = new VisualElement();
        node.AddToClassList("skill-node");

        Button iconBtn = new Button();
        iconBtn.AddToClassList("skill-icon");
        iconBtn.AddToClassList("simple-button");
        iconBtn.style.backgroundImage = new StyleBackground(data.SkillIcon);
        VisualElement badge = new VisualElement();
        badge.AddToClassList("skill-badge");

        Label badgeText = new Label(data.UsageCount.ToString());
        badgeText.AddToClassList("badge-text");

        if (data.UsageCount == 0)
        {
            node.AddToClassList("skill-empty");
        }
        else
        {
            node.RemoveFromClassList("skill-empty");
        }
        badge.Add(badgeText);
        node.Add(iconBtn);
        node.Add(badge);
        skillsWrapper.Add(node);

        iconBtn.clicked += () => OnSkillClicked(node, badgeText, data).Forget();
    }

    private async UniTaskVoid OnSkillClicked(VisualElement node, Label badgeText, SkillUsageData data)
    {
        if (ecsBootstrap.IsUIBlockingInput || ecsBootstrap.IsPlaying == false)
        {
            return;
        }

        if (data.UsageCount <= 0) return;
        if (!DataManager.Instance.TryUseSkill(data.SkillId)) return;

        data.UsageCount--;
        badgeText.text = data.UsageCount.ToString();
        
        node.style.scale = new StyleScale(Vector2.one); // Reset scale
        _ = DOTween.To(() => 1f, 
                   x => node.style.scale = new StyleScale(new Vector2(x, x)), 
                   0.8f, 0.1f)
               .SetLoops(2, LoopType.Yoyo); 
        ExecuteSkill(data.SkillId);

        if (data.UsageCount == 0)
        {
            await UniTask.Delay(150);
            node.AddToClassList("skill-empty");
        }
    }

    private void ExecuteSkill(string skillId)
    {
        switch (skillId)
        {
            case "0":
                ecsBootstrap.TriggerRedo();
                break;
            case "1":
                ecsBootstrap.TriggerRedoSwap();
                break;
        }
    }

    public void OnWinGameResultSkill()
    {
        int rand = UnityEngine.Random.Range(0, skillDatabase.skills.Count);
        DataManager.Instance.AddSkillUsage(skillDatabase.skills[rand].SkillId, 1);
    }
}