using Scellecs.Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;

[System.Serializable]
[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public struct AttackableComponent : IComponent 
{
    public float attackRange;    // Радиус атаки
    public float attackSpeed;    // Скорость атаки (атак в секунду)
    public int attackDamage;     // Урон за атаку
    public float detectionRange; // Радиус обнаружения врагов
    public bool autoMoveToAttackRange;
    public Entity attackTarget;  // Текущая цель атаки
    public float attackCooldown; // Текущий кулдаун атаки

    
} 