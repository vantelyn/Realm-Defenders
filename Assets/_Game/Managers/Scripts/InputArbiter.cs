namespace Game.Managers
{

/// <summary>
/// Coordinador mínimo para evitar que varios managers consuman el mismo input
/// en el mismo frame (especialmente Escape, que tiene 3 consumidores: cancelar
/// placement, deseleccionar unidad, abrir/cerrar pausa).
///
/// Convención de uso: cada manager comprueba <c>!EscapeConsumed</c> antes de
/// actuar y, si actúa, marca <c>EscapeConsumed = true</c>. El reset al final
/// del frame lo hace <see cref="PauseManager"/> en su LateUpdate (es el último
/// en ExecutionOrder dentro del modal stack).
/// </summary>
public static class InputArbiter
{
    public static bool EscapeConsumed;
}

}
