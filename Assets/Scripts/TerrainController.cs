using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    private bool initiated;
    private int resolution = 33;
    private float rootDimensions;
    private float dimensions;
    private readonly int[] divDistance = { 1600, 800, 400, 200, 100 };
    private int maxLoD=5;
    [SerializeField]
    private int myFace;
    [SerializeField]
    private Vector2 centerCoords;
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

    [field: SerializeField]
    public TerrainController[] Neighbours { get; set; } //Adjacent TerrainControllers, clockwise starting from topleft

    [field:SerializeField]
    public int[] MyQuadrants { get; set; }
    public int MyLoD { get; set; }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void Initiate(int LoD, int[] quadrants)
    {
        initiated = false;
        MyLoD = LoD;
        myFace = quadrants[0];
        MyQuadrants = quadrants;
        planet = transform.root.gameObject;
        planetScript = planet.GetComponent<PlanetController>();
        rootDimensions = planetScript.RootDimensions;
        dimensions = rootDimensions / (Mathf.Pow(2, MyLoD));
        terrainPrefab = planetScript.TerrainPrefab;
        playerCam = GameObject.FindGameObjectWithTag("MainCamera");
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        centerCoords = Vector2.zero;
        for(int i = 1; i <= LoD; i++)
        {
            if (MyQuadrants[i] < 2)
                centerCoords.y -= rootDimensions / Mathf.Pow(2, i) / 2;
            else
                centerCoords.y += rootDimensions / Mathf.Pow(2, i) / 2;
            if (MyQuadrants[i]==0 || MyQuadrants[i] == 2)
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
        if (MyLoD<maxLoD && transform.childCount==0 && Vector3.Distance(playerCam.transform.position, transform.position + transform.rotation * (FaceToCubeCoords(Vector2.zero).normalized*rootDimensions/2)) < divDistance[MyLoD] * rootDimensions / 1000)
        {
            subdivide();
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
                float elevation = Perlin.Noise(vertexPosition*0.01f) + 0.5f*Perlin.Noise(vertexPosition * 0.1f);
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

    int[] FindNeighbour(int[] quads, int dir)
    {
        int LoD = quads.Length - 1;
        int[] nbrQuads = new int[LoD + 1];
        //Quadrants on the same side as the direction being searched
        int border1=0, border2=0;
        //Quadrants on the opposite side of the direction being searched. Finding their neighbours is simple and safe.
        int safe1=0, safe2=0;

        bool isOnFaceBorder = true;

        switch (dir)
        {
            case 0:
                break;
            case 1:
                if (LoD == 0)
                {
                    switch (quads[0])
                    {
                        case 0:
                            return new int[] { 4 };
                        case 1:
                            return new int[] { 4 };
                        case 2:
                            return new int[] { 4 };
                        case 3:
                            return new int[] { 4 };
                        case 4:
                            return new int[] { 2 };
                        case 5:
                            return new int[] { 0 };
                        default:
                            break;
                    }
                }
                border1 = 2;
                border2 = 3;
                safe1 = 0;
                safe2 = 1;

                for(int i = 1; i <= LoD; i++)
                {
                    if(quads[i]==safe1 || quads[i] == safe2)
                    {
                        isOnFaceBorder = false;
                        break;
                    }
                }
                if(isOnFaceBorder)
                {
                    switch (quads[0])
                    {
                        case 0:
                            break;
                        case 1:
                            safe1 = 1;
                            safe2 = 3;
                            break;
                        case 2:
                            safe1 = 3;
                            safe2 = 2;
                            break;
                        case 3:
                            safe1 = 2;
                            safe2 = 0;
                            break;
                        case 4:
                            safe1 = 3;
                            safe2 = 2;
                            break;
                        case 5:
                            break;
                        default:
                            Debug.LogWarning("ERROR: Invalid face");
                            break;
                    }
                }
                break;
            case 2:
                break;
            case 3:
                if (LoD == 0)
                {
                    switch (quads[0])
                    {
                        case 0:
                            return new int[] { 5 };
                        case 1:
                            return new int[] { 5 };
                        case 2:
                            return new int[] { 5 };
                        case 3:
                            return new int[] { 5 };
                        case 4:
                            return new int[] { 0 };
                        case 5:
                            return new int[] { 2 };
                        default:
                            break;
                    }
                }
                border1 = 0;
                border2 = 1;
                safe1 = 2;
                safe2 = 3;

                for (int i = 1; i <= LoD; i++)
                {
                    if (quads[i] == safe1 || quads[i] == safe2)
                    {
                        isOnFaceBorder = false;
                        break;
                    }
                }
                if (isOnFaceBorder)
                {
                    switch (quads[0])
                    {
                        case 0:
                            break;
                        case 1:
                            safe1 = 3;
                            safe2 = 1;
                            break;
                        case 2:
                            safe1 = 1;
                            safe2 = 0;
                            break;
                        case 3:
                            safe1 = 0;
                            safe2 = 2;
                            break;
                        case 4:
                            break;
                        case 5:
                            safe1 = 1;
                            safe2 = 0;
                            break;
                        default:
                            Debug.LogWarning("ERROR: Invalid face");
                            break;
                    }
                }
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            case 7:
                break;
            default:
                Debug.LogWarning("ERROR: Invalid direction");
                break;
        }

        if (!isOnFaceBorder)
        {
            if (quads[LoD] == safe1)
            {
                Array.Copy(quads, nbrQuads, LoD + 1);
                nbrQuads[LoD] = border1;
                return nbrQuads;
            }
            else if (quads[LoD] == safe2)
            {
                Array.Copy(quads, nbrQuads, LoD + 1);
                nbrQuads[LoD] = border2;
                return nbrQuads;
            }
        }
        int[] parentQuads = new int[LoD];
        Array.Copy(quads, parentQuads, LoD);
        int[] parentNbrQuads = FindNeighbour(parentQuads, dir);
        Array.Copy(parentNbrQuads, nbrQuads, LoD);
        if (quads[LoD] == border1)
        {
            nbrQuads[LoD] = safe1;
        }
        else if (quads[LoD] == border2)
        {
            nbrQuads[LoD] = safe2;
        }

        return nbrQuads;
    }

    void FixOuterSeams()
    {
        int nbrLoD = Neighbours[1].MyLoD;
        int diff=0;
        if (MyLoD >= Neighbours[1].MyLoD) {
            diff = (int)Mathf.Pow(2,MyLoD - nbrLoD)+1; //Resolution should be 1 greater than a power of 2
            print(diff);
        }
        else
        {
            Debug.LogWarning("WARNING: Trying to fix seams when LoD is less than that of neighbours");
        }
        Vector3 prevVert = vertices[0];
        Mesh myMesh = transform.GetChild(0).GetComponent<MeshFilter>().mesh;
        Vector3[] verts = myMesh.vertices;
        for (int i = 0; i < resolution; i++)
        {
            if (i % diff != 0) {
                verts[i] = prevVert;
            }
            prevVert = verts[i];
        }
        myMesh.vertices = verts;
        
    }

    TerrainController QuadsToTerrain(int[] quads)
    {
        Transform terrain=transform.root;
        for(int i = 0; i<quads.Length; i++)
        {
            terrain = terrain.GetChild(quads[i]);
        }
        return terrain.GetComponent<TerrainController>();
    }

    void subdivide()
    {
        Vector3[] offsets = new Vector3[4];
        offsets[0] = transform.position + transform.rotation * new Vector3(-dimensions / 4, -dimensions / 4, 0);
        offsets[1] = transform.position + transform.rotation * new Vector3(dimensions / 4, -dimensions / 4, 0);
        offsets[2] = transform.position + transform.rotation * new Vector3(-dimensions / 4, dimensions / 4, 0);
        offsets[3] = transform.position + transform.rotation * new Vector3(dimensions / 4, dimensions / 4, 0);
        GameObject[] subTerrains = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            int[] childQuadrants = new int[MyLoD + 2];
            for (int j = 0; j <= MyLoD; j++)
            {
                childQuadrants[j] = MyQuadrants[j];
            }
            childQuadrants[MyLoD + 1] = i;
            subTerrains[i] = GameObject.Instantiate(terrainPrefab, transform.position, transform.rotation, transform);
            subTerrains[i].GetComponent<TerrainController>().Initiate(MyLoD + 1, childQuadrants);
            subTerrains[i].transform.SetParent(transform);
        }
        meshRenderer.enabled = false;
        Neighbours = new TerrainController[8];
        Neighbours[1] = QuadsToTerrain(FindNeighbour(MyQuadrants, 1));
        Neighbours[3] = QuadsToTerrain(FindNeighbour(MyQuadrants, 3));
        FixOuterSeams();
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
            cubeCoords = new Vector3(-x, y, rootDimensions / 2);
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
