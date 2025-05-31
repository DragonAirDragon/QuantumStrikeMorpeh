using UnityEngine;
using PrimeTween;

/// <summary>
/// Эффект красивого появления здания с анимацией масштабирования
/// </summary>
public class BuildingSpawnEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.0f;
    [SerializeField] private Ease easeType = Ease.OutBack;
    [SerializeField] private float startScale = 0.1f;
    [SerializeField] private bool playOnStart = true;
    
    [Header("Advanced Settings")]
    [SerializeField] private float bounceIntensity = 1.7f;
    [SerializeField] private bool rotateOnSpawn = false;
    [SerializeField] private float rotationAmount = 360f;
    [SerializeField] private bool fadeInEffect = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioSource audioSource;
    
    private Vector3 originalScale;
    private Sequence spawnSequence;
    private Renderer[] renderers;
    
    private void Awake()
    {
        // Сохраняем оригинальный масштаб
        originalScale = transform.localScale;
        
        // Получаем все рендереры для эффекта исчезновения
        if (fadeInEffect)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        
        // Получаем AudioSource если не назначен
        if (spawnSound != null && audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        if (playOnStart)
        {
            PlaySpawnEffect();
        }
    }
    
    /// <summary>
    /// Запускает эффект появления здания
    /// </summary>
    public void PlaySpawnEffect()
    {
        // Останавливаем предыдущую анимацию если она есть
        if (spawnSequence.isAlive)
        {
            spawnSequence.Complete();
        }
        
        // Подготавливаем объект к анимации
        PrepareForAnimation();
        
        // Создаем последовательность анимаций
        CreateSpawnSequence();
        
        // Воспроизводим звук
        PlaySpawnSound();
    }
    
    private void PrepareForAnimation()
    {
        // Устанавливаем начальный масштаб
        transform.localScale = originalScale * startScale;
        
        // Если нужен эффект исчезновения, делаем объект прозрачным
        if (fadeInEffect && renderers != null)
        {
            SetAlpha(0f);
        }
    }
    
    private void CreateSpawnSequence()
    {
        spawnSequence = Sequence.Create();
        
        // Основная анимация масштабирования
        var scaleAnimation = Tween.Scale(transform, originalScale, animationDuration, easeType);
        
        if (easeType == Ease.OutBack)
        {
            // Для OutBack добавляем красивый отскок
            scaleAnimation = Tween.Scale(transform, originalScale, animationDuration, Ease.OutBack);
        }
        
        spawnSequence.Chain(scaleAnimation);
        
        // Добавляем поворот если нужно
        if (rotateOnSpawn)
        {
            var rotationAnimation = Tween.Rotation(transform, 
                transform.rotation * Quaternion.Euler(0, rotationAmount, 0), 
                animationDuration, 
                Ease.OutQuart);
            spawnSequence.Group(rotationAnimation);
        }
        
        // Добавляем эффект исчезновения
        if (fadeInEffect && renderers != null)
        {
            var fadeAnimation = Tween.Custom(0f, 1f, animationDuration * 0.8f, 
                onValueChange: SetAlpha, 
                ease: Ease.OutQuart);
            spawnSequence.Group(fadeAnimation);
        }
        
        // Добавляем небольшой дополнительный отскок в конце
        spawnSequence.ChainDelay(0.1f);
        spawnSequence.Chain(Tween.Scale(transform, originalScale * 1.05f, 0.15f, Ease.OutQuart));
        spawnSequence.Chain(Tween.Scale(transform, originalScale, 0.15f, Ease.InQuart));
        
        // Событие завершения анимации
        spawnSequence.OnComplete(() => OnSpawnComplete());
    }
    
    private void SetAlpha(float alpha)
    {
        if (renderers == null) return;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
                else if (material.HasProperty("_BaseColor"))
                {
                    Color color = material.GetColor("_BaseColor");
                    color.a = alpha;
                }
            }
        }
    }
    
    private void PlaySpawnSound()
    {
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
    }
    
    private void OnSpawnComplete()
    {
        // Можно добавить дополнительные эффекты или события
        // Например, частицы, дополнительные звуки и т.д.
    }
    
    /// <summary>
    /// Мгновенно показывает объект без анимации
    /// </summary>
    public void ShowInstantly()
    {
        if (spawnSequence.isAlive)
        {
            spawnSequence.Complete();
        }
        
        transform.localScale = originalScale;
        
        if (fadeInEffect && renderers != null)
        {
            SetAlpha(1f);
        }
    }
    
    /// <summary>
    /// Скрывает объект для повторного воспроизведения эффекта
    /// </summary>
    public void Hide()
    {
        transform.localScale = originalScale * startScale;
        
        if (fadeInEffect && renderers != null)
        {
            SetAlpha(0f);
        }
    }
    
    private void OnDestroy()
    {
        // Очищаем анимацию при уничтожении объекта
        if (spawnSequence.isAlive)
        {
            spawnSequence.Complete();
        }
    }
    
    private void OnValidate()
    {
        // Ограничиваем значения в редакторе
        animationDuration = Mathf.Max(0.1f, animationDuration);
        startScale = Mathf.Clamp(startScale, 0.01f, 1f);
    }
} 