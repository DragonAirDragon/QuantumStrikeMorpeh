using Scellecs.Morpeh;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpawnRequestSender : MonoBehaviour
{
    private string unitName = "";
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    private Request<BuildUnitRequest> spawnRequest;
    
    private void Start()
    {
        // Получаем request из мира Morpeh
        spawnRequest = World.Default.GetRequest<BuildUnitRequest>();
        button.onClick.AddListener(SendSpawnRequest);
    }
    /// <summary>
    /// Отправляет запрос на строительство
    /// </summary>
    public void SendSpawnRequest()
    {
        if (spawnRequest == null)
        {
            Debug.LogError("SpawnRequest не инициализирован! Убедитесь, что Morpeh World создан.");
            return;
        }
        
        var request = new BuildUnitRequest
        {
            unitName = this.unitName
        };
        
        spawnRequest.Publish(request);
        Debug.Log($"Отправлен запрос на строительство: {unitName}");
    }
    /// <summary>
    /// Изменяет название здания для строительства
    /// </summary>
    /// <param name="newBuildingName">Новое название здания</param>
    public void SetUnitName(string newUnitName, Sprite newIcon)
    {
        unitName = newUnitName;
        image.sprite = newIcon;
        text.text = newUnitName;
    }
}
