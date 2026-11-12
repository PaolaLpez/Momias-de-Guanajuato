using UnityEngine;

/// <summary>
/// Utilidad centralizada para escalar interfaces IMGUI (OnGUI) de forma responsiva
/// en celulares (pantallas pequeñas o WebGL con alta densidad de píxeles) y PC.
/// </summary>
public static class ResponsiveGUIHelper
{
    public const float DefaultRefWidth = 900f;
    public const float DefaultRefHeight = 520f;

    /// <summary>
    /// Calcula el factor de escala uniforme para que la resolución lógica de referencia
    /// quepa completamente dentro de la pantalla actual.
    /// </summary>
    public static float GetScale(
        float refWidth = DefaultRefWidth,
        float refHeight = DefaultRefHeight,
        float minScale = 0.45f,
        float maxScale = 4.0f)
    {
        if (Screen.width <= 0 || Screen.height <= 0) return 1f;

        float sx = Screen.width / Mathf.Max(1f, refWidth);
        float sy = Screen.height / Mathf.Max(1f, refHeight);
        return Mathf.Clamp(Mathf.Min(sx, sy), minScale, maxScale);
    }

    /// <summary>
    /// Aplica una matriz de escala uniforme a GUI.matrix y devuelve las dimensiones virtuales
    /// equivalentes de la pantalla actual. Siempre restaurar con EndScaledGUI(prevMatrix).
    /// </summary>
    public static float BeginScaledGUI(
        out float virtualWidth,
        out float virtualHeight,
        out Matrix4x4 prevMatrix,
        float refWidth = DefaultRefWidth,
        float refHeight = DefaultRefHeight,
        float minScale = 0.45f,
        float maxScale = 4.0f)
    {
        prevMatrix = GUI.matrix;
        float s = GetScale(refWidth, refHeight, minScale, maxScale);
        virtualWidth = Screen.width / s;
        virtualHeight = Screen.height / s;
        GUI.matrix = prevMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        return s;
    }

    /// <summary>
    /// Restaura la matriz previa de GUI para no afectar otros scripts OnGUI.
    /// </summary>
    public static void EndScaledGUI(Matrix4x4 prevMatrix)
    {
        GUI.matrix = prevMatrix;
    }

    /// <summary>
    /// Convierte un rectángulo en espacio virtual IMGUI a píxeles reales de pantalla.
    /// </summary>
    public static Rect VirtualRectToScreen(Rect virtualRect, float scale)
    {
        return new Rect(
            virtualRect.x * scale,
            virtualRect.y * scale,
            virtualRect.width * scale,
            virtualRect.height * scale
        );
    }

    /// <summary>
    /// Convierte un punto devuelto por Camera.WorldToScreenPoint a coordenadas virtuales de GUI.
    /// </summary>
    public static Vector2 WorldScreenToVirtualGUI(Vector3 worldScreenPos, float scale)
    {
        return new Vector2(
            worldScreenPos.x / scale,
            (Screen.height - worldScreenPos.y) / scale
        );
    }
}
