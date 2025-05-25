using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class DeathCleanupSystem : ISystem 
{
    #region Properties
    
    public World World { get; set; }
    
    #endregion
    
    #region Fields
    
    // Фильтр для сущностей с здоровьем
    private Filter entitiesWithHealth;
    
    // Кэш компонентов
    private Stash<HealthComponent> healthStash;
    
    // Список для временного хранения мертвых сущностей
    private readonly List<Entity> deadEntities = new List<Entity>();
    
    #endregion
    
    #region System Lifecycle
    
    public void OnAwake() 
    {
        // Инициализируем фильтр для всех сущностей с HealthComponent
        entitiesWithHealth = World.Filter
            .With<HealthComponent>()
            .Build();
        
        // Инициализируем stash для работы с здоровьем
        healthStash = World.GetStash<HealthComponent>();
    }

    public void OnUpdate(float deltaTime) 
    {
        CleanupDeadEntities();
    }

    public void Dispose()
    {
        deadEntities?.Clear();
    }
    
    #endregion
    
    #region Cleanup Logic
    
    private void CleanupDeadEntities()
    {
        // Очищаем список от предыдущего кадра
        deadEntities.Clear();
        
        // Собираем все мертвые сущности
        foreach (var entity in entitiesWithHealth)
        {
            var health = healthStash.Get(entity);
            if (health.healthPoints <= 0)
            {
                deadEntities.Add(entity);
            }
        }
        
        // Удаляем мертвые сущности
        foreach (var deadEntity in deadEntities)
        {
            if (!World.IsDisposed(deadEntity))
            {
                // Лог для дебага
                Debug.Log($"[DeathCleanup] Destroying dead entity: {deadEntity}");
                
                // Удаляем GameObject если он существует
                var health = healthStash.Get(deadEntity);
                if (health.transform != null && health.transform.gameObject != null)
                {
                    Object.Destroy(health.transform.gameObject);
                }
                
                // Удаляем сущность из мира
                World.RemoveEntity(deadEntity);
            }
        }
        
        // Очищаем список после использования
        deadEntities.Clear();
    }
    
    
    #endregion
} 