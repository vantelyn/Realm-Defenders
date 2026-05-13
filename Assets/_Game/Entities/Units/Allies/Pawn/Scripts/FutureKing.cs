using UnityEngine;
using Game.Combat;
using Game.Buildings;

namespace Game.Units
{
public class FutureKing : PawnUnit
{
    public static FutureKing Instance { get; private set; }
    public static event System.Action OnKingDied;
    private Building hostBuilding;

    protected override void Awake() {
        base.Awake();
        Instance = this;
        var rec = GetComponent<DamageReceiverPlayer>();
        if (rec != null) rec.OnDying += HandleDied;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void HandleDied() { if (OnKingDied != null) OnKingDied(); }

    public override void OnEnteredBuilding(Building building, Transform slot) {
        if (doorOpenClip != null) AudioSource.PlayClipAtPoint(doorOpenClip, building.transform.position, doorVolume);
        currentBuilding = building;
        hostBuilding = building;
        building.OnBuildingDestroyed += HandleHostDestroyed;
        if (ai != null) ai.OnHostEnteredBuilding();
        gameObject.SetActive(false);
    }
    public override void OnExitedBuilding(Vector3 doorPosition) {
        if (hostBuilding != null) { hostBuilding.OnBuildingDestroyed -= HandleHostDestroyed; hostBuilding = null; }
        gameObject.SetActive(true);
        if (doorCloseClip != null) AudioSource.PlayClipAtPoint(doorCloseClip, doorPosition, doorVolume);
        base.OnExitedBuilding(doorPosition);
    }
    private void HandleHostDestroyed() {
        hostBuilding = null;
        // Reactivar para que el damage receiver pueda ejecutar la muerte y notificar.
        gameObject.SetActive(true);
        currentBuilding = null;
        var rec = GetComponent<DamageReceiverPlayer>();
        if (rec != null) rec.ApplyDamage(99999, false, false, Vector2.zero, 1f);
    }
}
}
