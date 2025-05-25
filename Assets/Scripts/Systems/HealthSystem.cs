using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class HealthSystem : ISystem 
{
    public World World { get; set;}
    private Filter filter;
    private Stash<HealthComponent> healthStash;

    public void OnAwake() 
    {
        filter = World.Filter.With<HealthComponent>().Build();
        healthStash = World.GetStash<HealthComponent>();
    }

    public void OnUpdate(float deltaTime) 
    {
        foreach (var entity in filter)
        {
            ref var healthComponent = ref healthStash.Get(entity);
            //Debug.Log(healthComponent.healthPoints);
        }
    }

    public void Dispose()
    {

    }
}