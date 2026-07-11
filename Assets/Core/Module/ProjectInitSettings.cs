using UnityEngine;

[CreateAssetMenu(fileName = "Project Init Settings", menuName = "Settings/Project Init Settings")]
public class ProjectInitSettings : ScriptableObject
{
    [SerializeField] InitModule[] initModules;
    public InitModule[] InitModules => initModules;

    public void Init(Initialiser initialiser)
    {
        for (int i = 0; i < initModules.Length; i++)
        {
            initModules[i].CreateComponent(initialiser);
        }
    }

    public void StartInit(Initialiser initialiser)
    {
        for (int i = 0; i < initModules.Length; i++)
        {
            initModules[i].StartInit(initialiser);
        }
    }
}

