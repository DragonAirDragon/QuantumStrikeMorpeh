using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class FactorySystem : ISystem 
{
    public Filter factoryFilter;
    public Filter selectedFactoryFilter;
    public Filter availableUnitsFilter;
    public Stash<FactoryComponent> factoryStash;
    public Stash<SelectionComponent> selectedStash;
    public Stash<AvailableUnitsComponent> availableUnitsStash;
    private Request<BuildUnitRequest> buildUnitRequest;
  
    
    
    

    public World World { get; set;}
    public void OnAwake() 
    {
        InitializeFilters();
        InitializeStashes();
        InitializeRequests();
    }
    private void InitializeFilters()
    {
        factoryFilter = World.Filter
            .With<FactoryComponent>().Build();
        selectedFactoryFilter = World.Filter
            .With<SelectionComponent>()
            .With<FactoryComponent>().Build();
        availableUnitsFilter = World.Filter
            .With<AvailableUnitsComponent>().Build();
    }
    private void InitializeStashes()
    {
        factoryStash = World.GetStash<FactoryComponent>();
        selectedStash = World.GetStash<SelectionComponent>();
        availableUnitsStash = World.GetStash<AvailableUnitsComponent>();
    }
    private void InitializeRequests()
    {
        buildUnitRequest = World.GetRequest<BuildUnitRequest>();
    }

    public void OnUpdate(float deltaTime) 
    {
        foreach (var entity in factoryFilter)
        {
            ref var factory = ref factoryStash.Get(entity);

            // Инициализируем очередь если она null
            if (factory.productionQueue == null)
            {
                factory.productionQueue = new Queue<ProductionItem>();
            }

            // Обрабатываем текущее производство
            if (factory.currentProduction.HasValue)
            {
                var currentItem = factory.currentProduction.Value;
                currentItem.remainingTime -= deltaTime;

                if (currentItem.remainingTime <= 0)
                {
                    // Юнит готов - создаем его
                    BuildUnit(currentItem.unitName, factory.spawnPoint);
                    factory.currentProduction = null;
                    
                    Debug.Log($"Unit {currentItem.unitName} production completed!");
                }
                else
                {
                    // Обновляем текущий элемент производства
                    factory.currentProduction = currentItem;
                }
            }

            // Если нет текущего производства, берем следующий из очереди
            if (!factory.currentProduction.HasValue && factory.productionQueue.Count > 0)
            {
                factory.currentProduction = factory.productionQueue.Dequeue();
                Debug.Log($"Started production of {factory.currentProduction.Value.unitName}");
            }
        }

        foreach (var entity in availableUnitsFilter) {
            ref var availableUnits = ref availableUnitsStash.Get(entity);
            availableUnits.availableUnits = GetAvailableUnitsFromSelectedFactories();
        }

        foreach (var request in buildUnitRequest.Consume()) {
            foreach (var factory in selectedFactoryFilter) {
                ref var factoryComponent = ref factoryStash.Get(factory);
                ref var selectionComponent = ref selectedStash.Get(factory);
                if (selectionComponent.isSelected == true) {
                    AddUnitToProductionQueue(factoryComponent, request.unitName);
                }
            }
        }
        
    }

    /// <summary>
    /// Добавляет юнит в очередь производства для указанной фабрики.
    /// </summary>
    /// <param name="factory">Компонент фабрики.</param>
    /// <param name="unitName">Имя юнита для производства.</param>
    public void AddUnitToProductionQueue(FactoryComponent factory, string unitName)
    {    
        var unitsConfig = ConfigService.Units;
        if (unitsConfig == null)
        {
            Debug.LogError("UnitsConfig not found in ConfigService");
            return;
        }

        // Проверяем, может ли этот завод производить данный юнит
        if (factory.availableUnitNames == null || factory.availableUnitNames.Length == 0)
        {
            Debug.LogError("Factory has no available units configured");
            return;
        }

        bool canProduceUnit = false;
        foreach (var availableUnit in factory.availableUnitNames)
        {
            if (availableUnit == unitName)
            {
                canProduceUnit = true;
                break;
            }
        }

        if (!canProduceUnit)
        {
            Debug.LogWarning($"Factory cannot produce unit: {unitName}");
            return;
        }

        var unitData = unitsConfig.units.Find(u => u.unitName == unitName);
        if (unitData == null)
        {
            Debug.LogError($"Unit {unitName} not found in config");
            return;
        }

        // Инициализируем очередь если она null
        if (factory.productionQueue == null)
        {
            factory.productionQueue = new Queue<ProductionItem>();
        }

        var productionItem = new ProductionItem(unitName, unitData.buildTime);
        factory.productionQueue.Enqueue(productionItem);
        
        Debug.Log($"Added {unitName} to production queue. Queue length: {factory.productionQueue.Count}");
    }



    /// <summary>
    /// Очищает очередь производства фабрики.
    /// </summary>
    /// <param name="factoryEntity">Сущность фабрики.</param>
    public void ClearProductionQueue(Entity factoryEntity)
    {
        if (!factoryStash.Has(factoryEntity))
            return;

        ref var factory = ref factoryStash.Get(factoryEntity);
        factory.productionQueue?.Clear();
        Debug.Log("Production queue cleared");
    }

    /// <summary>
    /// Создает юнит в указанной точке.
    /// </summary>
    /// <param name="unitName">Имя юнита для создания.</param>
    /// <param name="spawnPoint">Точка спавна юнита.</param>
    public void BuildUnit(string unitName, Transform spawnPoint)
    {
        var unitsConfig = ConfigService.Units;
        
        // Проверка входных параметров
        if (string.IsNullOrEmpty(unitName))
        {
            Debug.LogError("Unit name cannot be null or empty");
            return;
        }
        
        if (unitsConfig == null)
        {
            Debug.LogError("UnitsConfig not found in ConfigService");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point cannot be null");
            return;
        }

        // Поиск юнита в конфигурации
        var unit = unitsConfig.units.Find(u => u.unitName == unitName);
        if (unit == null)
        {
            Debug.LogError($"Unit with name {unitName} not found");
            return;
        }
        
        if (unit.unitPrefab == null)
        {
            Debug.LogError($"Unit prefab for {unitName} is null");
            return;
        }
        // Создание GameObject
        var createdUnit = Object.Instantiate(unit.unitPrefab, spawnPoint.position, spawnPoint.rotation);
        // Провайдеры в префабе автоматически создадут Entity и добавят компоненты при OnEnable
        Debug.Log($"Successfully created unit: {unitName} at position {spawnPoint.position}");
    }




    private string[] GetAvailableUnitsFromSelectedFactories()
    {
        var availableUnits = new HashSet<string>();
        var unitsConfig = ConfigService.Units;
        
        foreach (var entity in selectedFactoryFilter) {
            ref var selection = ref selectedStash.Get(entity);
            if (selection.isSelected) {
                ref var factory = ref factoryStash.Get(entity);
                
                // Используем список доступных юнитов конкретного завода
                if (factory.availableUnitNames != null && factory.availableUnitNames.Length > 0) {
                    foreach (var unitName in factory.availableUnitNames) {
                        if (!string.IsNullOrEmpty(unitName)) {
                            // Дополнительно проверяем, что юнит существует в общем конфиге
                            if (unitsConfig?.units != null) {
                                var unitExists = unitsConfig.units.Exists(u => u.unitName == unitName);
                                if (unitExists) {
                                    availableUnits.Add(unitName);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        var result = new string[availableUnits.Count];
        availableUnits.CopyTo(result);
        return result;
    }

    public void Dispose()
    {

    }
}