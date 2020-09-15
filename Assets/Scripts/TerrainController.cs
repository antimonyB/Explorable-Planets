using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    private int resolution = 33;
    private float rootDimensions;
    private float dimensions;
    private readonly int[] divDistance = { 1600, 800, 400, 200, 100, 50 };
    private int maxLoD;
    int terrainLoadSpeed = 100;
    [SerializeField]
    private int myFace;
    [SerializeField]
    private Vector2 centerCoords;
    private Vector3[] vertices;
    private int[] triangles;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private bool isUndividing;
    private bool isSubdividing;
    private int quadrantID;
    private List<int> queue;
    [SerializeField]
    private GameObject planet;
    private PlanetController planetScript;
    [SerializeField]
    private GameObject terrainPrefab;
    private GameObject myCam;
    [SerializeField]
    private GameObject sphere;

    [field: SerializeField]
    public TerrainController[] Neighbours { get; set; } //Adjacent TerrainControllers, clockwise starting from topleft

    [field:SerializeField]
    public int[] MyQuadrants { get; set; }
    public int MyLoD { get; set; }
    public Vector2 CenterCoords { get => centerCoords; set => centerCoords = value; }
    public bool Initiated { get; set; }


    // Start is called before the first frame update
    void Start()
    {
    }

    public void Initiate(int LoD, int[] quadrants, bool enableRenderer = true)
    {
        Initiated = false;
        MyLoD = LoD;
        myFace = quadrants[0];
        MyQuadrants = quadrants;
        planet = transform.root.gameObject;
        planetScript = planet.GetComponent<PlanetController>();
        quadrantID = planetScript.IdCount;
        planetScript.IdCount++;
        queue = planetScript.Queue;
        rootDimensions = planetScript.RootDimensions;
        dimensions = rootDimensions / (Mathf.Pow(2, MyLoD));
        maxLoD = planetScript.MaxLoD;
        terrainPrefab = planetScript.TerrainPrefab;
        myCam = planetScript.MyCam;
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (!enableRenderer)
        {
            meshRenderer.enabled = false;
        }

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
        
        StartCoroutine(QueueGeneration());
    }

    // Update is called once per frame
    void Update()
    {
        if (!Initiated)
        {
            return;
        }
        if (queue.Count > 0)
        {
            return;
        }
        float temp1 = Vector3.Distance(myCam.transform.position, transform.position + transform.rotation * (FaceToCubeCoords(Vector2.zero).normalized * rootDimensions / 2));
        float temp2 = divDistance[MyLoD] * rootDimensions * transform.lossyScale.x / 1000f;
        if (!isUndividing && !isSubdividing && MyLoD<maxLoD && transform.childCount==0 && Vector3.Distance(myCam.transform.position, transform.position + transform.rotation * (FaceToCubeCoords(Vector2.zero).normalized*rootDimensions * transform.lossyScale.x / 2)) < divDistance[MyLoD] * rootDimensions * transform.lossyScale.x / 1000f)
        {
            StartCoroutine(Subdivide());
        }
        else if (!isUndividing && !isSubdividing && transform.childCount > 0 && Vector3.Distance(myCam.transform.position, transform.position + transform.rotation * (FaceToCubeCoords(Vector2.zero).normalized * rootDimensions * transform.lossyScale.x / 2)) > divDistance[MyLoD] * rootDimensions * transform.lossyScale.x / 1000f)
        {
            StartCoroutine(Undivide());
        }
    }

    IEnumerator QueueGeneration()
    {
        queue.Add(quadrantID);
        while (queue[0] != quadrantID)
        {
            yield return null;
        }
        StartCoroutine(GenerateMesh());
    }

    IEnumerator GenerateMesh()
    {
        //Generate vertices
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        vertices = new Vector3[resolution*resolution];
        mesh.vertices = vertices;
        Vector2[] uvs = new Vector2[resolution * resolution];
        Vector2[] uvs2 = new Vector2[resolution * resolution];
        Vector2[] uvs3 = new Vector2[resolution * resolution];
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                int n = i * resolution + j;
                Vector3 vertexPosition = LocateCubeVertex(n, MyQuadrants);
                float elevation = GenerateElevation(vertexPosition);
                vertices[n] = ApplyElevation(vertexPosition, elevation);
                uvs[n] = CalculateUVs(elevation, vertexPosition, vertices[n])[0];
                uvs2[n] = CalculateUVs(elevation, vertexPosition, vertices[n])[1];
                uvs3[n] = CalculateUVs(elevation, vertexPosition, vertices[n])[2];
                if (n % terrainLoadSpeed == 0)
                {
                    yield return null;
                }
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.uv2 = uvs2;
        mesh.uv3 = uvs3;

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
        mesh.RecalculateBounds();
        Initiated = true;
        queue.RemoveAt(0);
    }

    int[] FindNeighbour(int[] quads, int dir)
    {
        int LoD = quads.Length - 1;
        int[] nbrQuads = new int[LoD + 1];

        if(LoD==0){
            switch (dir)
            {
                case 0:
                    break;
                case 1:
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
                    break;
                case 2:
                    break;
                case 3:
                    switch (quads[0])
                    {
                        case 0:
                            return new int[] { 1 };
                        case 1:
                            return new int[] { 2 };
                        case 2:
                            return new int[] { 3 };
                        case 3:
                            return new int[] { 0 };
                        case 4:
                            return new int[] { 1 };
                        case 5:
                            return new int[] { 1 };
                        default:
                            break;
                    }
                    break;
                case 4:
                    break;
                case 5:
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
                    break;
                case 6:
                    break;
                case 7:
                    switch (quads[0])
                    {
                        case 0:
                            return new int[] { 3 };
                        case 1:
                            return new int[] { 0 };
                        case 2:
                            return new int[] { 1 };
                        case 3:
                            return new int[] { 2 };
                        case 4:
                            return new int[] { 3 };
                        case 5:
                            return new int[] { 3 };
                        default:
                            break;
                    }
                    break;
                default:
                    Debug.LogWarning("ERROR: Invalid direction");
                    break;
            }
        }

        int[,] borderIndices = FindBorderIndices(quads, dir);
        
        bool isOnFaceBorder = true;
        for(int i = 1; i < quads.Length; i++)
        {
            if(quads[i]!=borderIndices[1,0] && quads[i] != borderIndices[1,1])
            {
                isOnFaceBorder = false;
                break;
            }
        }

        if (!isOnFaceBorder)
        {
            if (quads[LoD] == borderIndices[0, 0])
            {
                Array.Copy(quads, nbrQuads, LoD + 1);
                nbrQuads[LoD] = borderIndices[1, 0];
                return nbrQuads;
            }
            else if (quads[LoD] == borderIndices[0, 1])
            {
                Array.Copy(quads, nbrQuads, LoD + 1);
                nbrQuads[LoD] = borderIndices[1, 1];
                return nbrQuads;
            }
        }
        //If isOnFaceBorder then quad[LoD] can safely be assumed to be bordering, not safe
        int[] parentQuads = new int[LoD];
        Array.Copy(quads, parentQuads, LoD);
        int[] parentNbrQuads = FindNeighbour(parentQuads, dir);
        Array.Copy(parentNbrQuads, nbrQuads, LoD);
        if (quads[LoD] == borderIndices[1, 0])
        {
            nbrQuads[LoD] = borderIndices[2, 0];
        }
        else if (quads[LoD] == borderIndices[1, 1])
        {
            nbrQuads[LoD] = borderIndices[2, 1];
        }

        return nbrQuads;
    }

    int[,] FindBorderIndices(int[] quads, int dir)
    {
        /*
         * Indices[0,_] are away from the border
         * Indices[1,_] are near the border
         * Indices[2,_] are on the opposite side of the border. (Same as [0,_] if on the same face)
         * 
         * Botleft index is 0, botright 1, topleft 2, topright 3
         * Directions are relative to the face. When border is the border of a face, Indices[2,_] may have a seemingly unusual value due to the face change.
         * 
         * Basic example when border direction is top and not the border of the face:
         * 
         *      ||||||||||||||||||||||
         *      ||        ||        ||
         *      ||        ||        ||
         *      ||        ||        ||
         *      ||||||||||||||||||||||
         *      ||        ||        ||
         *      || [2][0] || [2][1] ||
         *      ||  =0    ||  =1    ||
         *      ||||||||||||||||||||||
         *      |||||||||||||||||||||| <-- Border
         *      ||        ||        ||
         *      || [1][0] || [1][1] ||
         *      ||  =2    ||  =3    ||
         *      ||||||||||||||||||||||
         *      ||        ||        ||
         *      || [0][0] || [0][1] ||
         *      ||  =0    ||  =1    ||
         *      ||||||||||||||||||||||
         *      
         */
        int[,] indices = new int[3,2];
        switch (dir)
        {
            case 0:
                break;
            case 1:
                indices[0, 0] = 0;
                indices[0, 1] = 1;
                indices[1, 0] = 2;
                indices[1, 1] = 3;
                break;
            case 2:
                break;
            case 3:
                indices[0, 0] = 0;
                indices[0, 1] = 2;
                indices[1, 0] = 1;
                indices[1, 1] = 3;
                break;
            case 4:
                break;
            case 5:
                indices[0, 0] = 2;
                indices[0, 1] = 3;
                indices[1, 0] = 0;
                indices[1, 1] = 1;
                break;
            case 6:
                break;
            case 7:
                indices[0, 0] = 1;
                indices[0, 1] = 3;
                indices[1, 0] = 0;
                indices[1, 1] = 2;
                break;
            default:
                break;
        }

        bool isOnFaceBorder = true;
        for(int i = 1; i < quads.Length; i++)
        {
            if(quads[i]!=indices[1,0] && quads[i] != indices[1,1])
            {
                isOnFaceBorder = false;
                break;
            }
        }
        int face = quads[0];
        if (isOnFaceBorder)
        {
            switch (dir)
            {
                case 0:
                    break;
                case 1:
                    switch (face)
                    {
                        case 0:
                            indices[2, 0] = 0;
                            indices[2, 1] = 1;
                            break;
                        case 1:
                            indices[2, 0] = 1;
                            indices[2, 1] = 3;
                            break;
                        case 2:
                            indices[2, 0] = 3;
                            indices[2, 1] = 2;
                            break;
                        case 3:
                            indices[2, 0] = 2;
                            indices[2, 1] = 0;
                            break;
                        case 4:
                            indices[2, 0] = 3;
                            indices[2, 1] = 2;
                            break;
                        case 5:
                            indices[2, 0] = 0;
                            indices[2, 1] = 1;
                            break;
                        default:
                            Debug.LogWarning("ERROR: Invalid face");
                            break;
                    }
                    break;
                case 2:
                    break;
                case 3:
                    switch (face)
                    {
                        case 0:
                            indices[2, 0] = 0;
                            indices[2, 1] = 2;
                            break;
                        case 1:
                            indices[2, 0] = 0;
                            indices[2, 1] = 2;
                            break;
                        case 2:
                            indices[2, 0] = 0;
                            indices[2, 1] = 2;
                            break;
                        case 3:
                            indices[2, 0] = 0;
                            indices[2, 1] = 2;
                            break;
                        case 4:
                            indices[2, 0] = 2;
                            indices[2, 1] = 3;
                            break;
                        case 5:
                            indices[2, 0] = 1;
                            indices[2, 1] = 0;
                            break;
                        default:
                            Debug.LogWarning("ERROR: Invalid face");
                            break;
                    }
                    break;
                case 4:
                    break;
                case 5:
                    switch (face)
                    {
                        case 0:
                            indices[2, 0] = 2;
                            indices[2, 1] = 3;
                            break;
                        case 1:
                            indices[2, 0] = 3;
                            indices[2, 1] = 1;
                            break;
                        case 2:
                            indices[2, 0] = 1;
                            indices[2, 1] = 0;
                            break;
                        case 3:
                            indices[2, 0] = 0;
                            indices[2, 1] = 2;
                            break;
                        case 4:
                            indices[2, 0] = 2;
                            indices[2, 1] = 3;
                            break;
                        case 5:
                            indices[2, 0] = 1;
                            indices[2, 1] = 0;
                            break;
                        default:
                            break;
                    }
                    break;
                case 6:
                    break;
                case 7:
                    switch (face)
                    {
                        case 0:
                            indices[2, 0] = 1;
                            indices[2, 1] = 3;
                            break;
                        case 1:
                            indices[2, 0] = 1;
                            indices[2, 1] = 3;
                            break;
                        case 2:
                            indices[2, 0] = 1;
                            indices[2, 1] = 3;
                            break;
                        case 3:
                            indices[2, 0] = 1;
                            indices[2, 1] = 3;
                            break;
                        case 4:
                            indices[2, 0] = 3;
                            indices[2, 1] = 2;
                            break;
                        case 5:
                            indices[2, 0] = 0;
                            indices[2, 1] = 1;
                            break;
                        default:
                            Debug.LogWarning("ERROR: Invalid face");
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            indices[2, 0] = indices[0, 0];
            indices[2, 1] = indices[0, 1];
        }
        return indices;
    }

    void FixRootSeams()
    {
        for (int nbrDir = 1; nbrDir < 8; nbrDir += 2)
        {
            Neighbours[nbrDir] = QuadsToTerrain(FindNeighbour(MyQuadrants, nbrDir));
            if (Neighbours[nbrDir].transform.childCount > 1)
            {
                Neighbours[nbrDir].FixAllSeams();
            }
        }
    }

    void FixOuterSeam(int nbrDir)
    {
        Neighbours[nbrDir] = QuadsToTerrain(FindNeighbour(MyQuadrants, nbrDir));
        int nbrLoD = Neighbours[nbrDir].MyLoD;
        int dtl=MyLoD+1;
        int nbrDtl=nbrLoD;
        if(Neighbours[nbrDir].transform.childCount > 1){
            nbrDtl++;
        }
        int vertRatio=0;
        bool isEqualDtl = false;
        if (MyLoD >= nbrLoD) {
            vertRatio = (int)Mathf.Pow(2,dtl - nbrDtl); //Resolution should be 1 greater than a power of 2
        }
        else
        {
            Debug.LogWarning("WARNING: Trying to fix seams when LoD is different than that of neighbour");
        }
        if(dtl==nbrDtl){
            isEqualDtl = true;
        }
        int startIndex = resolution * (resolution - 1); //Topleft vertex
        int nbrStartIndex = 0; //Botleft vertex;
        int[,] borderIndices=FindBorderIndices(MyQuadrants,nbrDir);
        int vertDir = borderIndices[1,1] - borderIndices[1,0];
        int nbrVertDir = borderIndices[2,1] - borderIndices[2,0];
        switch(borderIndices[1,0]){
            case 0:
                startIndex = 0; //Botleft vertex
                break;
            case 1:
                startIndex = resolution - 1; //Botright vertex
                break;
            case 2:
                startIndex = resolution * (resolution - 1); //Topleft vertex
                break;
            case 3:
                startIndex = resolution * resolution - 1; //Topright vertex
                break;
            default:
                Debug.LogWarning("ERROR: Unhandled direction");
                break;
        }
        switch(borderIndices[2,0]){
            case 0:
                nbrStartIndex = 0; //Botleft vertex
                break;
            case 1:
                nbrStartIndex = resolution - 1; //Botright vertex
                break;
            case 2:
                nbrStartIndex = resolution * (resolution - 1); //Topleft vertex
                break;
            case 3:
                nbrStartIndex = resolution * resolution - 1; //Topright vertex
                break;
            default:
                Debug.LogWarning("ERROR: Unhandled neighbour direction");
                break;
        }
        int step = 0;
        switch(vertDir){
            case 1:
                step = 1;
                break;
            case -1:
                step = -1;
                break;
            case 2:
                step = resolution;
                break;
            case -2:
                step = -resolution;
                break;
            default:
                Debug.LogWarning("ERROR: Invalid vertex direction");
                break;
        }
        int nbrStep = 0;
        switch(nbrVertDir){
            case 1:
                nbrStep = 1;
                break;
            case -1:
                nbrStep = -1;
                break;
            case 2:
                nbrStep = resolution;
                break;
            case -2:
                nbrStep = -resolution;
                break;
            default:
                Debug.LogWarning("ERROR: Invalid vertex direction");
                break;
        }
        for (int h = 0; h < 2; h++)
        {
            Mesh myMesh = transform.GetChild(borderIndices[1,h]).GetComponent<MeshFilter>().mesh;
            TerrainController nbr;
            if(isEqualDtl){ //Neighbour may actually be more detailed, but that depends on its children
                //Operate on the correct half of the neighbour
                nbr = Neighbours[nbrDir].transform.GetChild(borderIndices[2,h]).GetComponent<TerrainController>();
                
                //If current half of neighbour is more detailed, then tell this half of neighbour to fix the seams from its perspective
                if(nbr.transform.childCount > 0){
                    bool isOnFaceBorder = true;
                    for(int i = 1; i < MyQuadrants.Length; i++)
                    {
                        if(MyQuadrants[i] != borderIndices[1,0] && MyQuadrants[i] != borderIndices[1,1])
                        {
                            isOnFaceBorder = false;
                            break;
                        }
                    }
                    int dirFromNbr = (nbrDir+4) % 8;
                    if(isOnFaceBorder){ //Handle unusual cases that happen when neighbour is on a different face
                        if((borderIndices[2,0]==2 || borderIndices[2,1]==2) && (borderIndices[2,0]==3 || borderIndices[2,1]==3)){
                            dirFromNbr = 1;
                        }
                        else if((borderIndices[2,0]==1 || borderIndices[2,1]==1) && (borderIndices[2,0]==3 || borderIndices[2,1]==3)){
                            dirFromNbr = 3;
                        }
                        else if((borderIndices[2,0]==0 || borderIndices[2,1]==0) && (borderIndices[2,0]==1 || borderIndices[2,1]==1)){
                            dirFromNbr = 5;
                        }
                        else if((borderIndices[2,0]==0 || borderIndices[2,1]==0) && (borderIndices[2,0]==2 || borderIndices[2,1]==2)){
                            dirFromNbr = 7;
                        }
                    }
                    FixOppositeSide(nbr, dirFromNbr, borderIndices);
                }
            }
            else{
                //Since the neighbour is less detailed, it is not split into halves. Operate on the entire neighbour section the whole way through.
                nbr = Neighbours[nbrDir];
            }
            Mesh nbrMesh = nbr.GetComponent<MeshFilter>().mesh;
            Vector3[] verts = myMesh.vertices;
            Vector3[] norms = myMesh.normals;
            Vector2[] uvs = myMesh.uv;
            Vector2[] uvs2 = myMesh.uv2;
            Vector2[] uvs3 = myMesh.uv3;
            Vector3[] nbrVerts = nbrMesh.vertices;
            Vector3[] nbrNorms = nbrMesh.normals;
            Vector2[] nbrUvs = nbrMesh.uv;
            Vector2[] nbrUvs2 = nbrMesh.uv2;
            Vector2[] nbrUvs3 = nbrMesh.uv3;
            int prevIndex = 0;
            int prevNbrIndex = -1;
            for (int i = 0; i < resolution; i++)
            {
                int index = i * step + startIndex;
                int nbrIndex = i / vertRatio * nbrStep + nbrStartIndex;

                //Regenerate current vertex of neighbour's edge in case it was hidden
                if(nbrIndex != prevNbrIndex){
                    Vector3 vertexPosition = LocateCubeVertex(nbrIndex, nbr.MyQuadrants);
                    float elevation = GenerateElevation(vertexPosition);
                    nbrVerts[nbrIndex] = ApplyElevation(vertexPosition, elevation);
                    Vector2[] uvArr = CalculateUVs(elevation, vertexPosition, verts[index]);
                    nbrUvs[nbrIndex] = uvArr[0];
                    nbrUvs2[nbrIndex] = uvArr[1];
                    nbrUvs3[nbrIndex] = uvArr[2];
                }

                if (i % vertRatio != 0) //If neighbour has no corresponding vertex
                {
                    //Hide current vertex
                    verts[index] = verts[prevIndex];
                    norms[index] = norms[prevIndex];
                    uvs[index] = uvs[prevIndex];
                    uvs2[index] = uvs2[prevIndex];
                    uvs3[index] = uvs3[prevIndex];
                }
                else{
                    //Regenerate current vertex in case it has been hidden before
                    TerrainController subTerrain = transform.GetChild(borderIndices[1, h]).GetComponent<TerrainController>();
                    Vector3 vertexPosition = LocateCubeVertex(index, subTerrain.MyQuadrants);
                    float elevation = GenerateElevation(vertexPosition);
                    verts[index] = ApplyElevation(vertexPosition, elevation);
                    Vector2[] uvArr = CalculateUVs(elevation, vertexPosition, verts[index]);
                    uvs[index] = uvArr[0];
                    uvs2[index] = uvArr[1];
                    uvs3[index] = uvArr[2];

                    //Fix normal for current vertex and neighbours' corresponding vertices
                    Vector3 normal = EstimateNormal(norms[index], nbrNorms[nbrIndex]);
                    norms[index] = normal;
                    nbrNorms[nbrIndex] = normal;
                }

                prevIndex = index;
                prevNbrIndex = nbrIndex;
            }
            myMesh.vertices = verts;
            myMesh.normals = norms;
            myMesh.uv = uvs;
            myMesh.uv2 = uvs2;
            myMesh.uv3 = uvs3;
            nbrMesh.vertices = nbrVerts;
            nbrMesh.normals = nbrNorms;
            nbrMesh.uv = nbrUvs;
            nbrMesh.uv2 = nbrUvs2;
            nbrMesh.uv3 = nbrUvs3;

            //If neighbour is less detailed, start next iteration further along the same neighbour's edge
            if(!isEqualDtl)
            {
                nbrStartIndex += nbrStep * (resolution / vertRatio);
            }
        }
    }

    void FixInnerSeams()
    {
        Mesh[] subMeshes = new Mesh[4];
        for(int i = 0; i < 4; i++)
        {
            subMeshes[i] = transform.GetChild(i).GetComponent<MeshFilter>().mesh;
        }
        int[] subMeshIndices = {0, 0, 3, 3};
        int[] otherSubMeshIndices = {1, 2, 1, 2};
        int[] startIndices = {resolution - 1, resolution * (resolution - 1), 0, 0};
        int[] otherStartIndices = {0, 0, resolution * (resolution - 1), resolution - 1};
        int[] steps = {resolution, 1, 1, resolution};
        for(int i = 0; i < 4; i++)
        {
            Mesh subMesh = subMeshes[subMeshIndices[i]];
            Mesh otherSubMesh = subMeshes[otherSubMeshIndices[i]];
            int startIndex = startIndices[i];
            int otherStartIndex = otherStartIndices[i];
            int step = steps[i];
            Vector3[] norms = subMesh.normals;
            Vector3[] otherNorms = otherSubMesh.normals;
            for(int j = 1; j < resolution - 1; j++)
            {
                int index = startIndex + (j * step);
                int otherIndex = otherStartIndex + (j * step);

                TerrainController subTerrain = transform.GetChild(subMeshIndices[i]).GetComponent<TerrainController>();
                Vector3 vertexPosition = LocateCubeVertex(index, subTerrain.MyQuadrants);
                Vector3 normal = EstimateNormal(norms[index], otherNorms[otherIndex]);
                norms[index] = normal;
                otherNorms[otherIndex] = normal;
            }
            subMesh.normals = norms;
            otherSubMesh.normals = otherNorms;
        }
    }

    void FixOppositeSide(TerrainController nbr, int dirFromNbr, int[,] borderIndices)
    {
        for (int i = 0; i < 2; i++) {
            if (nbr.transform.GetChild(borderIndices[2, i]).childCount > 1) {
                FixOppositeSide(nbr.transform.GetChild(borderIndices[2, i]).GetComponent<TerrainController>(), dirFromNbr, borderIndices);
            }
        }
        TerrainController[] temp = new TerrainController[8];
        temp[dirFromNbr] = QuadsToTerrain(nbr.FindNeighbour(nbr.MyQuadrants, dirFromNbr));
        nbr.Neighbours = temp;
        nbr.FixOuterSeam(dirFromNbr);
    }

    void FixAllSeams()
    {
        for (int nbrDir = 1; nbrDir < 8; nbrDir += 2) //Loop through non diagonal neighbours, clockwise starting from top
        {
            FixOuterSeam(nbrDir);
        }
        FixInnerSeams();
        for(int i = 0; i < 4; i++)
        {
            Transform child = transform.GetChild(i);
            TerrainController childTerrain = child.GetComponent<TerrainController>();
            if (child.childCount > 0 && !childTerrain.isUndividing)
            {
                childTerrain.FixAllSeams();
            }
        }
    }

    Vector3 LocateCubeVertex(int index, int[] quads)
    {
        float dims = rootDimensions / (Mathf.Pow(2, quads.Length - 1));
        float x = (float)(index % resolution) / (resolution - 1) * dims - dims / 2;
        float y = (float)(index / resolution) / (resolution - 1) * dims - dims / 2;
        return FaceToCubeCoords(new Vector2(x, y), quads[0], QuadsToTerrain(quads).CenterCoords);
    }

    Vector3 EstimateNormal(Vector3 norm1, Vector3 norm2)
    {

        return (norm1 + norm2) / 2f;
    }

    float GenerateElevation(Vector3 vertCubePos)
    {
        float elevation = Perlin.Noise(vertCubePos * 0.01f) + 0.5f * Perlin.Noise(vertCubePos * 0.1f);
        return elevation;
    }

    Vector2[] CalculateUVs(float elevation, Vector3 vertCubePos, Vector3 pos)
    {
        Vector2[] uvArray = new Vector2[3];
        uvArray[0] = new Vector2(pos.x, pos.y);
        uvArray[1] = new Vector2(pos.z, elevation);
        Vector2 biomePos = new Vector2(vertCubePos.normalized.x/2 + 0.5f, vertCubePos.normalized.y/2 + 0.5f);
        uvArray[2] = biomePos;
        return uvArray;
    }

    Vector3 ApplyElevation(Vector3 vertCubePos, float elevation)
    {
        return vertCubePos.normalized * rootDimensions / 2 * (1 + elevation / 100);
    }

    TerrainController QuadsToTerrain(int[] quads)
    {
        Transform terrain=transform.root;
        for(int i = 0; i<quads.Length; i++)
        {
            if(terrain.childCount>0){
                terrain = terrain.GetChild(quads[i]);
            }else{
                return terrain.GetComponent<TerrainController>();
            }
        }
        return terrain.GetComponent<TerrainController>();
    }

    IEnumerator Subdivide()
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
            subTerrains[i].GetComponent<TerrainController>().Initiate(MyLoD + 1, childQuadrants, false);
            subTerrains[i].transform.SetParent(transform);
        }
        Neighbours = new TerrainController[8];

        while (queue.Count > 0)
        {
            yield return null;
        }
        FixAllSeams();
        meshRenderer.enabled = false;
        for (int i = 0; i < 4; i++)
        {
            subTerrains[i].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    IEnumerator Undivide()
    {
        TerrainController parentTerrain = transform.parent.GetComponent<TerrainController>();

        for (int i = 0; i < 4; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        GetComponent<MeshRenderer>().enabled = true;
        yield return null;
        while (queue.Count > 0)
        {
            yield return null;
        }
        if (parentTerrain != null)
        {
            parentTerrain.FixAllSeams();
        }
        else
        {
            FixRootSeams();
        }
    }

    Vector3 FaceToCubeCoords(Vector2 faceCoords, int face, Vector2 centerCoords)
    {
        Vector3 cubeCoords=Vector3.zero;
        float x = faceCoords.x + centerCoords.x;
        float y = faceCoords.y + centerCoords.y;
        if (face == 0)
        {
            cubeCoords = new Vector3(x, y, -rootDimensions / 2);
        }
        if (face == 1)
        {
            cubeCoords = new Vector3(rootDimensions / 2, y, x);
        }
        if (face == 2)
        {
            cubeCoords = new Vector3(-x, y, rootDimensions / 2);
        }
        if (face == 3)
        {
            cubeCoords = new Vector3(-rootDimensions / 2, y, -(x));
        }
        if (face == 4)
        {
            cubeCoords = new Vector3(x, rootDimensions / 2, y);
        }
        if (face == 5)
        {
            cubeCoords = new Vector3(x, -rootDimensions / 2, -(y));
        }
        return cubeCoords;
    }
    Vector3 FaceToCubeCoords(Vector2 faceCoords)
    {
        return FaceToCubeCoords(faceCoords, myFace, this.centerCoords);
    }
}
