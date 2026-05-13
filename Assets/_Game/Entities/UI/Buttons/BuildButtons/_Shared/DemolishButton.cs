using UnityEngine;
using UnityEngine.UI;
using Game.Managers;

namespace Game.UI
{
[RequireComponent(typeof(Button))]
public class DemolishButton : MonoBehaviour
{
    [SerializeField] private BuildingManager placer;
    [SerializeField] private Image targetImage;
    [SerializeField] private Color activeColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    private Button button;
    private Color baseColor;
    private bool wasActive;
    private void Awake() {
        button = GetComponent<Button>();
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null) baseColor = targetImage.color;
        button.onClick.AddListener(OnClicked);
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(OnClicked); }
    private void Update() {
        bool active = placer != null && placer.IsDemolishing;
        if (active == wasActive) return;
        wasActive = active;
        if (targetImage != null) targetImage.color = active ? activeColor : baseColor;
    }
    private void OnClicked() {
        if (placer == null) return;
        if (placer.IsDemolishing) { placer.CancelDemolish(); return; }
        bool sticky = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        placer.BeginDemolish(sticky);
    }
}
}
