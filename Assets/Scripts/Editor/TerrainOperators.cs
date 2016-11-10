using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class TerrainOperators : EditorWindow {

    enum Direction { Across, Down }

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
    int terrainRes = 0;
    int strength = 0;
    int stitchWidth = 20;
    float stitchWidthPercent = 25;
    Terrain[] terrains;

	Terrain terrainToMountainize;
    private float bumpiness = 0.5f;
    private float distortion = 0.02f;
    private int smoothRadius = 3;
    private float calderasC = 0.7f;
	private float mountainBumpiness = 0.2f;
    private bool terraceWithValues = false;
    private bool terraceWithInterval = true;
    private float terraceInterval = 0.1f;
    private string terraceValuesString = "0 0.25 0.5 0.75 1";
    private List<float> terraceValues = new List<float>();
	private int selectedTerrain = 0;

    // Use this for initialization
    void OnEnable () {
        SetNumberOfTerrains();
	}
	
	// Update is called once per frame
	void OnGUI () {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        GUILayout.BeginHorizontal(GUILayout.Width(300));
        across = EditorGUILayout.IntField("Number of terrains across:", across);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        down = EditorGUILayout.IntField("Number of terrains down:", down);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Apply"))
        {
            tWidth = across;
            tHeight = down;
            SetNumberOfTerrains();
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Autofill from scene"))
        {
            AutoFill();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical();
        int counter = 0;
        for (int h = 0; h < tHeight; h++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(300));
            for (int w = 0; w < tWidth; w++)
            {
                terrains[counter] = (Terrain)EditorGUILayout.ObjectField(terrains[counter], typeof(Terrain), true);
                counter++;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        bumpiness = Mathf.Clamp01(EditorGUILayout.FloatField("Bumpiness", bumpiness));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Randomize Heights (square diamond)"))
        {
            diamondSquares(bumpiness);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.BeginHorizontal(GUILayout.Width(300));
		mountainBumpiness = Mathf.Clamp01(EditorGUILayout.FloatField("Mountain bumpiness", mountainBumpiness));
		terrainToMountainize = (Terrain)EditorGUILayout.ObjectField(terrainToMountainize, typeof(Terrain), true);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(GUILayout.Width(300));
		if (GUILayout.Button("Mountainize"))
		{
			mountainize3(terrainToMountainize, mountainBumpiness);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        distortion = Mathf.Clamp01(EditorGUILayout.FloatField("Distortion", distortion));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Distort current heights"))
        {
            distort(distortion);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        smoothRadius = Mathf.Clamp(EditorGUILayout.IntField("Smooth Radius (pixels)", smoothRadius), 1, 10);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Smooth Heights"))
        {
            smooth(smoothRadius);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(150));
        terraceWithValues = EditorGUILayout.Toggle("Use values", terraceWithValues);
        GUILayout.EndHorizontal();
        if (terraceWithValues)
            terraceWithInterval = false;
        else
            terraceWithInterval = true;
        GUILayout.BeginHorizontal(GUILayout.Width(150));
        terraceWithInterval = EditorGUILayout.Toggle("Use interval", terraceWithInterval);        
        GUILayout.EndHorizontal();
        if (terraceWithInterval)
            terraceWithValues = false;
        else
            terraceWithValues = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (terraceWithValues)
        {
            terraceValuesString = EditorGUILayout.TextField(terraceValuesString);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(300));
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
            GUILayout.EndHorizontal();
        }
        if (terraceWithInterval)
        {
            terraceInterval = Mathf.Clamp(EditorGUILayout.FloatField("Terrace interval", terraceInterval), 0.01f, 1f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(300));
            if (GUILayout.Button("Terrace"))
            {
                terrace(terraceInterval);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        calderasC = Mathf.Clamp01(EditorGUILayout.FloatField("Caldera height inversion", calderasC));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(300));
        if (GUILayout.Button("Caldera inversion"))
        {
            calderas(calderasC);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal(GUILayout.Width(600));
        stitchWidthPercent = Mathf.Clamp(EditorGUILayout.FloatField("Stitch %", stitchWidthPercent), 1, 50);
        strength = Mathf.Clamp(EditorGUILayout.IntField("Strength", strength), 0, 100);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal(GUILayout.Width(600));
        if (GUILayout.Button("Stitch"))
        {
            stitchTerrains();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
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

    public void distort(float s)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heightmap = terrainData.GetHeights(0, 0, heightmapRes, heightmapRes);
            for (int y = 0; y < heightmapRes; y++)
            {
                for(int x = 0; x < heightmapRes; x++)
                {
                    heightmap[y, x] += s * (Random.value * 2 - 1);
                }
            }
            terrainData.SetHeights(0, 0, heightmap);
        }
    }
	public void mountainize(Terrain t, float s){
		
		TerrainData terrainData = t.terrainData;
		int heightmapRes = terrainData.heightmapResolution;
		float[,] heightmap = new float[heightmapRes, heightmapRes];		
		heightmap[0, 0] = s * Random.value;   
		heightmap[heightmapRes - 1, 0] = s * Random.value;
		heightmap[0, heightmapRes - 1] = s * Random.value;
		heightmap[heightmapRes - 1, heightmapRes - 1] = s * Random.value;
		divide(ref heightmap, (int)heightmapRes, s / 2, heightmapRes);
		for (int y = 0; y < heightmapRes; y++) {
			for (int x = 0; x < heightmapRes; x++) {
				heightmap [y, x] = heightmap [y, x] * 0.2f + 0.8f;
			}
		}
		terrainData.SetHeights(0, 0, heightmap);

	}

    public void diamondSquares(float s)
    {
        for (int t = 0; t < terrains.Length; t++)
        {
            TerrainData terrainData = terrains[t].terrainData;
            int heightmapRes = terrainData.heightmapResolution;
			float[,] heightmap = new float[heightmapRes, heightmapRes];
			heightmap[0, 0] = s * Random.value;   
			heightmap[heightmapRes - 1, 0] = s * Random.value;
			heightmap[0, heightmapRes - 1] = s * Random.value;
			heightmap[heightmapRes - 1, heightmapRes - 1] = s * Random.value;
            divide(ref heightmap, (int)heightmapRes, s / 2, heightmapRes);
            terrainData.SetHeights(0, 0, heightmap);
        }
    }
    public void mountainize2(Terrain t, float s)
    {
        TerrainData terrainData = t.terrainData;
        int heightmapRes = terrainData.heightmapResolution;
        float[,] heightmap = new float[heightmapRes, heightmapRes];

        heightmap[0, 0] = s * Random.value;
        heightmap[heightmapRes - 1, 0] = s * Random.value;
        heightmap[0, heightmapRes - 1] = s * Random.value;
        heightmap[heightmapRes - 1, heightmapRes - 1] = s * Random.value;

        heightmap[heightmapRes / 2, heightmapRes / 2] = 0.9f;

        heightmap[heightmapRes / 2, 0] = s * Random.value;
        heightmap[heightmapRes / 2, heightmapRes - 1] = s * Random.value;
        heightmap[0, heightmapRes / 2] = s * Random.value;
        heightmap[heightmapRes - 1, heightmapRes / 2] = s * Random.value;

        divide(ref heightmap, (int)heightmapRes / 2, s / 2, heightmapRes);
        terrainData.SetHeights(0, 0, heightmap);
    }
    public void mountainize3(Terrain t, float s)
    {
        TerrainData terrainData = t.terrainData;
        int heightmapRes = terrainData.heightmapResolution;
        float[,] heightmap = new float[heightmapRes, heightmapRes];
        float[,] heightmap2 = new float[heightmapRes, heightmapRes];

        int half = heightmapRes / 2;

        while (half > 0)
        {
            for (int i = half; i < heightmapRes; i += 2 * half)
            {
                for (int j = 0; j < heightmapRes; j++)
                {
                    if(i == heightmapRes / 2)
                    {
                        heightmap[i, j] = 1;
                    }
                    else
                    {
                        float mid = (heightmap[i + half, j] + heightmap[i - half, j]) / 2.0f;
                        heightmap[i, j] = mid + (heightmap[i + half, j] - heightmap[i - half, j]) * 2 * (0.5f - Random.value) * s;
                    }                                    
                }                
            }
            
            for (int i = half; i < heightmapRes; i += 2 * half)
            {
                for (int j = 0; j < heightmapRes; j++)
                {
                    if (i == heightmapRes / 2)
                    {
                        heightmap2[j, i] = 1;
                    }
                    else
                    {
                        float mid = (heightmap2[j, i + half] + heightmap2[j, i - half]) / 2.0f;
                            heightmap2[j, i] = mid + (heightmap2[j, i + half] - heightmap2[j, i - half]) * 2 * (0.5f - Random.value) * s;
                    }
                }
            }           
            half /= 2;
        }
        for (int i = 0; i < heightmapRes; i++)
        {
            for (int j = 0; j < heightmapRes; j++)
            {
                if (heightmap[i, j] > heightmap2[i, j])
                {
                    heightmap[i, j] = heightmap2[i, j];
                }
            }
        }
        terrainData.SetHeights(0, 0, heightmap);
        smooth(t, 3);
    }
    public void smooth(Terrain t, int radius)
    {
        TerrainData terrainData = t.terrainData;
        int heightmapRes = terrainData.heightmapResolution;
        float[,] heightmap = terrainData.GetHeights(0, 0, (int)heightmapRes, (int)heightmapRes);
        heightmap = gaussBlur(heightmap, (int)heightmapRes, (int)heightmapRes, radius);
        terrainData.SetHeights(0, 0, heightmap);
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
        float avg = (hm[y - size, x - size] + hm[y + size, x - size] + hm[y - size, x + size] + hm[y + size, x + size]) / 4.0f;
        hm[y, x] = avg + offset;
        return hm;
    }
    private float[,] diamond(ref float[,] hm, int x, int y, int size, float offset, int heightmapRes)
    {
        int c = 0;
        float avg = 0;
        if (y - size >= 0)
        {
            avg += hm[y - size, x];
            c++;
        }
        if (y + size < heightmapRes)
        {
            avg += hm[y + size, x];
            c++;
        }
        if (x - size >= 0)
        {
            avg += hm[y, x - size];
            c++;
        }
        if (x + size < heightmapRes)
        {
            avg += hm[y, x + size];
            c++;
        }
		avg /= (float)c;
        hm[y, x] = avg + offset;

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
    private void stitchTerrains()
    {
        foreach (Terrain t in terrains)
        {
            if (t == null)
            {
                Debug.Log("All terrain slots must have a terrain assigned");
                return;
            }
        }
        terrainRes = terrains[0].terrainData.heightmapWidth;
        if (terrains[0].terrainData.heightmapHeight != terrainRes)
        {
            Debug.Log("Heightmap width and height must be the same");
            return;
        }

        foreach (Terrain t in terrains)
        {
            if (t.terrainData.heightmapWidth != terrainRes || t.terrainData.heightmapHeight != terrainRes)
            {
                Debug.Log("All heightmaps must be the same resolution");
                return;
            }
        }      

        stitchWidth = (int)Mathf.Clamp(terrainRes * stitchWidthPercent, 2, (terrainRes - 1) / 2);
        var counter = 0;
        var total = tHeight * (tWidth - 1) + (tHeight - 1) * tWidth;

        if (tWidth == 1 && tHeight == 1)
        {
            blendData(terrains[0].terrainData, terrains[0].terrainData, Direction.Across, true);
            blendData(terrains[0].terrainData, terrains[0].terrainData, Direction.Down, true);
            Debug.Log("Terrain has been made repeatable with itself");
        }
        else
        {
            for (int h = 0; h < tHeight; h++)
            {
                for (int w = 0; w < tWidth - 1; w++)
                {
                    EditorUtility.DisplayProgressBar("Stitching...", "", Mathf.InverseLerp(0, total, ++counter));
                    blendData(terrains[h * tWidth + w].terrainData, terrains[h * tWidth + w + 1].terrainData, Direction.Across, false);
                }
            }
            for (int h = 0; h < tHeight - 1; h++)
            {
                for (int w = 0; w < tWidth; w++)
                {
                    EditorUtility.DisplayProgressBar("Stitching...", "", Mathf.InverseLerp(0, total, ++counter));
                    blendData(terrains[h * tWidth + w].terrainData, terrains[(h + 1) * tWidth + w].terrainData, Direction.Down, false);
                }
            }
            Debug.Log("Terrains stitched successfully");
        }

        EditorUtility.ClearProgressBar();
    }
    private void blendData(TerrainData terrain1, TerrainData terrain2, Direction thisDirection, bool singleTerrain)
    {
        float[,] heightmapData = terrain1.GetHeights(0, 0, terrainRes, terrainRes);
        float[,] heightmapData2 = terrain2.GetHeights(0, 0, terrainRes, terrainRes);
        int width = terrainRes - 1;

        if (thisDirection == Direction.Across)
        {
            for (int i = 0; i < terrainRes; i++)
            {
                float midpoint = (heightmapData[i, width] + heightmapData2[i, 0]) * .5f;
                for (int j = 1; j < stitchWidth; j++)
                {
                    float mix = Mathf.Lerp(heightmapData[i, width - j], heightmapData2[i, j], .5f);
                    if (j == 1)
                    {
                        heightmapData[i, width] = Mathf.Lerp(mix, midpoint, strength);
                        heightmapData2[i, 0] = Mathf.Lerp(mix, midpoint, strength);
                    }
                    float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(1, stitchWidth - 1, j));
                    float mixdata = Mathf.Lerp(mix, heightmapData[i, width - j], t);
                    heightmapData[i, width - j] = Mathf.Lerp(mixdata, Mathf.Lerp(midpoint, heightmapData[i, width - j], t), strength);

                    mixdata = Mathf.Lerp(mix, heightmapData2[i, j], t);
                    float blend = Mathf.Lerp(mixdata, Mathf.Lerp(midpoint, heightmapData2[i, j], t), strength);
                    if (!singleTerrain)
                    {
                        heightmapData2[i, j] = blend;
                    }
                    else
                    {
                        heightmapData[i, j] = blend;
                    }
                }
            }
            if (singleTerrain)
            {
                for (int i = 0; i < terrainRes; i++)
                {
                    heightmapData[i, 0] = heightmapData[i, width];
                }
            }
        }
        else
        {
            for (int i = 0; i < terrainRes; i++)
            {
                float midpoint = (heightmapData2[width, i] + heightmapData[0, i]) * .5f;
                for (int j = 1; j < stitchWidth; j++)
                {
                    float mix = Mathf.Lerp(heightmapData2[width - j, i], heightmapData[j, i], .5f);
                    if (j == 1)
                    {
                        heightmapData2[width, i] = Mathf.Lerp(mix, midpoint, strength);
                        heightmapData[0, i] = Mathf.Lerp(mix, midpoint, strength);
                    }
                    float t = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(1, stitchWidth - 1, j));
                    float mixdata = Mathf.Lerp(mix, heightmapData[j, i], t);
                    heightmapData[j, i] = Mathf.Lerp(mixdata, Mathf.Lerp(midpoint, heightmapData[j, i], t), strength);

                    mixdata = Mathf.Lerp(mix, heightmapData2[width - j, i], t);
                    float blend = Mathf.Lerp(mixdata, Mathf.Lerp(midpoint, heightmapData2[width - j, i], t), strength);
                    if (!singleTerrain)
                    {
                        heightmapData2[width - j, i] = blend;
                    }
                    else
                    {
                        heightmapData[width - j, i] = blend;
                    }
                }
            }
            if (singleTerrain)
            {
                for (int i = 0; i < terrainRes; i++)
                {
                    heightmapData[width, i] = heightmapData[0, i];
                }
            }
        }

        terrain1.SetHeights(0, 0, heightmapData);
        if (!singleTerrain)
        {
            terrain2.SetHeights(0, 0, heightmapData2);
        }
    }
}
