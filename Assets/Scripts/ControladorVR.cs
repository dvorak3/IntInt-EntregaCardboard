using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class ControladorVR : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraVR;

    [Header("Desplazamiento por Mirada")]
    public bool moverConMirada = true;
    public float velocidadMovimiento = 3.0f;
    [Tooltip("Ángulo de inclinación de la cabeza mirando abajo para empezar a caminar")]
    public float anguloInclinacionCaminar = 35.0f;

    [Header("Selección por Mirada (Gaze)")]
    public float distanciaMaxSeleccion = 10.0f;
    public float tiempoMiradaRequerido = 1.5f;

    [Header("Feedback visual")]
    public Color colorMirada = new Color(0.6f, 0.85f, 1f);    // azul claro
    public Color colorCarga = new Color(0.05f, 0.15f, 0.65f); // azul oscuro
    public ParticleSystem particulasFeedback;                 // prefab de partículas

    [Header("Interfaz")]
    public TMP_Text contadorTexto;

    [Header("Depuración (solo en el editor)")]
    public float sensibilidadRaton = 0.15f;

    private float contadorTiempoMirada = 0f;
    private GameObject objetoMirandoActual;
    private List<GameObject> objetosRecolectados = new List<GameObject>();
    private MaterialPropertyBlock bloque;
    private int totalRecolectables;

#if UNITY_EDITOR
    private float yawEditor;
    private float pitchEditor;
#endif

    void Start()
    {
        bloque = new MaterialPropertyBlock();

        if (camaraVR == null)
        {
            camaraVR = Camera.main != null ? Camera.main.transform : transform;
        }

#if UNITY_EDITOR
        // Parte de la orientación actual de la cámara
        Vector3 e = camaraVR.localEulerAngles;
        yawEditor = e.y;
        pitchEditor = e.x > 180f ? e.x - 360f : e.x;
#endif

    if (contadorTexto == null)
    {
        InterfazVR ui = FindFirstObjectByType<InterfazVR>();
        if (ui != null) contadorTexto = ui.contador;
    }
        totalRecolectables = GameObject.FindGameObjectsWithTag("Recolectable").Length;
        ActualizarContador();
    }

    void Update()
    {
#if UNITY_EDITOR
        ControlCamaraEditor();
#endif
        ProcesarRaycastMirada();
        MoverJugador();
    }

#if UNITY_EDITOR
    // Mantén pulsado el botón derecho del ratón y muévelo para girar la cámara.
    // Este bloque no existe en la build de Android/iOS.
    void ControlCamaraEditor()
    {
        Mouse raton = Mouse.current;
        if (raton == null || camaraVR == null) return;
        if (!raton.rightButton.isPressed) return;

        Vector2 delta = raton.delta.ReadValue();
        yawEditor += delta.x * sensibilidadRaton;
        pitchEditor -= delta.y * sensibilidadRaton;
        pitchEditor = Mathf.Clamp(pitchEditor, -89f, 89f);

        camaraVR.localRotation = Quaternion.Euler(pitchEditor, yawEditor, 0f);
    }
#endif

    void MoverJugador()
    {
        if (!moverConMirada || camaraVR == null) return;
        if (objetoMirandoActual != null) return; // mirando algo interactivo: no caminar

        float anguloX = camaraVR.eulerAngles.x;
        if (anguloX > anguloInclinacionCaminar && anguloX < 85.0f)
        {
            Vector3 direccionAvanzar = camaraVR.forward;
            direccionAvanzar.y = 0;
            direccionAvanzar.Normalize();

            transform.position += direccionAvanzar * velocidadMovimiento * Time.deltaTime;
        }
    }

    void ProcesarRaycastMirada()
    {
        Ray rayo = new Ray(camaraVR.position, camaraVR.forward);

        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxSeleccion))
        {
            GameObject detectado = BuscarInteractivo(hit.collider.transform);

            if (detectado != null) {
                if (objetoMirandoActual == detectado)
                {
                    contadorTiempoMirada += Time.deltaTime;

                    // Barra de carga: azul claro -> azul oscuro
                    float progreso = Mathf.Clamp01(contadorTiempoMirada / tiempoMiradaRequerido);
                    AplicarColor(detectado, Color.Lerp(colorMirada, colorCarga, progreso));

                    if (contadorTiempoMirada >= tiempoMiradaRequerido)
                    {
                        LimpiarColor(detectado);
                        EjecutarAccion(detectado);
                        contadorTiempoMirada = 0f;
                        objetoMirandoActual = null;
                    }
                }
                else
                {
                    // Cambio de objeto: restaurar el anterior y empezar de nuevo
                    if (objetoMirandoActual != null) LimpiarColor(objetoMirandoActual);
                    objetoMirandoActual = detectado;
                    contadorTiempoMirada = 0f;
                    AplicarColor(detectado, colorMirada);
                }
                return;
            }
        }

        // La mirada se ha apartado de cualquier objeto interactivo
        if (objetoMirandoActual != null) LimpiarColor(objetoMirandoActual);
        objetoMirandoActual = null;
        contadorTiempoMirada = 0f;
    }

    GameObject BuscarInteractivo(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("Recolectable") || t.CompareTag("Recuperador"))
                return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    void EjecutarAccion(GameObject obj)
    {
        if (obj.CompareTag("Recolectable"))
        {
            Recolectar(obj);
        }
        else if (obj.CompareTag("Recuperador"))
        {
            RecuperarTodos(obj);
        }
    }

    void Recolectar(GameObject recolectable)
    {
        if (!objetosRecolectados.Contains(recolectable))
        {
            objetosRecolectados.Add(recolectable);
            LanzarParticulas(recolectable.transform.position);
            recolectable.SetActive(false);
            ActualizarContador();
            Debug.Log("Objeto recolectado con la vista: " + recolectable.name);
        }
    }

    void RecuperarTodos(GameObject recuperador)
    {
        if (objetosRecolectados.Count == 0) return;

        Vector3 posicionSalida = recuperador.transform.position + Vector3.up * 0.5f;
        LanzarParticulas(posicionSalida);

        foreach (GameObject obj in objetosRecolectados)
        {
            obj.transform.position = posicionSalida;
            obj.SetActive(true);
            LimpiarColor(obj);
            StartCoroutine(AnimarVueloHaciaJugador(obj));
        }

        objetosRecolectados.Clear();
        ActualizarContador();
    }

    IEnumerator AnimarVueloHaciaJugador(GameObject obj)
    {
        float velocidadVuelo = 7.0f;

        while (obj != null && Vector3.Distance(obj.transform.position, transform.position + Vector3.up * 0.5f) > 0.6f)
        {
            Vector3 destino = transform.position + Vector3.up * 0.5f;
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, destino, velocidadVuelo * Time.deltaTime);
            yield return null;
        }
    }

    // ---------- Feedback visual ----------

    void AplicarColor(GameObject obj, Color color)
    {
        // Cubre los nombres de propiedad de URP (_BaseColor) y del pipeline clásico (_Color)
        bloque.SetColor("_BaseColor", color);
        bloque.SetColor("_Color", color);

        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            r.SetPropertyBlock(bloque);
        }
    }

    void LimpiarColor(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            r.SetPropertyBlock(null); // vuelve al material original
        }
    }

    void LanzarParticulas(Vector3 posicion)
    {
        if (particulasFeedback == null) return;

        ParticleSystem ps = Instantiate(particulasFeedback, posicion, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 3f);
    }

    // ---------- Interfaz ----------

    void ActualizarContador()
    {
        if (contadorTexto != null)
            contadorTexto.text = "SETAS " + objetosRecolectados.Count + "/" + totalRecolectables;
    }
}