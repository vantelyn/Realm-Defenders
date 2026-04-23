using UnityEngine;

[CreateAssetMenu(fileName = "TargetingConfig", menuName = "Game/Targeting Config")]
public class TargetingConfig : ScriptableObject
{
    [Header("Layers")]
    [Tooltip("Layer de unidades aliadas clicables (tag 'Unit').")]
    public LayerMask unitsLayer;

    [Tooltip("Layer de edificios aliados clicables (tag 'Building').")]
    public LayerMask buildingsLayer;

    [Tooltip("Unión de unitsLayer y buildingsLayer. Se recalcula automáticamente.")]
    public LayerMask selectableLayer => unitsLayer | buildingsLayer;

    [Tooltip("Layer de unidades/edificios enemigos.")]
    public LayerMask enemyLayer;

    [Tooltip("Layer de árboles.")]
    public LayerMask treeLayer;

    [Header("Tags")]
    public string unitTag = "Unit";
    public string buildingTag = "Building";
}