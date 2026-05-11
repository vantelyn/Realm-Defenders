using UnityEngine;
using Game.Buildings;
using Game.Combat;
using Game.Units;

namespace Game.AI
{

[System.Serializable]
public class BuildingGarrisonHelper
{
    [SerializeField] private bool seekBuildings = false;
    [SerializeField] private float searchRadius = 12f;
    [SerializeField] private LayerMask buildingsLayer;
    [SerializeField] private string buildingTag = "Building";
    [SerializeField] private float doorArriveDistance = 0.3f;

    private Building targetBuilding;

    public bool SeekBuildings => seekBuildings;
    public Building TargetBuilding => targetBuilding;
    public bool HasTarget => targetBuilding != null;

    public void ClearTarget() { targetBuilding = null; }
    public void SetTarget(Building building) { targetBuilding = building; }

    public Building FindFreeBuilding(Vector3 origin)
    {
        return QueryService.FindClosestFreeBuilding(origin, searchRadius, buildingsLayer, buildingTag);
    }

    public bool IsTargetValid()
    {
        return targetBuilding != null && targetBuilding.HasFreeSlot;
    }

    public bool IsAtDoor(Vector3 unitPosition)
    {
        if (targetBuilding == null) return false;
        float distSqr = ((Vector2)unitPosition - (Vector2)targetBuilding.DoorPosition).sqrMagnitude;
        return distSqr <= doorArriveDistance * doorArriveDistance;
    }

    public bool TryEnter(PlayerUnit unit)
    {
        if (targetBuilding == null) return false;
        bool entered = targetBuilding.TryEnter(unit);
        targetBuilding = null;
        return entered;
    }

    public Vector3 GetDoorPosition() => targetBuilding != null ? targetBuilding.DoorPosition : Vector3.zero;
}
}
