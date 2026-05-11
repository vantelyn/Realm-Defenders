using UnityEngine;

namespace Game.AI
{

public interface IUnitAI
{
    void Enable();
    void Disable();
    void SetHome(Vector3 position);

    // Hooks opcionales: se invocan cuando la unidad entra/sale de un edificio.
    void OnHostEnteredBuilding() { }
    void OnHostExitedBuilding() { }
}
}
