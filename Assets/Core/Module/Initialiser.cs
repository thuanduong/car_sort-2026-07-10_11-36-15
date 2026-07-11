using UnityEngine;
using System.Linq;

public class Initialiser : MonoBehaviour
{
    [SerializeField] ProjectInitSettings initSettings;
    [Space]
    [SerializeField] ScreenSettings screenSettings;

    public static GameObject InitialiserGameObject;

    public static bool IsInititalized { get; private set; }
    public static bool IsStartInitialized { get; private set; }

    private static Initialiser _instance;
    public static Initialiser Instance => _instance;

    public void Awake()
    {
        screenSettings.Initialise();

        if (!IsInititalized)
        {
            IsInititalized = true;

            InitialiserGameObject = gameObject;
            _instance = this;

            DontDestroyOnLoad(gameObject);

            initSettings.Init(this);

            Debug.Log("INITILIZER");
        }
    }

    public void Start()
    {
        Initialise();
    }

    public void Initialise()
    {
        if (!IsStartInitialized)
        {
            // Initialise components
            initSettings.StartInit(this);

            IsStartInitialized = true;
        }
    }

    private void OnDestroy()
    {
        IsInititalized = false;
        _instance = default;
    }

    private void OnApplicationFocus(bool focus)
    {

    }

    public InitModule getModule(string name)
    {
        if (initSettings == default)
        {
            return default;
        }
        return initSettings.InitModules.FirstOrDefault(x => x.name.Equals(name));
    }

}
