using Scellecs.Morpeh;
using UnityEngine;

public class Startup : MonoBehaviour
{
    private World world;
    
    private void Start() {
        this.world = World.Default;
        var systemsGroup = this.world.CreateSystemsGroup();
        systemsGroup.AddSystem(new HealthSystem());
        systemsGroup.AddSystem(new MovingSystem());
        systemsGroup.AddSystem(new SelectionSystem());
        systemsGroup.AddSystem(new SelectionVisualSystem());
        systemsGroup.AddSystem(new CombatSystem());
        systemsGroup.AddSystem(new DeathCleanupSystem());
        systemsGroup.AddSystem(new BuildingSystem());
        systemsGroup.AddSystem(new GridSystem());
        systemsGroup.AddSystem(new FactorySystem());
        
        this.world.AddSystemsGroup(order: 0, systemsGroup);
    }
}
