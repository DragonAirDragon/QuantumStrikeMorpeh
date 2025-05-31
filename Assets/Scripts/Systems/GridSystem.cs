using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class GridSystem : ISystem 
{
    public World World { get; set;}
    public Filter gridFilter;
    public Stash<GridComponent> gridStash;
    private Request<ToggleGrid> toggleGrid;
    public void OnAwake() 
    {
        this.toggleGrid = this.World.GetRequest<ToggleGrid>();
        this.gridFilter = this.World.Filter.With<GridComponent>().Build();
        this.gridStash = this.World.GetStash<GridComponent>();
        foreach (var entity in gridFilter)
        {
            ref var gridComponent = ref gridStash.Get(entity);
            SetSize(gridComponent);
        }
    }

    public void SetSize(GridComponent gridComponent)
    {
        gridComponent.transform.localScale = new Vector3(gridComponent.size, 1 , gridComponent.size);
        gridComponent.transform.position = new Vector3(gridComponent.gridScale / 2, gridComponent.yOffset, gridComponent.gridScale / 2);


        
        // Устанавливаем масштаб сетки в материал шейдера
        if (gridComponent.material != null)
        {
            gridComponent.material.SetFloat("_Scale", gridComponent.gridScale);
            gridComponent.material.SetFloat("_GraduationScale", gridComponent.size);
        }
    }

    public void OnUpdate(float deltaTime) 
    {
        foreach (var entity in gridFilter)
        {
            ref var gridComponent = ref gridStash.Get(entity);
            foreach (var request in toggleGrid.Consume()) {
                gridComponent.isActive = request.activity;
            }
            gridComponent.gameObject.SetActive(gridComponent.isActive);
        }
    }
    public void Dispose()
    {

    }
}