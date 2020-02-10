using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetController : MonoBehaviour
{
    public readonly int rootDimensions = 1000;
    private readonly Vector3[] dirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left, Vector3.up, Vector3.down };

    public GameObject terrainPrefab;
    private GameObject[] terrainRoots = new GameObject[6];
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 6; i++) {
            terrainRoots[0] = GameObject.Instantiate(terrainPrefab, transform.position, transform.rotation, transform);
            terrainRoots[0].GetComponent<TerrainController>().Initiate(0,i,new int[] { 0 });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
