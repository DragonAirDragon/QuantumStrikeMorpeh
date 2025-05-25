using Scellecs.Morpeh;
using UnityEngine;

public class SelectionBoxDrawer : MonoBehaviour {
    private World world;
    private Filter filter;
    private Stash<SelectionBoxComponent> selectionBoxStash;
    
    private void Start() {
        world = World.Default; // или твой способ получения мира
        filter = world.Filter.With<SelectionBoxComponent>().Build();
        this.selectionBoxStash = world.GetStash<SelectionBoxComponent>();
    }

    private void OnGUI() {
        Entity? foundEntity = null;
        foreach (var entity in filter) {
            foundEntity = entity;
            break;
        }
        if (foundEntity == null) return;
        ref var box = ref selectionBoxStash.Get(foundEntity.Value);
        if (box.isSelecting) {
            Rect rect = GetScreenRect(box.startPos, box.currentPos);
            // Сохраняем текущий цвет
            Color oldColor = GUI.color;
            // Полупрозрачная заливка
            GUI.color = new Color(0.3f, 0.6f, 1f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            // Рамка
            GUI.color = new Color(0.3f, 0.6f, 1f, 1f);
            DrawRectBorder(rect, 2);
            // Возвращаем цвет
            GUI.color = oldColor;
        }
    }

    private Rect GetScreenRect(Vector3 startPos, Vector3 endPos) {
        startPos.y = Screen.height - startPos.y;
        endPos.y = Screen.height - endPos.y;
        Vector3 min = Vector3.Min(startPos, endPos);
        Vector3 max = Vector3.Max(startPos, endPos);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Вспомогательный метод для отрисовки рамки
    private void DrawRectBorder(Rect rect, float thickness) {
        // Верх
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
        // Низ
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // Лево
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        // Право
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
    }
}
