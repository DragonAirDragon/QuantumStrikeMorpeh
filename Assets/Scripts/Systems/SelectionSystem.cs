using Scellecs.Morpeh;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public sealed class SelectionSystem : ISystem 
{
    public World World { get; set;}
    private Filter filter;
    private Filter selectionBoxFilter;
    private bool isShiftPressed;
    private Camera mainCamera;
    private Stash<SelectionComponent> selectionStash;
    private Stash<SelectionBoxComponent> selectionBoxStash;
    private Entity selectionBoxEntity;
    
    public void OnAwake() 
    {
        this.selectionStash = World.GetStash<SelectionComponent>();
        this.filter = World.Filter.With<SelectionComponent>().Build();
        this.mainCamera = Camera.main;

        this.selectionBoxStash = World.GetStash<SelectionBoxComponent>();
        this.selectionBoxFilter = World.Filter.With<SelectionBoxComponent>().Build();
        this.selectionBoxEntity = selectionBoxFilter.First();
    }

    public void OnUpdate(float deltaTime) 
    {
        // Проверяем, находится ли курсор над UI элементом
        if (IsPointerOverUI())
        {
            return; // Не обрабатываем выбор, если курсор над UI
        }
        
        isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        ref var selectionBox = ref selectionBoxStash.Get(selectionBoxEntity);
        
        if (Input.GetMouseButtonDown(0)) {
            selectionBox.isSelecting = true;
            selectionBox.startPos = Input.mousePosition;
            if (!isShiftPressed) {
                ClearSelection();
            }
        }

        if (selectionBox.isSelecting) {
            selectionBox.currentPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && selectionBox.isSelecting) {
            selectionBox.isSelecting = false;
            SelectEntitiesInBox(selectionBox.startPos, selectionBox.currentPos);
        }
    }
    
    /// <summary>
    /// Проверяет, находится ли курсор мыши над UI элементом
    /// </summary>
    /// <returns>True, если курсор над UI</returns>
    private bool IsPointerOverUI()
    {
        // Проверяем для мыши
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        
        // Дополнительная проверка для мобильных устройств (касания)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private void ClearSelection() {
        foreach (var entity in filter) {
            ref var selection = ref selectionStash.Get(entity);
            selection.isSelected = false;
        }
    }

    private void SelectEntitiesInBox(Vector3 startPos, Vector3 endPos) {
        Rect selectionRect = GetScreenRect(startPos, endPos);
        foreach (var entity in filter) {
            ref var selection = ref selectionStash.Get(entity);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(selection.transform.position);
            screenPos.y = Screen.height - screenPos.y;

            if (selectionRect.Contains(screenPos)) {
                selection.isSelected = true;
            }
        }
    }

    private Rect GetScreenRect(Vector3 startPos, Vector3 endPos) {
        startPos.y = Screen.height - startPos.y;
        endPos.y = Screen.height - endPos.y;
        Vector3 min = Vector3.Min(startPos, endPos);
        Vector3 max = Vector3.Max(startPos, endPos);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void Dispose()
    {

    }
}