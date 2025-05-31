using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingsConfig", menuName = "Scriptable Objects/BuildingsConfig")]
public class BuildingsConfig : ScriptableObject
{
    public List<BuildingData> buildings = new List<BuildingData>();

}

[System.Serializable]
public class BuildingData
{
    [Header("Основные")]
    public string buildingName;
    public Vector2Int size = Vector2Int.one;
    [Header("Префабы и материалы")]
    public GameObject buildingPrefab;
    public GameObject ghostPrefab;
    public Material ghostMaterial;
    [Header("UI")]
    public Sprite icon; // Иконка для отображения на кнопке
    
    [Header("Цвета")]
    public Color readyColor = Color.green;
    public Color errorColor = Color.red;
}
