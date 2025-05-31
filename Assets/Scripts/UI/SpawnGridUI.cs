using System.Collections.Generic;
using Scellecs.Morpeh;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class SpawnGridUI : MonoBehaviour
{
    [Header("Настройки UI")]
    [SerializeField] private Transform gridParent; // Родительский объект для кнопок (обычно с Grid Layout Group)
    [SerializeField] private GameObject buttonPrefab; // Префаб кнопки с компонентом SpawnRequestSender
    [SerializeField] private UnitsConfig unitsConfig; // Конфигурация 
    [SerializeField] private AvailableUnitsProvider availableUnitsProvider;
    private AvailableUnitsComponent availableUnitsComponent;

    [Header("События юнитов")]
    [SerializeField] private UnityEvent unitsExist; // Вызывается когда есть доступные юниты
    [SerializeField] private UnityEvent unitsNotExist; // Вызывается когда нет доступных юнитов

    [Header("Настройки кнопок")]
    [SerializeField] private string buttonNamePrefix = "Unit_"; // Префикс для имени кнопок
    private List<GameObject> createdButtons = new List<GameObject>(); // Список созданных кнопок для очистки
    
    [Header("Автообновление")]
    [SerializeField] private bool autoUpdateUI = true; // Автоматически обновлять UI при изменении доступных юнитов
    [SerializeField] private float updateCheckInterval = 0.1f; // Интервал проверки изменений в секундах
    
    // Переменные для отслеживания изменений
    private string[] lastAvailableUnits = new string[0];
    private float lastUpdateTime = 0f;
    private bool lastUnitsExistState = false; // Для отслеживания изменения состояния
    
    void Start()
    {
        if (availableUnitsProvider != null)
        {
            availableUnitsComponent = availableUnitsProvider.GetData();
        }
        unitsNotExist?.Invoke();
        // Создаем начальные кнопки
        UpdateUI();
    }

    void Update()
    {
        if (autoUpdateUI && availableUnitsProvider != null)
        {
            // Проверяем изменения с заданным интервалом
            if (Time.time - lastUpdateTime >= updateCheckInterval)
            {
                CheckForUpdatesAndRefreshUI();
                lastUpdateTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Проверяет изменения в доступных юнитах и обновляет UI при необходимости
    /// </summary>
    private void CheckForUpdatesAndRefreshUI()
    {
        availableUnitsComponent = availableUnitsProvider.GetData();
        string[] currentAvailableUnits = availableUnitsComponent.availableUnits ?? new string[0];
        
        // Проверяем, изменился ли список доступных юнитов
        if (!ArraysEqual(lastAvailableUnits, currentAvailableUnits))
        {
            lastAvailableUnits = currentAvailableUnits;
            UpdateUI();
        }
    }
    
    /// <summary>
    /// Сравнивает два массива строк на равенство
    /// </summary>
    private bool ArraysEqual(string[] array1, string[] array2)
    {
        if (array1.Length != array2.Length) return false;
        
        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i]) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Принудительно обновляет UI, создавая кнопки для доступных юнитов
    /// </summary>
    public void UpdateUI()
    {
        CreateBuildingButtons();
    }

    /// <summary>
    /// Создает кнопки только для доступных юнитов
    /// </summary>
    public void CreateBuildingButtons()
    {
        if (unitsConfig == null)
        {
            Debug.LogError("UnitsConfig не назначен в SpawnGridUI!");
            return;
        }
        
        if (gridParent == null)
        {
            Debug.LogError("Grid Parent не назначен в SpawnGridUI!");
            return;
        }
        
        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab не назначен в SpawnGridUI!");
            return;
        }
        
        // Очищаем существующие кнопки перед созданием новых
        ClearButtons();
        
        // Получаем актуальный список доступных юнитов
        string[] availableUnitNames = availableUnitsComponent.availableUnits ?? new string[0];
        
        // Проверяем состояние наличия юнитов и вызываем соответствующие события
        bool currentUnitsExist = availableUnitNames.Length > 0;
        CheckAndInvokeUnitsEvents(currentUnitsExist);
        
        if (availableUnitNames.Length == 0)
        {
            return;
        }
        
        // Создаем кнопку для каждого доступного юнита
        foreach (string unitName in availableUnitNames)
        {
            UnitData unitData = unitsConfig.units.Find(u => u.unitName == unitName);
            if (unitData != null)
            {
                CreateUnitButton(unitData);
            }
            else
            {
                Debug.LogWarning($"Юнит '{unitName}' найден в доступных, но отсутствует в конфигурации");
            }
        }
    }

    /// <summary>
    /// Проверяет состояние наличия юнитов и вызывает соответствующие события при изменении
    /// </summary>
    /// <param name="currentUnitsExist">Текущее состояние наличия юнитов</param>
    private void CheckAndInvokeUnitsEvents(bool currentUnitsExist)
    {
        // Вызываем события только при изменении состояния
        if (currentUnitsExist != lastUnitsExistState)
        {
            if (currentUnitsExist)
            {
                unitsExist?.Invoke();
            }
            else
            {
                unitsNotExist?.Invoke();
            }
            
            lastUnitsExistState = currentUnitsExist;
        }
    }

    /// <summary>
    /// Создает отдельную кнопку для юнита
    /// </summary>
    /// <param name="unitData">Данные юнита</param>
    private void CreateUnitButton(UnitData unitData)
    {
        // Создаем экземпляр кнопки
        GameObject buttonObject = Instantiate(buttonPrefab, gridParent);
        buttonObject.name = buttonNamePrefix + unitData.unitName;
        
        // Получаем компонент SpawnRequestSender
        SpawnRequestSender requestSender = buttonObject.GetComponent<SpawnRequestSender>();
        if (requestSender == null)
        {
            Debug.LogError($"Префаб кнопки {buttonPrefab.name} не содержит компонент SpawnRequestSender!");
            Destroy(buttonObject);
            return;
        }
        
        // Устанавливаем данные юнита
        requestSender.SetUnitName(unitData.unitName, unitData.icon);
        
        // Добавляем кнопку в список для отслеживания
        createdButtons.Add(buttonObject);
    }
    
    /// <summary>
    /// Очищает все созданные кнопки
    /// </summary>
    public void ClearButtons()
    {
        foreach (GameObject button in createdButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button);
            }
        }
        createdButtons.Clear();
    }
    
    /// <summary>
    /// Включает/выключает автоматическое обновление UI
    /// </summary>
    /// <param name="enable">Включить автообновление</param>
    public void SetAutoUpdateUI(bool enable)
    {
        autoUpdateUI = enable;
        Debug.Log($"Автообновление UI: {(enable ? "включено" : "выключено")}");
    }
    
    /// <summary>
    /// Устанавливает интервал проверки обновлений
    /// </summary>
    /// <param name="interval">Интервал в секундах</param>
    public void SetUpdateCheckInterval(float interval)
    {
        updateCheckInterval = Mathf.Max(0.01f, interval);
        Debug.Log($"Интервал проверки обновлений установлен: {updateCheckInterval} сек");
    }
}
