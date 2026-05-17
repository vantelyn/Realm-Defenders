using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Units;
using Game.Combat;
using Game.Managers;

namespace Game.UI
{
public class KingPortrait : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Image hpFill;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject deadOverlay;
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.35f;

    private void Awake() {
        if (portraitImage != null && portraitSprite != null) portraitImage.sprite = portraitSprite;
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (deadOverlay != null) deadOverlay.SetActive(false);
        FutureKing.OnKingDied += HandleKingDied;
    }
    private void OnDestroy() {
        FutureKing.OnKingDied -= HandleKingDied;
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
    }
    private void Update() {
        var k = FutureKing.Instance;
        bool alive = k != null;
        if (hpFill != null && alive) {
            var rec = k.GetComponent<DamageReceiverPlayer>();
            if (rec != null && rec.MaxHealth > 0) hpFill.fillAmount = (float)rec.CurrentHealth / rec.MaxHealth;
        }
        if (exitButton != null) exitButton.gameObject.SetActive(alive && k.IsGarrisoned);
    }
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        float t = Time.unscaledTime;
        if (t - lastClickTime < DOUBLE_CLICK_TIME) { FocusCameraOnKing(); lastClickTime = 0f; }
        else lastClickTime = t;
    }
    private void FocusCameraOnKing() {
        var k = FutureKing.Instance;
        if (k == null) return;
        // Seleccionar al rey (no se selecciona si esta garrisoned: el GO esta inactivo).
        var sm = Object.FindFirstObjectByType<SelectionManager>();
        if (sm != null && !k.IsGarrisoned) sm.SelectOnly(k);
        // Camara: si esta dentro de un edificio, snap al edificio sin follow.
        if (k.IsGarrisoned && k.CurrentBuilding != null) {
            CameraManager.SetFollowTarget(k.CurrentBuilding.transform);
            CameraManager.SetFollowTarget(null); // snap + libera para WASD
        } else {
            CameraManager.SetFollowTarget(k.transform);
            CameraManager.SetFollowTarget(null);
        }
    }
    private void OnExitClicked() {
        var k = FutureKing.Instance;
        if (k == null || !k.IsGarrisoned || k.CurrentBuilding == null) return;
        k.OnExitedBuilding(k.CurrentBuilding.DoorPosition);
    }
    private void HandleKingDied() {
        if (deadOverlay != null) deadOverlay.SetActive(true);
        if (hpFill != null) hpFill.fillAmount = 0f;
        if (exitButton != null) exitButton.gameObject.SetActive(false);
    }
}
}
