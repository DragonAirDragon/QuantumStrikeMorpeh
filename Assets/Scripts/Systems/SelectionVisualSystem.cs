using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class SelectionVisualSystem : ISystem 
{
    private Filter filter;
    private Stash<SelectionComponent> selectionStash;
    public World World { get; set;}

    public void OnAwake() 
    {
        this.selectionStash = World.GetStash<SelectionComponent>();
        this.filter = World.Filter.With<SelectionComponent>().Build();
    }

    public void OnUpdate(float deltaTime) 
    {
        foreach (var entity in filter) {
            ref var selection = ref selectionStash.Get(entity);
            selection.selectionObject.SetActive(selection.isSelected);
        }
    }

    public void Dispose()
    {

    }
}