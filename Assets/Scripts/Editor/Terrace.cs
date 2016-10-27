using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class TerrainOperators : EditorWindow {

    [MenuItem("Window/Terrain Operators")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TerrainOperators));
    }

    [MenuItem("Terrain/Terrain Operators")]
    public static void CreateWindow()
    {
        window = EditorWindow.GetWindow(typeof(TerrainOperators));
        window.titleContent = new GUIContent("Terrain Operators");
        window.minSize = new Vector2(500f, 700f);
    }

    // Editor variables
    private static EditorWindow window;
    private Vector2 scrollPosition;

    // Other variables
    int across = 2;
    int down = 2;
    int tWidth = 2;
    int tHeight = 2;
    int gridPixelWidth = 121;
    int gridPixelHeight = 28;
    Texture2D lineTex;
    Terrain[] terrains;

    private float bumpiness = 0.2f;
    private int smoothRadius = 3;
    private float calderasC = 0.2f;
    private bool terraceWithValues = false;
    private bool terraceWithInterval = true;
    private float terraceInterval = 0.1f;
    private string terraceValuesString = "0 0.25 0.5 0.75 1";
    private List<float> terraceValues = new List<float>();

    // Use this for initialization
    void OnEnable () {
        SetNumberOfTerrains();
	}
	
	// Update is called once per frame
	void OnGUI () {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        across = EditorGUILayout.IntField("Number of terrains across:", across);
        down = EditorGUILayout.IntField("Number of terrains down:", down);
        if (GUILayout.Button("Apply"))
        {
            tWidth = across;
            tHeight = down;
            SetNumberOfTerrains();
        }

        if (GUILayout.Button("Autofill from scene"))
        {
            AutoFill();
        }

        GUILayout.BeginVertical();
        int counter = 0;
        for (int h = 0; h < tHeight; h++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(400));
            for (int w = 0; w < tWidth; w++)
            {
                terrains[counter] = (Terrain)EditorGUILayout.ObjectField(terrains[counter], typeof(Terrain), true);
                counter++;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        bumpiness = Mathf.Clamp01(EditorGUILayout.FloatField("Bumpiness", bumpiness));
        if(GUILayout.Button("Randomize Heights (square diamond)"))
        {
            diamondSquares(bumpiness);
        }

        smoothRadius = Mathf.Clamp(EditorGUILayout.IntField("Smooth Radius (pixels)", smoothRadius), 1, 10);
        if(GUILayout.Button("Smooth Heights"))
        {
            smooth(smoothRadius);
        }

        terraceWithValues = EditorGUILayout.Toggle("Use values", terraceWithValues);
        if (terraceWithValues)
            terraceWithInterval = false;
        else
            terraceWithInterval = true;
        terraceWithInterval = EditorGUILayout.Toggle("Use interval", terraceWithInterval);
        if (terraceWithInterval)
            terraceWithValues = false;
        else
            terraceWithValues = true;
        if (terraceWithValues)
        {
            terraceValuesString = EditorGUILayout.TextField(terraceValuesString);
            if (GUILayout.Button("Terrace"))
            {
                if (ValidatedTerraceValues())
                {
                    terrace(terraceValues);
                }
                else
                {
                    Debug.Log("Issue with entries");
                }
            }
        }
        if (terraceWithInterval)
        {
            terraceInterval = Mathf.Clamp(EditorGUILayout.FloatField("Terrace interval", terraceInterval), 0.01f, 1f);
            if (GUILayout.Button("Terrace"))
            {
                terrace(terraceInterval);
            }
        }


        GUILayout.EndScrollView();
    }
    private bool ValidatedTerraceValues()
    {
        bool pass = true;
        string og = terraceValuesString;
        terraceValues = new List<float>();
        while (terraceValuesString.Length > 0 && pass)
        {
            int spot = terraceValuesString.IndexOf(" ");

            string v;

            if (spot == -1)
            {
                v = terraceValuesString;
                terraceValuesString = "";
            }
            else
            {
                v = terraceValuesString.Substring(0, spot);
                terraceValuesString = terraceValuesString.Substring(spot + 1,
                    terraceValuesString.Length - spot - 1);
            }

            float value = 0;
            try
            {
                value = float.Parse(v);
            }
            catch (System.FormatException e)
            {
                Debug.Log(e);
                pass = false;
            }
            if (pass)
            {
                terraceValues.Add(value);
            }
        }
        terraceValuesString = og;
        return pass;
    }

    private void SetNumberOfTerrains()
    {
        terrains = new Terrain[tWidth * tHeight];
    }
    private void AutoFill()
    {
        Terrain[] sceneTerrains = GameObject.FindObjectsOfType<Terrain>();
        if (sceneTerrains.Length == 0)
        {
            Debug.Log("No terrains found");
            return;
        }

        List<float> xPositions = new List<float>();
        List<float> zPositions = new List<float>();
        Vector3 tPosition = sceneTerrains[0].transform.position;
        xPositions.Add(tPosition.x);
        zPositions.Add(tPosition.z);
        for (int i = 0; i < sceneTerrains.Length; i++)
        {
            tPosition = sceneTerrains[i].transform.position;
            if (!xPositions.Contains(tPosition.x))
            {
                xPositions.Add(tPosition.x);
            }
            if (!zPositions.Contains(tPosition.z))
            {
                zPositions.Add(tPosition.z);
            }
        }
        if (xPositions.Count * zPositions.Count != sceneTerrains.Length)
        {
            Debug.Log("Unable to autofill. Terrains should line up closely in the form of a grid.");
            return;
        }

        xPositions.Sort();
        zPositions.Sort();
        zPositions.Reverse();
        across = tWidth = xPositions.Count;
        down = tHeight = zPositions.Count;
        terrains = new Terrain[tWidth * tHeight];
        var count = 0;
        for (int z = 0; z < zPositions.Count; z++)
        {
            for (int  x = 0; x < xPositions.Count; x++)
            {
                for (int i = 0; i < sceneTerrains.Length; i++)
                {
                    tPosition = sceneTerrains[i].transform.position;
                    if (Approx(tPosition.x, xPositions[x]) && Approx(tPosition.z, zPositions[z]))
                    {
                        terrains[count++] = sceneTerrains[i];
                        break;
                    }
                }
            }
        }
    }
    private bool Approx(float pos1, float pos2) {		
		return pos1 >= pos2 - 1.0 && pos1 <= pos2 + 1.0;
	}

    public void diamondSquares(float s)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);
            heightmap[0, 0] = 0.5f;
            heightmap[0, (int)heightmapRes - 1] = 0.5f;
            heightmap[(int)heightmapRes - 1, 0] = 0.5f;
            heightmap[(int)heightmapRes - 1, (int)heightmapRes - 1] = 0.5f;
            divide(ref heightmap, (int)heightmapRes, s / 2, heightmapRes);
            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    public void smooth(int radius)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);
            heightmap = gaussBlur(heightmap, (int)heightmapRes, (int)heightmapRes, radius);
            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    public void calderas(float c)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);
            for (int i = 0; i < (int)heightmapRes; i++)
            {
                for (int j = 0; j < (int)heightmapRes; j++)
                {
                    if (heightmap[i, j] > c)
                    {
                        heightmap[i, j] = c - (heightmap[i, j] - c);
                    }
                }
            }
            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    public void terrace(List<float> heights)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);

            for (int i = 0; i < (int)heightmapRes; i++)
            {
                for (int j = 0; j < (int)heightmapRes; j++)
                {
                    int closest = 0;
                    for (int k = 1; k < heights.Count; k++)
                    {
                        if (Mathf.Abs(heightmap[i, j] - (heights[k])) < Mathf.Abs(heightmap[i, j] - (heights[closest])))
                        {
                            closest = k;
                        }
                    }
                    heightmap[i, j] = heights[closest];
                }
            }

            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    public void terrace(float interval)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);

            for (int i = 0; i < (int)heightmapRes; i++)
            {
                for (int j = 0; j < (int)heightmapRes; j++)
                {
                    int closest = 0;
                    for (int k = 1; k < 1.0f / interval; k++)
                    {
                        if (Mathf.Abs(heightmap[i, j] - (k * interval)) < Mathf.Abs(heightmap[i, j] - (closest * interval)))
                        {
                            closest = k;
                        }
                    }
                    heightmap[i, j] = closest * interval;
                }
            }

            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    private void divide(ref float[,] hm, int size, float s, int heightmapRes)
    {
        int x, y, half = size / 2;
        float scale = (size / (float)heightmapRes) * s;
        if (half < 1) return;

        for (y = half; y < heightmapRes - 1; y += size)
        {
            for (x = half; x < heightmapRes - 1; x += size)
            {
                square(ref hm, x, y, half, scale * (Random.value * 2 - 1), heightmapRes);
            }
        }
        for (y = 0; y < heightmapRes; y += half)
        {
            for (x = (y + half) % size; x < heightmapRes; x += size)
            {
                diamond(ref hm, x, y, half, scale * (Random.value * 2 - 1), heightmapRes);
            }
        }
        divide(ref hm, half, s, heightmapRes);
    }
    private float[,] square(ref float[,] hm, int x, int y, int size, float offset, int heightmapRes)
    {
        float avg = (hm[x - size, y - size] + hm[x + size, y - size] + hm[x - size, y + size] + hm[x + size, y + size]) / 4.0f;
        hm[x, y] = avg + offset;

        return hm;
    }
    private float[,] diamond(ref float[,] hm, int x, int y, int size, float offset, int heightmapRes)
    {
        int c = 0;
        float avg = 0;
        if (y - size >= 0)
        {
            avg += hm[x, y - size];
            c++;
        }
        if (y + size < heightmapRes)
        {
            avg += hm[x, y + size];
            c++;
        }
        if (x - size >= 0)
        {
            avg += hm[x - size, y];
            c++;
        }
        if (x + size < heightmapRes)
        {
            avg += hm[x + size, y];
            c++;
        }
        avg /= 4.0f;
        hm[x, y] = avg + offset;

        return hm;
    }
    private int[] boxesForGauss(float sigma, int n)
    {
        float wIdeal = Mathf.Sqrt((12 * sigma * sigma / n) + 1);
        float w1 = Mathf.Floor(wIdeal);
        if (w1 % 2 == 0) w1--;
        float wu = w1 + 2;

        float mIdeal = (12 * sigma * sigma - n * w1 * w1 - 4 * n * w1 - 3 * n) / (-4 * w1 - 4);
        float m = Mathf.Round(mIdeal);

        int[] sizes = new int[n];
        for (int i = 0; i < n; i++)
        {
            sizes[i] = (int)(i < m ? w1 : wu);
        }
        return sizes;
    }
    private float[,] gaussBlur(float[,] scl, int w, int h, int r)
    {
        int[] boxes = boxesForGauss(r, 3);
        float[,] pass1 = boxBlur(scl, w, h, (boxes[0] - 1) / 2);
        float[,] pass2 = boxBlur(pass1, w, h, (boxes[1] - 1) / 2);
        float[,] finalPass = boxBlur(pass2, w, h, (boxes[2] - 1) / 2);
        return finalPass;
    }
    private float[,] boxBlur(float[,] scl, int w, int h, int r)
    {
        float[,] blur = new float[w, h];
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                float val = 0.0f;
                for (int iy = i - r; iy < i + r + 1; iy++)
                {
                    for (int ix = j - r; ix < j + r + 1; ix++)
                    {
                        int x = Mathf.Min(w - 1, Mathf.Max(0, ix));
                        int y = Mathf.Min(h - 1, Mathf.Max(0, iy));
                        val += scl[y, x];
                    }
                }
                blur[i, j] = val / ((r + r + 1) * (r + r + 1));
            }
        }
        return blur;
    }
}
