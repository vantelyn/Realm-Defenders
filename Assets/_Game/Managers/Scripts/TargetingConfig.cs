using UnityEngine;
using Game.Buildings;

namespace Game.Config
{

[CreateAssetMenu(fileName = "TargetingConfig", menuName = "Game/Targeting Config")]

public class TargetingConfig : ScriptableObject
{
    [Header("Layers")]
    public LayerMask unitsLayer;
    public LayerMask buildingsLayer;
    public LayerMask enemyLayer;
    public LayerMask treeLayer;
    public LayerMask sheepLayer;

    [Header("Tags")]
    public string unitTag = "Unit";
    public string buildingTag = "Building";
}
}
