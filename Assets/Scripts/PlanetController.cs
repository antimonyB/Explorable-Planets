using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetController : MonoBehaviour
{
    private readonly Vector3[] dirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left, Vector3.up, Vector3.down };

    [SerializeField]
    private GameObject terrainPrefab;
    private GameObject[] terrainRoots = new GameObject[6];

    public int RootDimensions { get; } = 1000;

    public GameObject TerrainPrefab { get => terrainPrefab; set => terrainPrefab = value; }

    public int IdCount { get; set; } = 0;
    public List<int> Queue { get; set; } = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 6; i++) {
            terrainRoots[i] = GameObject.Instantiate(TerrainPrefab, transform.position, transform.rotation, transform);
            terrainRoots[i].GetComponent<TerrainController>().Initiate(0,new int[] { i });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
