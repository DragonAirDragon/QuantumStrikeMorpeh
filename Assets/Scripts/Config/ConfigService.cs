using UnityEngine;

/// <summary>
/// Глобальный сервис для доступа к конфигурациям игры
/// </summary>
public class ConfigService : MonoBehaviour
{
    [Header("Game Configurations")]
    [SerializeField] private UnitsConfig unitsConfig;
    [SerializeField] private BuildingsConfig buildingsConfig;
    
    private static ConfigService instance;
    
    public static ConfigService Instance 
    {
        get 
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ConfigService>();
                if (instance == null)
                {
                    Debug.LogError("ConfigService not found in scene! Please add ConfigService component to a GameObject.");
                }
            }
            return instance;
        }
    }
    
    public static UnitsConfig Units => Instance?.unitsConfig;
    public static BuildingsConfig Buildings => Instance?.buildingsConfig;
    
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Проверка конфигураций
        ValidateConfigs();
    }
    
    private void ValidateConfigs()
    {
        if (unitsConfig == null)
        {
            Debug.LogError("UnitsConfig is not assigned in ConfigService!");
        }
        
        if (buildingsConfig == null)
        {
            Debug.LogError("BuildingsConfig is not assigned in ConfigService!");
        }
    }
} 