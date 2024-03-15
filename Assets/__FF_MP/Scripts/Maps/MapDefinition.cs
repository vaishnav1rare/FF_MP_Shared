using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Map Definition", menuName = "Scriptable Object/Map Definiton")]
public class MapDefinition : ScriptableObject
{
    public string mapName;
    public Sprite mapIcon;
    public int buildIndex;
}
