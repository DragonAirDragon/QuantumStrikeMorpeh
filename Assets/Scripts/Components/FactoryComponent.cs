using Scellecs.Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using System.Collections.Generic;

[System.Serializable]
public struct ProductionItem
{
    public string unitName;
    public float remainingTime;
    public float totalTime;

    public ProductionItem(string unitName, float buildTime)
    {
        this.unitName = unitName;
        this.remainingTime = buildTime;
        this.totalTime = buildTime;
    }
}

[System.Serializable]
[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public struct FactoryComponent : IComponent 
{
    public string[] availableUnitNames; // Юниты, которые может производить этот завод
    public Queue<ProductionItem> productionQueue; // Очередь производства юнитов
    public Transform spawnPoint; // Точка спавна юнитов
    
    // Опционально: текущий производимый юнит для отображения прогресса
    public ProductionItem? currentProduction;
}