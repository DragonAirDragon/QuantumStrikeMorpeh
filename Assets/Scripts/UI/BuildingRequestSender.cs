using UnityEngine;
using Scellecs.Morpeh;
using UnityEngine.UI;
using TMPro;

public class BuildingRequestSender : MonoBehaviour
{
    private string buildingName = "";
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    private Request<BuildRequest> buildRequest;
    
    private void Start()
    {
        // Получаем request из мира Morpeh
        buildRequest = World.Default.GetRequest<BuildRequest>();
        button.onClick.AddListener(SendBuildRequest);
    }
    /// <summary>
    /// Отправляет запрос на строительство
    /// </summary>
    public void SendBuildRequest()
    {
        if (buildRequest == null)
        {
            Debug.LogError("BuildRequest не инициализирован! Убедитесь, что Morpeh World создан.");
            return;
        }
        
        var request = new BuildRequest
        {
            buildingName = this.buildingName
        };
        
        buildRequest.Publish(request);
    }
    /// <summary>
    /// Изменяет название здания для строительства
    /// </summary>
    /// <param name="newBuildingName">Новое название здания</param>
    public void SetBuildingName(string newBuildingName, Sprite newIcon)
    {
        buildingName = newBuildingName;
        image.sprite = newIcon;
        text.text = newBuildingName;
    }
} 