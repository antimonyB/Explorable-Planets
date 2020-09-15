using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetController : MonoBehaviour
{
    private readonly Vector3[] dirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left, Vector3.up, Vector3.down };

    [SerializeField]
    private GameObject terrainPrefab;
    public GameObject TerrainPrefab { get => terrainPrefab; set => terrainPrefab = value; }
    private GameObject[] terrainRoots = new GameObject[6];

    [SerializeField]
    private GameObject masterPlanet;
    private bool hasMasterPlanet;
    public GameObject PlayerCam { get; set; }
    [SerializeField]
    private GameObject myCam;
    public GameObject MyCam { get => myCam; set => myCam = value; }

    [SerializeField]
    private int rootDimensions = 1000;
    public int RootDimensions { get => rootDimensions; }
    [SerializeField]
    private int maxLoD = 5;
    public int MaxLoD { get => maxLoD; }

    public int IdCount { get; set; } = 0;
    public List<int> Queue { get; set; } = new List<int>();


    // Start is called before the first frame update
    void Start()
    {
        PlayerCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (masterPlanet == null)
        {
            myCam = PlayerCam;
        }
        else
        {
            hasMasterPlanet = true;
        }
        for (int i = 0; i < 6; i++) {
            terrainRoots[i] = GameObject.Instantiate(TerrainPrefab, transform.position, transform.rotation, transform);
            terrainRoots[i].GetComponent<TerrainController>().Initiate(0,new int[] { i });
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hasMasterPlanet)
        {
            MyCam.transform.position = transform.position + (PlayerCam.transform.position - masterPlanet.transform.position) * transform.localScale.x / masterPlanet.transform.localScale.x;
        }
    }
}
