using UnityEngine;
using System.Collections;
using TriInspector;

public class CameraService : MonoBehaviour
{
    [Title("Перемещение камеры")]
    [SerializeField] private float _moveSpeed = 30f;
    [SerializeField] private float _accelerationMultiplier = 2f;
    [SerializeField] private float _dragSpeed = 3f;
    [SerializeField] private bool _edgeScrollingEnabled = true;
    [SerializeField] private float _edgeScrollingSize = 20f;
    
    [Title("Зум камеры")]
    [SerializeField] private float _zoomSpeed = 15f;
    [Min(5f)]
    [SerializeField] private float _minZoom = 10f;
    [Min(20f)]
    [SerializeField] private float _maxZoom = 50f;
    [SerializeField] private bool _smoothZoom = true;
    [SerializeField] private float _smoothZoomSpeed = 8f;
    [SerializeField] private float _zoomFactor = 2f;
    
    [Title("Ограничения камеры")]
    [SerializeField] private bool _useBoundaries = true;
    [SerializeField] private float _minX = -50f;
    [SerializeField] private float _maxX = 50f;
    [SerializeField] private float _minZ = -50f;
    [SerializeField] private float _maxZ = 50f;
    
    [Title("Наклон камеры")]
    [SerializeField] private bool _enableTilt = true;
    [SerializeField] private float _minTiltAngle = 30f;
    [SerializeField] private float _maxTiltAngle = 60f;
    [SerializeField, Range(0f, 1f)] private float _tiltDynamicFactor = 0.4f;
    
    // Константы для улучшения читаемости
    private const float TILT_LERP_SPEED = 5f;
    private const float SCROLL_THRESHOLD = 0.01f;
    private const float SMOOTH_MOVE_DURATION = 0.5f;
    
    // Кешированные компоненты и переменные
    private Camera _camera;
    private Transform _cameraTransform;
    private float _currentZoom;
    private float _targetZoom;
    private bool _isDragging = false;
    private Vector3 _dragStartPosition;
    private Vector3 _dragCurrentPosition;
    private Vector3 _targetTiltAngles;
    
    // Оптимизация вычислений
    private Vector3 _cachedMoveDirection;
    private Quaternion _cachedHorizontalRotation;
    private bool _needsRotationUpdate = true;
    private float _lastYRotation;

    private void Awake()
    {
        InitializeCamera();
        ValidateSettings();
    }
    
    private void InitializeCamera()
    {
        // Более безопасное получение камеры
        _camera = Camera.main;
        if (_camera == null)
        {
            _camera = FindFirstObjectByType<Camera>();
            if (_camera == null)
            {
                Debug.LogError("CameraService: Не найдена камера в сцене!");
                enabled = false;
                return;
            }
        }
        
        _cameraTransform = _camera.transform;
        
        // Безопасная инициализация зума
        if (_cameraTransform != null)
        {
            _currentZoom = Vector3.Distance(_cameraTransform.position, 
                new Vector3(_cameraTransform.position.x, 0, _cameraTransform.position.z));
            _targetZoom = _currentZoom;
            _targetTiltAngles = _cameraTransform.eulerAngles;
            _lastYRotation = _cameraTransform.eulerAngles.y;
        }
    }
    
    private void ValidateSettings()
    {
        // Валидация настроек для предотвращения ошибок
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _accelerationMultiplier = Mathf.Max(1f, _accelerationMultiplier);
        _dragSpeed = Mathf.Max(0f, _dragSpeed);
        _edgeScrollingSize = Mathf.Max(1f, _edgeScrollingSize);
        
        _minZoom = Mathf.Max(1f, _minZoom);
        _maxZoom = Mathf.Max(_minZoom + 1f, _maxZoom);
        _smoothZoomSpeed = Mathf.Max(0.1f, _smoothZoomSpeed);
        _zoomFactor = Mathf.Max(0.1f, _zoomFactor);
        
        _minTiltAngle = Mathf.Clamp(_minTiltAngle, 0f, 89f);
        _maxTiltAngle = Mathf.Clamp(_maxTiltAngle, _minTiltAngle, 89f);
        
        // Валидация границ
        if (_maxX <= _minX) _maxX = _minX + 1f;
        if (_maxZ <= _minZ) _maxZ = _minZ + 1f;
    }
    
    private void Update()
    {
        if (_cameraTransform == null) return;
        
        UpdateCachedRotation();
        HandleKeyboardInput();
        HandleEdgeScrolling();
        HandleMouseDrag();
        HandleMouseZoom();
        HandleTilt();
        ClampPosition();
    }
    
    private void UpdateCachedRotation()
    {
        float currentYRotation = _cameraTransform.eulerAngles.y;
        if (Mathf.Abs(currentYRotation - _lastYRotation) > 0.01f)
        {
            _cachedHorizontalRotation = Quaternion.Euler(0, currentYRotation, 0);
            _lastYRotation = currentYRotation;
            _needsRotationUpdate = false;
        }
    }
    
    private void HandleKeyboardInput()
    {
        _cachedMoveDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            _cachedMoveDirection += Vector3.forward;
        
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            _cachedMoveDirection += Vector3.back;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            _cachedMoveDirection += Vector3.left;
        
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            _cachedMoveDirection += Vector3.right;
        
        if (_cachedMoveDirection.sqrMagnitude > 0)
        {
            float accelerationFactor = Input.GetKey(KeyCode.LeftShift) ? _accelerationMultiplier : 1f;
            MoveCamera(_cachedMoveDirection, _moveSpeed * accelerationFactor);
        }
    }
    
    private void HandleEdgeScrolling()
    {
        if (!_edgeScrollingEnabled) 
            return;
        
        _cachedMoveDirection = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;
        
        // Оптимизированная проверка границ экрана
        if (mousePos.x < _edgeScrollingSize)
            _cachedMoveDirection.x = -1f;
        else if (mousePos.x > Screen.width - _edgeScrollingSize)
            _cachedMoveDirection.x = 1f;
        
        if (mousePos.y < _edgeScrollingSize)
            _cachedMoveDirection.z = -1f;
        else if (mousePos.y > Screen.height - _edgeScrollingSize)
            _cachedMoveDirection.z = 1f;
        
        if (_cachedMoveDirection.sqrMagnitude > 0)
        {
            MoveCamera(_cachedMoveDirection, _moveSpeed);
        }
    }
    
    private void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isDragging = true;
            _dragStartPosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }
        
        if (_isDragging)
        {
            _dragCurrentPosition = Input.mousePosition;
            Vector3 mouseDelta = _dragCurrentPosition - _dragStartPosition;
            
            if (mouseDelta.sqrMagnitude > 0.1f) // Избегаем микро-движений
            {
                // Инвертируем направление для интуитивного управления
                Vector3 dragDirection = new Vector3(-mouseDelta.x, 0, -mouseDelta.y);
                
                // Нормализуем чувствительность по размеру экрана
                float sensitivity = _dragSpeed * 0.001f; // Уменьшил чувствительность
                dragDirection *= sensitivity;
                
                // Преобразуем в мировые координаты
                Vector3 worldMovement = _cachedHorizontalRotation * dragDirection;
                transform.position += worldMovement;
                
                // Обновляем стартовую позицию для следующего кадра
                _dragStartPosition = _dragCurrentPosition;
            }
        }
    }
    
    private void HandleMouseZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > SCROLL_THRESHOLD)
        {
            _targetZoom -= scrollInput * _zoomFactor;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
        }
        
        // Плавное зумирование
        if (_smoothZoom)
        {
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * _smoothZoomSpeed);
        }
        else
        {
            _currentZoom = _targetZoom;
        }
        
        // Более стабильное позиционирование камеры при зуме
        Vector3 basePosition = transform.position;
        Vector3 targetPosition = new Vector3(basePosition.x, basePosition.y + _currentZoom, basePosition.z);
        _cameraTransform.position = targetPosition;
        
        UpdateCameraTilt();
    }
    
    private void HandleTilt()
    {
        if (!_enableTilt) return;
        
        // Плавное применение наклона с проверкой на изменения
        if (Vector3.Distance(_cameraTransform.eulerAngles, _targetTiltAngles) > 0.1f)
        {
            _cameraTransform.eulerAngles = Vector3.Lerp(_cameraTransform.eulerAngles, _targetTiltAngles, Time.deltaTime * TILT_LERP_SPEED);
        }
    }
    
    private void UpdateCameraTilt()
    {
        if (!_enableTilt) return;
        
        // Оптимизированный расчет угла наклона
        float zoomRange = _maxZoom - _minZoom;
        if (zoomRange > 0)
        {
            float zoomRatio = (_currentZoom - _minZoom) / zoomRange;
            float dynamicTiltAngle = Mathf.Lerp(_minTiltAngle, _maxTiltAngle, zoomRatio * _tiltDynamicFactor);
            
            _targetTiltAngles = new Vector3(dynamicTiltAngle, _cameraTransform.eulerAngles.y, 0f);
        }
    }
    
    private void ClampPosition()
    {
        if (!_useBoundaries) return;
        
        Vector3 pos = transform.position;
        
        float clampedX = Mathf.Clamp(pos.x, _minX, _maxX);
        float clampedZ = Mathf.Clamp(pos.z, _minZ, _maxZ);
        
        if (Mathf.Abs(pos.x - clampedX) > 0.001f || Mathf.Abs(pos.z - clampedZ) > 0.001f)
        {
            pos.x = clampedX;
            pos.z = clampedZ;
            transform.position = pos;
        }
    }
    
    /// <summary>
    /// Унифицированный метод для перемещения камеры
    /// </summary>
    private void MoveCamera(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude <= 0) return;
        
        // Нормализация для равномерного движения по диагонали
        direction.Normalize();
        
        // Применение скорости и времени
        direction *= speed * Time.deltaTime;
        
        // Преобразование направления с учетом поворота камеры
        Vector3 worldMovement = _cachedHorizontalRotation * direction;
        
        // Перемещение камеры
        transform.position += worldMovement;
    }
    
    /// <summary>
    /// Плавное перемещение камеры к целевой позиции
    /// </summary>
    public void MoveToPosition(Vector3 targetPosition)
    {
        StartCoroutine(SmoothMoveCamera(targetPosition));
    }
    
    /// <summary>
    /// Установка зума камеры
    /// </summary>
    public void SetZoom(float zoom)
    {
        _targetZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        if (!_smoothZoom)
        {
            _currentZoom = _targetZoom;
        }
    }
    
    /// <summary>
    /// Получение текущего зума
    /// </summary>
    public float GetCurrentZoom()
    {
        return _currentZoom;
    }
    
    private IEnumerator SmoothMoveCamera(Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        
        // Ограничиваем целевую позицию границами
        if (_useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, _minX, _maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, _minZ, _maxZ);
        }
        
        while (elapsed < SMOOTH_MOVE_DURATION)
        {
            float t = elapsed / SMOOTH_MOVE_DURATION;
            // Используем SmoothStep для более естественного движения
            t = t * t * (3f - 2f * t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    private void OnDrawGizmos()
    {
        if (_useBoundaries)
        {
            Gizmos.color = Color.red;
            Vector3 size = new Vector3(_maxX - _minX, 1, _maxZ - _minZ);
            Vector3 center = new Vector3((_maxX + _minX) * 0.5f, 0, (_maxZ + _minZ) * 0.5f);
            Gizmos.DrawWireCube(center, size);
        }
    }
    
    private void OnValidate()
    {
        // Автоматическая валидация в редакторе
        ValidateSettings();
    }
} 