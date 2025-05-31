using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class CombatSystem : ISystem {    
    
    #region Properties
    
    public World World { get; set; }
    
    #endregion
    
    #region Fields
    
    
    // Фильтры
    private Filter attackingUnits;
    private Filter targetableUnits;
    private Filter attackingUnitsWithMovable;
    // Кэши компонентов
    private Stash<AttackableComponent> attackableStash;
    private Stash<HealthComponent> healthStash;
    private Stash<FractionComponent> fractionStash;
    private Stash<MovableComponent> movableStash;
    private Stash<SelectionComponent> selectionStash;
    
    #endregion
    
    #region System Lifecycle
    
    public void OnAwake() 
    {
        InitializeFilters();
        InitializeStashes();
    }

    public void OnUpdate(float deltaTime) 
    {
        UpdateAttackCooldowns(deltaTime);
        CheckAndAddTargets();
        MoveToEnemyToAttackRange();
        ProcessCombat();
    }

    private void UpdateAttackCooldowns(float deltaTime)
    {
        foreach (var entity in attackingUnits)
        {
            ref var attackable = ref attackableStash.Get(entity);
            if (attackable.attackCooldown > 0)
            {
                attackable.attackCooldown -= deltaTime;
            }
        }
    }

    private void CheckAndAddTargets()
    {
        foreach (var attackerEntity in attackingUnits)
        {
            ref var attackable = ref attackableStash.Get(attackerEntity);
            
            // Если у юнита уже есть цель и она жива, пропускаем
            if (!World.IsDisposed(attackable.attackTarget) && 
                healthStash.Has(attackable.attackTarget) && 
                healthStash.Get(attackable.attackTarget).healthPoints > 0)
            {
                continue;
            }
            
            // Получаем позицию атакующего юнита
            var attackerHealth = healthStash.Get(attackerEntity);
            if (attackerHealth.transform == null) continue;
            Vector3 attackerPosition = attackerHealth.transform.position;
            
            // Получаем фракцию атакующего юнита
            var attackerFraction = fractionStash.Get(attackerEntity).fractionId;
            
            Entity closestTarget = default(Entity);
            float closestDistance = float.MaxValue;
            
            // Ищем ближайшую враждебную цель в пределах атаки
            foreach (var targetEntity in targetableUnits)
            {
                // Пропускаем союзников
                var targetFraction = fractionStash.Get(targetEntity).fractionId;
                if (targetFraction == attackerFraction) continue;
                
                // Пропускаем мертвые цели
                var targetHealth = healthStash.Get(targetEntity);
                if (targetHealth.healthPoints <= 0) continue;
                
                // Получаем позицию цели
                if (targetHealth.transform == null) continue;
                Vector3 targetPosition = targetHealth.transform.position;
                
                // Проверяем расстояние
                float distance = Vector3.Distance(attackerPosition, targetPosition);
                if (distance <= attackable.attackRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = targetEntity;
                }
            }
            
            // Устанавливаем ближайшую цель
            if (!World.IsDisposed(closestTarget))
            {
                attackable.attackTarget = closestTarget;
            }
        }
    }

    private void ProcessCombat()
    {
        foreach (var attackerEntity in attackingUnits)
        {
            ref var attackable = ref attackableStash.Get(attackerEntity);
            
            // Проверяем, есть ли цель и жива ли она
            if (World.IsDisposed(attackable.attackTarget) || 
                !healthStash.Has(attackable.attackTarget))
            {
                attackable.attackTarget = default(Entity);
                continue;
            }
            
            var targetHealth = healthStash.Get(attackable.attackTarget);
            if (targetHealth.healthPoints <= 0)
            {
                attackable.attackTarget = default(Entity);
                continue;
            }
            
            // Проверяем кулдаун атаки
            if (attackable.attackCooldown > 0)
            {
                continue;
            }
            
            // Получаем позиции атакующего и цели
            var attackerHealth = healthStash.Get(attackerEntity);
            if (attackerHealth.transform == null || targetHealth.transform == null)
            {
                continue;
            }
            
            Vector3 attackerPosition = attackerHealth.transform.position;
            Vector3 targetPosition = targetHealth.transform.position;
            
            // Проверяем дистанцию до цели
            float distance = Vector3.Distance(attackerPosition, targetPosition);
            if (distance > attackable.attackRange)
            {
                continue;
            }
            
            // Выполняем атаку
            PerformAttack(attackerEntity, attackable.attackTarget, ref attackable, distance);
            
            // Отрисовка для дебага
            DrawAttackDebug(attackerPosition, targetPosition, attackable.attackRange);
        }
    }
    
    private void PerformAttack(Entity attacker, Entity target, ref AttackableComponent attackable, float distance)
    {
        // Наносим урон цели
        ref var targetHealth = ref healthStash.Get(target);
        targetHealth.healthPoints -= attackable.attackDamage;
        
        // Устанавливаем кулдаун атаки
        attackable.attackCooldown = 1.0f / attackable.attackSpeed;
        
        // Лог для дебага
        Debug.Log($"[Combat] Entity {attacker} attacked {target} for {attackable.attackDamage} damage. " +
                 $"Target health: {targetHealth.healthPoints}. Distance: {distance:F2}");
    }
    
    private void DrawAttackDebug(Vector3 attackerPos, Vector3 targetPos, float attackRange)
    {
        // Рисуем линию атаки (красная для успешной атаки)
        Debug.DrawLine(attackerPos, targetPos, Color.red, 0.1f);
        
        // Рисуем радиус атаки (желтый круг)
        float angle = 0;
        Vector3 lastPoint = attackerPos + new Vector3(attackRange, 0, 0);
        for (int i = 0; i <= 32; i++)
        {
            angle += 2 * Mathf.PI / 32;
            Vector3 newPoint = attackerPos + new Vector3(Mathf.Cos(angle) * attackRange, 0, Mathf.Sin(angle) * attackRange);
            Debug.DrawLine(lastPoint, newPoint, Color.yellow, 0.1f);
            lastPoint = newPoint;
        }
    }

    private void MoveToEnemyToAttackRange()
    {
        foreach (var attackerEntity in attackingUnitsWithMovable)
        {
            ref var attackable = ref attackableStash.Get(attackerEntity);
            
            // Проверяем, что включено автодвижение
            if (!attackable.autoMoveToAttackRange)
                continue;
                
            // Проверяем, что у юнита нет цели
            if (!World.IsDisposed(attackable.attackTarget))
                continue;
            
            // Получаем компоненты атакующего юнита
            var attackerHealth = healthStash.Get(attackerEntity);
            var attackerMovable = movableStash.Get(attackerEntity);
            var attackerFraction = fractionStash.Get(attackerEntity);
            
            if (attackerHealth.transform == null || attackerMovable.agent == null)
                continue;
                
            // Проверяем, что агент не движется сейчас
            if (attackerMovable.agent.pathPending || attackerMovable.agent.hasPath)
                continue;
            
            Vector3 attackerPosition = attackerHealth.transform.position;
            
            Entity closestEnemy = default(Entity);
            float closestDistance = float.MaxValue;
            
            // Ищем ближайшего врага в пределах detectionRange
            foreach (var targetEntity in targetableUnits)
            {
                // Пропускаем союзников
                var targetFraction = fractionStash.Get(targetEntity).fractionId;
                if (targetFraction == attackerFraction.fractionId)
                    continue;
                
                // Пропускаем мертвые цели
                var targetHealth = healthStash.Get(targetEntity);
                if (targetHealth.healthPoints <= 0)
                    continue;
                
                if (targetHealth.transform == null)
                    continue;
                
                Vector3 targetPosition = targetHealth.transform.position;
                float distance = Vector3.Distance(attackerPosition, targetPosition);
                
                // Проверяем, что цель в пределах обнаружения
                if (distance <= attackable.detectionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = targetEntity;
                }
            }
            
            // Если нашли врага, движемся к нему
            if (!World.IsDisposed(closestEnemy))
            {
                var enemyHealth = healthStash.Get(closestEnemy);
                Vector3 enemyPosition = enemyHealth.transform.position;
                
                // Рассчитываем позицию для движения (на расстоянии attackRange от врага)
                Vector3 directionToEnemy = (enemyPosition - attackerPosition).normalized;
                Vector3 targetMovePosition = enemyPosition - directionToEnemy * (attackable.attackRange * 0.9f); // 0.9f для небольшого запаса
                
                // Устанавливаем цель движения
                attackerMovable.agent.SetDestination(targetMovePosition);
                
                // Лог для дебага
                Debug.Log($"[Movement] Entity {attackerEntity} moving to attack enemy {closestEnemy}. " +
                         $"Distance: {closestDistance:F2}, AttackRange: {attackable.attackRange:F2}");
                
                // Отрисовка для дебага
                DrawMovementDebug(attackerPosition, enemyPosition, targetMovePosition, attackable.detectionRange);
            }
        }
    }
    
    private void DrawMovementDebug(Vector3 attackerPos, Vector3 enemyPos, Vector3 targetPos, float detectionRange)
    {
        // Рисуем линию от атакующего к цели движения (синяя)
        Debug.DrawLine(attackerPos, targetPos, Color.blue, 0.1f);
        
        // Рисуем линию от цели движения к врагу (зеленая)
        Debug.DrawLine(targetPos, enemyPos, Color.green, 0.1f);
        
        // Рисуем радиус обнаружения (голубой круг)
        float angle = 0;
        Vector3 lastPoint = attackerPos + new Vector3(detectionRange, 0, 0);
        for (int i = 0; i <= 16; i++) // Меньше точек для detectionRange
        {
            angle += 2 * Mathf.PI / 16;
            Vector3 newPoint = attackerPos + new Vector3(Mathf.Cos(angle) * detectionRange, 0, Mathf.Sin(angle) * detectionRange);
            Debug.DrawLine(lastPoint, newPoint, Color.cyan, 0.1f);
            lastPoint = newPoint;
        }
    }

    public void Dispose() {  }
    
    #endregion
    
    #region Initialization
    
    private void InitializeFilters()
    {
        // Юниты, которые могут атаковать
        attackingUnits = World.Filter
            .With<AttackableComponent>()
            .With<HealthComponent>()
            .With<FractionComponent>()
            .Build();

        // Юниты, которые могут атаковать и двигаться
        attackingUnitsWithMovable = World.Filter
            .With<AttackableComponent>()
            .With<HealthComponent>()
            .With<FractionComponent>()
            .With<MovableComponent>()
            .Build();

        // Юниты, которых можно атаковать
        targetableUnits = World.Filter
            .With<HealthComponent>()
            .With<FractionComponent>()
            .Build();
    }
    
    private void InitializeStashes()
    {
        attackableStash = World.GetStash<AttackableComponent>();
        healthStash = World.GetStash<HealthComponent>();
        fractionStash = World.GetStash<FractionComponent>();
        movableStash = World.GetStash<MovableComponent>();
        selectionStash = World.GetStash<SelectionComponent>();
    }
    

    
    #endregion
}