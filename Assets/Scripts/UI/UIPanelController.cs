using UnityEngine;
using PrimeTween;

public class UIPanelController : MonoBehaviour
{
    [Header("Настройки панели")]
    [SerializeField] private RectTransform panelRectTransform; // Панель для анимации
    [SerializeField] private float animationDuration = 0.5f; // Длительность анимации
    [SerializeField] private Ease animationEase = Ease.OutQuart; // Тип анимации
    
    [Header("Позиции панели")]
    [SerializeField] private Vector2 hiddenLeftPosition = new Vector2(-400f, 0f); // Позиция слева (скрыта)
    [SerializeField] private Vector2 visiblePosition = new Vector2(0f, 0f); // Видимая позиция
    [SerializeField] private Vector2 hiddenRightPosition = new Vector2(400f, 0f); // Позиция справа (скрыта)
    
    [Header("Автоматическая настройка позиций")]
    [SerializeField] private bool autoCalculatePositions = true; // Автоматически рассчитывать позиции
    
    private Vector2 initialPosition;
    private Tween currentTween;
    private bool isVisible = true;
    
    void Start()
    {
        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }
        
        if (panelRectTransform == null)
        {
            Debug.LogError("UIPanelController: RectTransform не найден!");
            return;
        }
        
        // Сохраняем начальную позицию
        initialPosition = panelRectTransform.anchoredPosition;
        
        if (autoCalculatePositions)
        {
            CalculatePositions();
        }
        
        // Устанавливаем видимую позицию как текущую
        visiblePosition = initialPosition;
    }
    
    /// <summary>
    /// Автоматически рассчитывает позиции на основе размера панели
    /// </summary>
    private void CalculatePositions()
    {
        float panelWidth = panelRectTransform.rect.width;
        float screenWidth = Screen.width;
        
        // Позиция слева - панель полностью скрыта за левым краем экрана
        hiddenLeftPosition = new Vector2(-screenWidth * 0.5f - panelWidth * 0.5f, initialPosition.y);
        
        // Позиция справа - панель полностью скрыта за правым краем экрана  
        hiddenRightPosition = new Vector2(screenWidth * 0.5f + panelWidth * 0.5f, initialPosition.y);
        
        // Видимая позиция остается как есть
        visiblePosition = initialPosition;
    }
    
    /// <summary>
    /// Показывает панель (выдвигает вправо, если юниты есть)
    /// </summary>
    public void ShowPanel()
    {
        if (panelRectTransform == null) return;
        
        // Останавливаем текущую анимацию
        if (currentTween.isAlive)
        {
            currentTween.Stop();
        }
        
        // Анимируем к видимой позиции
        currentTween = Tween.UIAnchoredPosition(panelRectTransform, visiblePosition, animationDuration, animationEase);
        isVisible = true;
    }
    
    /// <summary>
    /// Скрывает панель (задвигает влево, если юнитов нет)
    /// </summary>
    public void HidePanel()
    {
        if (panelRectTransform == null) return;
        
        // Останавливаем текущую анимацию
        if (currentTween.isAlive)
        {
            currentTween.Stop();
        }
        
        // Анимируем к скрытой позиции слева
        currentTween = Tween.UIAnchoredPosition(panelRectTransform, hiddenLeftPosition, animationDuration, animationEase);
        isVisible = false;
    }
    
    /// <summary>
    /// Переключает видимость панели
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }
    
    /// <summary>
    /// Устанавливает позицию панели мгновенно без анимации
    /// </summary>
    /// <param name="position">Новая позиция</param>
    public void SetPositionImmediate(Vector2 position)
    {
        if (panelRectTransform == null) return;
        
        // Останавливаем текущую анимацию
        if (currentTween.isAlive)
        {
            currentTween.Stop();
        }
        
        panelRectTransform.anchoredPosition = position;
    }
    
    /// <summary>
    /// Показывает панель мгновенно
    /// </summary>
    public void ShowPanelImmediate()
    {
        SetPositionImmediate(visiblePosition);
        isVisible = true;
    }
    
    /// <summary>
    /// Скрывает панель мгновенно
    /// </summary>
    public void HidePanelImmediate()
    {
        SetPositionImmediate(hiddenLeftPosition);
        isVisible = false;
    }
    
    /// <summary>
    /// Проверяет, видна ли панель в данный момент
    /// </summary>
    public bool IsVisible => isVisible;
    
    /// <summary>
    /// Устанавливает длительность анимации
    /// </summary>
    public void SetAnimationDuration(float duration)
    {
        animationDuration = Mathf.Max(0.1f, duration);
    }
    
    /// <summary>
    /// Устанавливает тип анимации
    /// </summary>
    public void SetAnimationEase(Ease ease)
    {
        animationEase = ease;
    }
} 