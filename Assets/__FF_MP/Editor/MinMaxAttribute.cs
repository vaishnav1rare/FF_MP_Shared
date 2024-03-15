using UnityEditor;
using UnityEngine;

[System.Serializable]
public class MinMaxAttribute : PropertyAttribute
{
    public float minLimit;
    public float maxLimit;

    public MinMaxAttribute(float min, float max)
    {
        minLimit = min;
        maxLimit = max;
    }
}


