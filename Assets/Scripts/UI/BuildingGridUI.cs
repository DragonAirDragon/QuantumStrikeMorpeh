using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BuildingGridUI : MonoBehaviour
{
    [Header("Настройки UI")]
    [SerializeField] private Transform gridParent; // Родительский объект для кнопок (обычно с Grid Layout Group)
    [SerializeField] private GameObject buttonPrefab; // Префаб кнопки с компонентом BuildingRequestSender
    [SerializeField] private BuildingsConfig buildingsConfig; // Конфигурация зданий
    
    [Header("Настройки кнопок")]
    [SerializeField] private string buttonNamePrefix = "Building_"; // Префикс для имени кнопок
    
    private List<GameObject> createdButtons = new List<GameObject>(); // Список созданных кнопок для очистки
    
    private void Start()
    {
        CreateBuildingButtons();
    }
    
    /// <summary>
    /// Создает кнопки для всех зданий из конфигурации
    /// </summary>
    public void CreateBuildingButtons()
    {
        if (buildingsConfig == null)
        {
            Debug.LogError("BuildingsConfig не назначен в BuildingGridUI!");
            return;
        }
        
        if (gridParent == null)
        {
            Debug.LogError("Grid Parent не назначен в BuildingGridUI!");
            return;
        }
        
        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab не назначен в BuildingGridUI!");
            return;
        }
        
        // Очищаем существующие кнопки перед созданием новых
        ClearButtons();
        
        // Создаем кнопку для каждого здания в конфигурации
        for (int i = 0; i < buildingsConfig.buildings.Count; i++)
        {
            BuildingData buildingData = buildingsConfig.buildings[i];
            CreateBuildingButton(buildingData);
        }
    }
    
    /// <summary>
    /// Создает отдельную кнопку для здания
    /// </summary>
    /// <param name="buildingData">Данные здания</param>
    private void CreateBuildingButton(BuildingData buildingData)
    {
        // Создаем экземпляр кнопки
        GameObject buttonObject = Instantiate(buttonPrefab, gridParent);
        buttonObject.name = buttonNamePrefix + buildingData.buildingName;
        
        // Получаем компонент BuildingRequestSender
        BuildingRequestSender requestSender = buttonObject.GetComponent<BuildingRequestSender>();
        if (requestSender == null)
        {
            Debug.LogError($"Префаб кнопки {buttonPrefab.name} не содержит компонент BuildingRequestSender!");
            Destroy(buttonObject);
            return;
        }
        
        // Устанавливаем название здания
        requestSender.SetBuildingName(buildingData.buildingName, buildingData.icon);
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
} 