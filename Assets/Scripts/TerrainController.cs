using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    private bool initiated;
    private int resolution = 20;
    private float rootDimensions;
    private float dimensions;
    private readonly int[] divDistance = {1600, 800, 400, 200, 100 };
    private int myLoD; //Level of detail;
    private int maxLoD=5;
    public int myFace;
    public int[] myQuadrants; //List of own and parent quadrants
    public Vector2 centerCoords;
    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;
    private MeshRenderer meshRenderer;

    [SerializeField]
    private GameObject planet;
    private PlanetController planetScript;
    [SerializeField]
    private GameObject terrainPrefab;
    [SerializeField]
    private GameObject playerCam;
    [SerializeField]
    private GameObject sphere;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    public void Initiate(int LoD, int face, int[] quadrants)
    {
        initiated = false;
        myLoD = LoD;
        myFace = face;
        myQuadrants = quadrants;
        planet = transform.root.gameObject;
        planetScript = planet.GetComponent<PlanetController>();
        rootDimensions = planetScript.rootDimensions;
        dimensions = rootDimensions / (Mathf.Pow(2, myLoD));
        terrainPrefab = planetScript.terrainPrefab;
        playerCam = GameObject.FindGameObjectWithTag("MainCamera");
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        centerCoords = Vector2.zero;
        for(int i = 1; i <= LoD; i++)
        {
            if (myQuadrants[i] < 2)
                centerCoords.y -= rootDimensions / Mathf.Pow(2, i) / 2;
            else
                centerCoords.y += rootDimensions / Mathf.Pow(2, i) / 2;
            if (myQuadrants[i]==0 || myQuadrants[i] == 2)
                centerCoords.x -= rootDimensions / Mathf.Pow(2, i) / 2;
            else
                centerCoords.x += rootDimensions / Mathf.Pow(2, i) / 2;
        }
        
        generateMesh();
        if (LoD == 0)
        {
            //subdivide(1);
        }
        initiated = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initiated)
        {
            Debug.LogWarning("ERROR: Unexpected update before initiation");
            return;
        }
        if (myLoD<maxLoD && transform.childCount==0 && Vector3.Distance(playerCam.transform.position, transform.position + transform.rotation * (FaceToCubeCoords(Vector2.zero).normalized*rootDimensions/2)) < divDistance[myLoD])
        {
            subdivide(myLoD + 1);
        }
    }

    void generateMesh()
    {
        //Generate vertices
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        vertices = new Vector3[resolution*resolution];
        mesh.vertices = vertices;
        Vector2[] uvs = new Vector2[resolution * resolution];
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float x = (float)j/(resolution-1)*dimensions - dimensions/2;
                float y = (float)i/(resolution-1)*dimensions - dimensions/2;
                int n = i * resolution + j;
                Vector3 vertexPosition = FaceToCubeCoords(new Vector2(x, y));
                float elevation = Perlin.Noise(vertexPosition*100);
                vertices[n] = vertexPosition.normalized*rootDimensions/2*(1+elevation/100);
                uvs[n] = new Vector2(elevation, 0.1f);
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;

        //Connect vertices into triangles
        triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        for (int i = 0, n = 0; i < ((resolution - 1) * resolution) - 1; i++)
        {
            if (i % resolution == (resolution-1)) i++;
            triangles[n++] = i; triangles[n++] = i + resolution; triangles[n++] = i + 1;
            triangles[n++] = i + resolution; triangles[n++] = i + resolution + 1; triangles[n++] = i + 1;
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void subdivide(int LoD)
    {
        Vector3[] offsets = new Vector3[4];
        offsets[0] = transform.position + transform.rotation * new Vector3(-dimensions / 4, -dimensions / 4, 0);
        offsets[1] = transform.position + transform.rotation * new Vector3(dimensions / 4, -dimensions / 4, 0);
        offsets[2] = transform.position + transform.rotation * new Vector3(-dimensions / 4, dimensions / 4, 0);
        offsets[3] = transform.position + transform.rotation * new Vector3(dimensions / 4, dimensions / 4, 0);
        GameObject[] subTerrains = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            int[] childQuadrants = new int[myLoD + 2];
            for (int j = 0; j <= myLoD; j++)
            {
                childQuadrants[j] = myQuadrants[j];
            }
            childQuadrants[myLoD + 1] = i;
            subTerrains[i] = GameObject.Instantiate(terrainPrefab, transform.position, transform.rotation, transform);
            subTerrains[i].GetComponent<TerrainController>().Initiate(LoD,myFace,childQuadrants);
            subTerrains[i].transform.SetParent(transform);
        }
        meshRenderer.enabled = false;
    }

    Vector3 FaceToCubeCoords(Vector2 faceCoords)
    {
        Vector3 cubeCoords=Vector3.zero;
        float x = faceCoords.x + centerCoords.x;
        float y = faceCoords.y + centerCoords.y;
        if (myFace == 0)
        {
            cubeCoords = new Vector3(x, y, -rootDimensions / 2);
        }
        if (myFace == 1)
        {
            cubeCoords = new Vector3(rootDimensions / 2, y, x);
        }
        if (myFace == 2)
        {
            cubeCoords = new Vector3(-(x), y, rootDimensions / 2);
        }
        if (myFace == 3)
        {
            cubeCoords = new Vector3(-rootDimensions / 2, y, -(x));
        }
        if (myFace == 4)
        {
            cubeCoords = new Vector3(x, rootDimensions / 2, y);
        }
        if (myFace == 5)
        {
            cubeCoords = new Vector3(x, -rootDimensions / 2, -(y));
        }
        return cubeCoords;
    }
}
