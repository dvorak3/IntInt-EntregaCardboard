using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorEscenario : MonoBehaviour
{
    [Header("1. Objeto Recuperador (Centro)")]
    public GameObject prefabRecuperador; // Si está vacío, el script buscará 'Stump_01' automáticamente

    [Header("2. Objetos Recolectables")]
    public GameObject[] prefabsRecolectables; // Si está vacío, buscará 'Mushroom_01' y 'Mushroom_02'
    public int cantidadRecolectables = 5;

    [Header("3. Árboles de Borde (Exteriores)")]
    public GameObject[] arbolesBorde;

    [Header("4. Árboles de Interior")]
    public GameObject[] arbolesInterior;

    [Header("5. Objetos de Decoración")]
    public GameObject[] elementosDecoracion;

    [Header("Zonas y Radios (Desde el centro X:0, Z:0)")]
    public float radioCentroLibre = 10f;
    public float radioZonaMedia = 26f;
    public float radioZonaBorde = 46f;

    [Header("Cantidades a Generar")]
    public int cantidadArbolesBorde = 200;
    public int cantidadArbolesInterior = 45;
    public int cantidadDecoraciones = 35;

    [Header("Físicas y Distancia")]
    public float distanciaMinimaEntreObjetos = 1.1f;
    public LayerMask capaSuelo;

    private void Start()
    {
        Generar();
    }

    [ContextMenu("Generar Escenario Completo")]
    public void Generar()
    {
        AutoCargarPrefabsFaltantes();

        // Limpiar elementos generados previamente
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        List<Vector3> posicionesOcupadas = new List<Vector3>();

        // 1. Generar Recuperador en el centro
        GenerarRecuperador(posicionesOcupadas);

        // 2. Generar Recolectables con Tag 'Recolectable'
        GenerarGrupo(prefabsRecolectables, cantidadRecolectables, 2.5f, radioZonaMedia, posicionesOcupadas, 0.9f, 1.1f, "Recolectable");

        // 3. Árboles de Borde
        GenerarGrupo(arbolesBorde, cantidadArbolesBorde, radioZonaMedia, radioZonaBorde, posicionesOcupadas, 0.9f, 1.3f, "Untagged");

        // 4. Árboles Interiores
        GenerarGrupo(arbolesInterior, cantidadArbolesInterior, radioCentroLibre, radioZonaMedia, posicionesOcupadas, 0.85f, 1.2f, "Untagged");

        // 5. Decoración
        GenerarGrupo(elementosDecoracion, cantidadDecoraciones, radioCentroLibre, radioZonaBorde, posicionesOcupadas, 0.7f, 1.25f, "Untagged");
    }

    private void AutoCargarPrefabsFaltantes()
    {
#if UNITY_EDITOR
        // Buscar Recuperador (Stump_01) si no está asignado
        if (prefabRecuperador == null)
        {
            string[] guids = AssetDatabase.FindAssets("Stump_01 t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                prefabRecuperador = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        // Buscar Recolectables (Mushroom_01, Mushroom_02) si está vacío
        if (prefabsRecolectables == null || prefabsRecolectables.Length == 0)
        {
            List<GameObject> recolectablesEncontrados = new List<GameObject>();
            string[] nombres = { "Mushroom_01", "Mushroom_02" };
            
            foreach (string nombre in nombres)
            {
                string[] guids = AssetDatabase.FindAssets(nombre + " t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) recolectablesEncontrados.Add(prefab);
                }
            }
            prefabsRecolectables = recolectablesEncontrados.ToArray();
        }
#endif
    }

    private void GenerarRecuperador(List<Vector3> posiciones)
    {
        if (prefabRecuperador == null) return;

        Vector3 origen = transform.position + new Vector3(0, 20f, 0);
        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 50f, capaSuelo))
        {
            GameObject recuperador = Instantiate(prefabRecuperador, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
            AsignarTagSeguro(recuperador, "Recuperador");
            posiciones.Add(hit.point);
        }
    }

    private void GenerarGrupo(GameObject[] prefabs, int cantidad, float radioMin, float radioMax, List<Vector3> posiciones, float escalaMin, float escalaMax, string tagAsignado)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int creados = 0;
        int intentos = 0;

        while (creados < cantidad && intentos < cantidad * 40)
        {
            intentos++;

            float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radio = Random.Range(radioMin, radioMax);

            Vector3 origenRaycast = transform.position + new Vector3(Mathf.Cos(angulo) * radio, 20f, Mathf.Sin(angulo) * radio);

            if (Physics.Raycast(origenRaycast, Vector3.down, out RaycastHit hit, 50f, capaSuelo))
            {
                Vector3 puntoPiso = hit.point;

                bool demasiadoCerca = false;
                foreach (Vector3 pos in posiciones)
                {
                    if (Vector3.Distance(pos, puntoPiso) < distanciaMinimaEntreObjetos)
                    {
                        demasiadoCerca = true;
                        break;
                    }
                }

                if (!demasiadoCerca)
                {
                    posiciones.Add(puntoPiso);

                    GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                    Quaternion rotacionAleatoria = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    GameObject nuevoObjeto = Instantiate(prefab, puntoPiso, rotacionAleatoria, transform);
                    nuevoObjeto.transform.localScale *= Random.Range(escalaMin, escalaMax);

                    if (tagAsignado != "Untagged")
                    {
                        AsignarTagSeguro(nuevoObjeto, tagAsignado);
                    }

                    creados++;
                }
            }
        }
    }

    private void AsignarTagSeguro(GameObject obj, string nombreTag)
    {
        try
        {
            obj.tag = nombreTag;
        }
        catch
        {
            Debug.LogWarning($"El Tag '{nombreTag}' no está creado en Unity. Ve a Edit -> Project Settings -> Tags and Layers para agregarlo.");
        }
    }
}