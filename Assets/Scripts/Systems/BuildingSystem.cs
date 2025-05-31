using System.Threading.Tasks;
using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class BuildingSystem : ISystem 
{
    private const float GROUND_Y_POSITION = 0f;
    
    private StateBuilding stateBuilding = StateBuilding.None;
    private Request<BuildRequest> buildRequest;
    private Request<ToggleGrid> toggleGrid;
    private Filter gridElementFilter;
    private Stash<GridElementComponent> gridElementStash;
    
    private BuildingData currentBuilding;
    private GameObject buildingGhost;
    private Camera mainCamera;

    public World World { get; set;}

    public void OnAwake() 
    {
        buildRequest = World.GetRequest<BuildRequest>();
        toggleGrid = World.GetRequest<ToggleGrid>();
        gridElementFilter = World.Filter.With<GridElementComponent>().Build();
        gridElementStash = World.GetStash<GridElementComponent>();
        mainCamera = Camera.main;
    }

    public void OnUpdate(float deltaTime) 
    {
        foreach (var request in buildRequest.Consume()) {
            if (stateBuilding == StateBuilding.None) {
                StartBuilding(request.buildingName);
            }
        }
        if (stateBuilding == StateBuilding.Building) {
            Vector2Int mousePosition = GetMousePosition();
            if (buildingGhost == null) {
                buildingGhost = Object.Instantiate(currentBuilding.ghostPrefab);
            }

            // Проверяем, можно ли разместить здание в текущей позиции
            bool canPlace = PositionIsFree(mousePosition, currentBuilding.size);
            currentBuilding.ghostMaterial.color = canPlace ? currentBuilding.readyColor : currentBuilding.errorColor;
            
            // Позиционируем призрак здания
            buildingGhost.transform.position = new Vector3(mousePosition.x, 0, mousePosition.y);

            if(Mouse.current.leftButton.wasPressedThisFrame) {
                if(canPlace) {
                    EndBuilding(currentBuilding, mousePosition);
                }
            }
        }
    }
    
    private Vector2Int GetMousePosition() {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 worldPosition = hit.point;
            Vector2Int gridPosition = new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
            return gridPosition;
        }
        return Vector2Int.zero;
    }
    
    private bool PositionIsFree(Vector2Int gridPosition, Vector2Int buildingSize) {
        // Вычисляем границы нового здания
        Vector2Int newBuildingMin = gridPosition;
        Vector2Int newBuildingMax = gridPosition + buildingSize - Vector2Int.one;
        
        // Проверяем пересечение с существующими зданиями
        foreach (var gridElement in gridElementFilter) {
            ref var gridElementComponent = ref gridElementStash.Get(gridElement);
            
            Vector2Int existingMin = gridElementComponent.position;
            Vector2Int existingMax = gridElementComponent.position + gridElementComponent.size - Vector2Int.one;
            
            // Проверяем пересечение прямоугольников
            if (!(newBuildingMax.x < existingMin.x || newBuildingMin.x > existingMax.x ||
                  newBuildingMax.y < existingMin.y || newBuildingMin.y > existingMax.y)) {
                return false;
            }
        }
        return true;
    }

    private void StartBuilding(string buildingName) {
        var buildingsConfig = ConfigService.Buildings;
        if (buildingsConfig == null) {
            Debug.LogError("BuildingsConfig not found in ConfigService");
            return;
        }
        
        currentBuilding = buildingsConfig.buildings.Find(b => b.buildingName == buildingName);
        if (currentBuilding != null) {
            stateBuilding = StateBuilding.Building;
            ToggleGrid(true);
            return;
        }
        
        Debug.LogWarning($"Building with name '{buildingName}' not found!");
    }

    private void EndBuilding(BuildingData buildingData, Vector2Int gridPosition) {
        var building = Object.Instantiate(buildingData.buildingPrefab, 
            new Vector3(gridPosition.x, GROUND_Y_POSITION, gridPosition.y), 
            Quaternion.identity);
        
        // Получаем GridElementProvider и устанавливаем позицию и размер
        var gridElementProvider = building.GetComponent<GridElementProvider>();
        if (gridElementProvider != null) {
            ref var gridElementComponent = ref gridElementProvider.GetData();
            gridElementComponent.position = gridPosition;
            gridElementComponent.size = buildingData.size;
        } else {
            Debug.LogWarning($"GridElementProvider not found on building prefab: {buildingData.buildingName}");
        }
        
        // Уничтожаем призрак здания
        DestroyBuildingGhost();
        ToggleGrid(false);
        stateBuilding = StateBuilding.None;
    }
    
    private void DestroyBuildingGhost() {
        if (buildingGhost != null) {
            Object.Destroy(buildingGhost);
            buildingGhost = null;
        }
    }

    private void ToggleGrid(bool activity) {
        var request = new ToggleGrid
        {
            activity = activity
        };
        toggleGrid.Publish(request, allowNextFrame: true);
    }


    public void Dispose()
    {

    }
}

public enum StateBuilding{
    None,
    Building
}