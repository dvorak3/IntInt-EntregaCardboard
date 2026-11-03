using UnityEngine;
using TMPro;

// Crea por código la mirilla (invierte el color) y el contador del HUD.
// Pónselo al Jugador.
public class InterfazVR : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camara;
    [Tooltip("Material con el shader Custom/ReticleInvert (MatMirilla)")]
    public Material materialMirilla;
    [Tooltip("Fuente pixelada (TMP Font Asset). Opcional")]
    public TMP_FontAsset fuente;

    [Header("Mirilla")]
    public float distanciaMirilla = 2f;
    public float tamanoMirilla = 0.02f;

    [Header("Contador")]
    public Vector3 posicionContador = new Vector3(0.3f, 0.18f, 1.5f);
    public float tamanoFuente = 36f;

    [HideInInspector] public TMP_Text contador;

    void Awake()
    {
        if (camara == null)
            camara = Camera.main != null ? Camera.main.transform : transform;

        CrearMirilla();
        CrearContador();
    }

    void CrearMirilla()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Mirilla";

        // Sin collider, para que el raycast de la mirada no choque con ella
        DestroyImmediate(quad.GetComponent<Collider>());

        quad.transform.SetParent(camara, false);
        quad.transform.localPosition = new Vector3(0f, 0f, distanciaMirilla);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(tamanoMirilla, tamanoMirilla, 1f);

        Renderer r = quad.GetComponent<Renderer>();
        if (materialMirilla != null)
        {
            r.sharedMaterial = materialMirilla;
        }
        else
        {
            Shader s = Shader.Find("Custom/ReticleInvert");
            if (s != null) r.material = new Material(s);
            else Debug.LogWarning("InterfazVR: asigna MatMirilla en el Inspector.");
        }
    }

    void CrearContador()
    {
        // Canvas en el mundo, hijo de la cámara (en VR estéreo un Canvas normal no se ve)
        GameObject canvasGO = new GameObject("HUD_Contador");
        canvasGO.transform.SetParent(camara, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 100f);
        rt.localScale = Vector3.one * 0.001f;
        rt.localPosition = posicionContador;
        rt.localRotation = Quaternion.identity;

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(canvasGO.transform, false);

        TextMeshProUGUI tmp = textoGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = tamanoFuente;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color = Color.white;
        tmp.text = "SETAS 0/0";
        if (fuente != null) tmp.font = fuente;

        RectTransform trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        contador = tmp;
    }
}