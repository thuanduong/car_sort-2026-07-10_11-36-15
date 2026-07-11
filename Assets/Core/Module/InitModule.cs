using UnityEngine;

public abstract class InitModule : ScriptableObject
{
    [HideInInspector]
    [SerializeField]
    protected string moduleName;

    public abstract void CreateComponent(Initialiser Initialiser);

    public virtual void StartInit(Initialiser Initialiser) { }

    public InitModule()
    {
        moduleName = "Default Module";
    }
}
