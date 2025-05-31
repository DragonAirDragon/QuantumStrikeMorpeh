using Scellecs.Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;

[System.Serializable]
[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
public struct GridComponent : IComponent 
{
    public bool isActive;
    public float yOffset;
    public int size;
    public float gridScale;
    public Material material;
    public Transform transform;
    public GameObject gameObject;
}