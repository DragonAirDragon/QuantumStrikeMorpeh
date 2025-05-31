using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitsConfig", menuName = "Scriptable Objects/UnitsConfig")]
public class UnitsConfig : ScriptableObject
{
    public List<UnitData> units = new List<UnitData>();
}


[System.Serializable]
public class UnitData
{
    [Header("Основные")]
    public string unitName;
    [Header("Префабы и материалы")]
    public GameObject unitPrefab;
    [Header("UI")]
    public Sprite icon; // Иконка для отображения на кнопке
    [Header("Строительство")]
    public float buildTime;
}