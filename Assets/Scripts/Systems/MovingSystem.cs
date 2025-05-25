using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.IL2CPP.CompilerServices;
using System.Collections.Generic;
using System.Linq;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class MovingSystem : ISystem 
{
    public World World { get; set;}
    private Filter selectedUnits;
    private Camera mainCamera;
    private Stash<SelectionComponent> selectionStash;
    private Stash<MovableComponent> movableStash;
    
    // Настройки формации
    private float unitSpacing = 2.0f; // Расстояние между юнитами в формации
    private bool useFormation = true; // Включить/выключить использование формации
    
    
    private Vector3 lastMoveDirection = Vector3.forward; // Последнее направление движения

    public void OnAwake() 
    {
        this.selectedUnits = World.Filter.With<SelectionComponent>().With<MovableComponent>().Build();
        this.selectionStash = World.GetStash<SelectionComponent>();
        this.movableStash = World.GetStash<MovableComponent>();
        this.mainCamera = Camera.main;
    }

    public void OnUpdate(float deltaTime) 
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {   
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetPosition = hit.point;
                
                // Получаем список выбранных юнитов
                List<Entity> selectedEntities = new List<Entity>();
                foreach (var entity in selectedUnits)
                {
                    ref var selection = ref selectionStash.Get(entity);
                    if(selection.isSelected)
                    {
                        selectedEntities.Add(entity);
                    }
                }
                
                if (selectedEntities.Count == 0) return;
                
                // Если только один юнит или формации отключены, просто перемещаем в указанную точку
                if (selectedEntities.Count == 1 || !useFormation)
                {
                    foreach (var entity in selectedEntities)
                    {
                        ref var movable = ref movableStash.Get(entity);
                        movable.agent.SetDestination(targetPosition);
                    }
                    return;
                }
                
                // Обновляем направление движения (можно вычислить средний центр юнитов)
                Vector3 centerPosition = GetCenterPosition(selectedEntities);
                Vector3 targetDirection = (targetPosition - centerPosition).normalized;
                if (targetDirection != Vector3.zero)
                {
                    lastMoveDirection = targetDirection;
                }
                
                // Всегда используем круговую формацию
                MoveInCircleFormation(targetPosition, selectedEntities);
            }
        }   
    }

    /// <summary>
    /// Получает центральную позицию группы юнитов.
    /// </summary>
    private Vector3 GetCenterPosition(List<Entity> entities)
    {
        if (entities.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (var entity in entities)
        {
            ref var movable = ref movableStash.Get(entity);
            sum += movable.agent.transform.position;
        }
        
        return sum / entities.Count;
    }
    
    /// <summary>
    /// Перемещает юнитов в круговой формации.
    /// </summary>
    private void MoveInCircleFormation(Vector3 target, List<Entity> entities)
    {
        int unitCount = entities.Count;
        
        // Максимальный радиус внешнего круга
        float maxRadius = unitSpacing * Mathf.Sqrt(unitCount) / 2f;
        maxRadius = Mathf.Clamp(maxRadius, unitSpacing, unitSpacing * 5); // Ограничиваем радиус
        
        // Количество колец в круговой формации
        int ringCount = Mathf.CeilToInt(Mathf.Sqrt(unitCount) / 2f);
        ringCount = Mathf.Max(1, ringCount);
        
        int unitsPlaced = 0;
        
        // Начинаем с центра (если число юнитов нечетное)
        if (unitCount % 2 == 1)
        {
            ref var movable = ref movableStash.Get(entities[0]);
            movable.agent.SetDestination(target);
            unitsPlaced = 1;
        }
        
        // Создаем кольца с юнитами
        for (int ring = 1; ring <= ringCount && unitsPlaced < unitCount; ring++)
        {
            // Радиус текущего кольца
            float radius = (maxRadius * ring) / ringCount;
            
            // Определяем количество юнитов в кольце
            int unitsInRing = Mathf.Min(Mathf.FloorToInt(2 * Mathf.PI * radius / unitSpacing), unitCount - unitsPlaced);
            
            // Если кольцо слишком маленькое, уменьшаем количество юнитов
            unitsInRing = Mathf.Max(1, unitsInRing);
            
            float angleStep = 360f / unitsInRing;
            
            // Размещаем юнитов в текущем кольце
            for (int i = 0; i < unitsInRing && unitsPlaced < unitCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                
                // Создаем оси с учетом направления движения
                Vector3 forward = lastMoveDirection;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Преобразуем в мировые координаты с учетом направления движения
                Vector3 offset = right * x + forward * z;
                
                Vector3 unitTarget = target + offset;
                ref var movable = ref movableStash.Get(entities[unitsPlaced]);
                movable.agent.SetDestination(unitTarget);
                unitsPlaced++;
            }
        }
    }

    public void Dispose()
    {

    }
}