using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MazeGenerator : EditorWindow
{
    [MenuItem("Window/Maze Generator")]
    public static void ShowWindow()
    {
		GetWindow(typeof(MazeGenerator));
    }

    private bool generateTiles = false;
    private bool useTiles = false;
    private bool addObject = false;

	const int EMPTY = 0;
	const int THROUGH_HORIZONTAL = 1;
	const int THROUGH_VERTICAL = 2;
	const int CROSS = 3;
	const int TOP_LEFT = 4;
	const int TOP_RIGHT = 5;
	const int BOT_LEFT = 6;
	const int BOT_RIGHT = 7;
	const int LEFT_T = 8;
	const int TOP_T = 9;
	const int RIGHT_T = 10;
	const int BOT_T = 11;
	const int LEFT = 12;
	const int RIGHT = 13;
	const int TOP = 14;
	const int BOT = 15;

    private int mazeWidth = 4;
    private int mazeHeight = 4;
	private int[] VEBP = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
	private int[] HEBP = new int[] { 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
	private string VEBPString = "111111111111";
	private string HEBPString = "100000011000";
	private Vector2 start = new Vector2(0, 0);
    private Vector2 end = new Vector2(0, 0);        

    private static EditorWindow window;

    private float tileWidth = 10;
    private float tileLength = 10;
    private float height = 0;
    private float wallHeight = 5;
    private float wallScale = 1;

    private int heightmapResolution = 129;
    private int detailResolution = 128;
    private int detailResolutionPerPatch = 8;
    private int baseTextureReolution = 1024;

    private bool isTextured = false;
    private bool texturesBasedOnHeightMap = false;
    private bool smoothHeightMap = false;
	private bool useTrees = false;

    private int gaussBlurRadius = 3;
	private int gaussBlurPass = 1;

    private Texture2D[] texturesList = new Texture2D[2];
    private int[] texturesHeights = new int[2];

	private string path = string.Empty;

	private GameObject tree;
	private int treeTextureNumber = 0;
	private float minDistTreeFromHeight = 0.2f;
	private float percentToMakeTree = 0.2f;

	private Vector2 scrollPosition;
    
    [MenuItem("Terrain/Maze Generator")]
    public static void CreateWindow()
    {
		window = EditorWindow.GetWindow(typeof(MazeGenerator));
        window.titleContent = new GUIContent("Maze Generator");
        window.minSize = new Vector2(500f, 700f);
    }

    private void OnGUI()
    {
		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
        EditorGUILayout.BeginHorizontal();
        generateTiles = EditorGUILayout.Toggle("Generate tiles", generateTiles);
        if (generateTiles) useTiles = false;
        useTiles = EditorGUILayout.Toggle("Use created tiles", useTiles);
        if (useTiles) generateTiles = false;
        EditorGUILayout.EndHorizontal();
        if (generateTiles)
        {
            mazeWidth = EditorGUILayout.IntField("Maze width", mazeWidth);
            if (mazeWidth < 2)
            {
                mazeWidth = 2;
            }
            mazeHeight = EditorGUILayout.IntField("Maze height", mazeHeight);
            if (mazeHeight < 2)
            {
                mazeHeight = 2;
            }
            VEBPString = EditorGUILayout.TextField("VEBP", VEBPString);
            HEBPString = EditorGUILayout.TextField("HEBP", HEBPString);

            if (GUILayout.Button("Randomize Bit Vectors"))
            {
                VEBPString = "";
                HEBPString = "";
                for (int i = 0; i < mazeWidth * (mazeHeight - 1); i++)
                {
                    if (Random.value > 0.5)
                    {
                        VEBPString = VEBPString + "0";
                    }
                    else
                    {
                        VEBPString = VEBPString + "1";
                    }
                }
                for (int i = 0; i < (mazeWidth - 1) * mazeHeight; i++)
                {
                    if (Random.value > 0.5)
                    {
                        HEBPString = HEBPString + "0";
                    }
                    else
                    {
                        HEBPString = HEBPString + "1";
                    }
                }
            }
            /*
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            start = EditorGUILayout.Vector2Field("Start position", start);
            EditorGUILayout.EndHorizontal();
            start.x = Mathf.Clamp(start.x, 0, mazeWidth * 2);
            start.y = Mathf.Clamp(start.y, 0, mazeHeight * 2);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            end = EditorGUILayout.Vector2Field("End position", end);
            EditorGUILayout.EndHorizontal();
            end.x = Mathf.Clamp(end.x, 0, mazeWidth * 2);
            end.y = Mathf.Clamp(end.y, 0, mazeHeight * 2);
            */
            tileWidth = EditorGUILayout.FloatField("Terrain Tile Width", tileWidth);
            tileLength = EditorGUILayout.FloatField("Terrain Tile Length", tileLength);
            height = EditorGUILayout.FloatField("Terrain Path Height", height);
            wallHeight = EditorGUILayout.FloatField("Terrain Wall Height", wallHeight);
            /*
            wallScale = EditorGUILayout.FloatField("Wall scale", wallScale);
            wallScale = Mathf.Clamp(wallScale, 0, 2);
            */

            EditorGUILayout.Space();

            if (isTextured)
            {
                baseTextureReolution = EditorGUILayout.IntField("Base Texture Reolution", baseTextureReolution);
                baseTextureReolution = Mathf.ClosestPowerOfTwo(baseTextureReolution);
                baseTextureReolution = Mathf.Clamp(baseTextureReolution, 16, 2048);
            }
            if (texturesBasedOnHeightMap)
            {
                heightmapResolution = EditorGUILayout.IntField("Heightmap Resolution", heightmapResolution);
                heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution) + 1;
                heightmapResolution = Mathf.Clamp(heightmapResolution, 33, 4097);

                detailResolution = EditorGUILayout.IntField("Detail Resolution", detailResolution);
                detailResolution = Mathf.ClosestPowerOfTwo(detailResolution);
                detailResolution = Mathf.Clamp(detailResolution, 0, 4096);

                detailResolutionPerPatch = EditorGUILayout.IntField("Detail Resolution Per Patch", detailResolutionPerPatch);
                detailResolutionPerPatch = Mathf.ClosestPowerOfTwo(detailResolutionPerPatch);
                detailResolutionPerPatch = Mathf.Clamp(detailResolutionPerPatch, 8, 128);
            }            
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            isTextured = EditorGUILayout.Toggle("Textured", isTextured);

            if (isTextured)
            {
                if (!texturesBasedOnHeightMap)
                {
                    Texture2D[] texturesListTemp = new Texture2D[1];
                    texturesListTemp[0] = texturesList[0];
                    texturesList = texturesListTemp;
                    texturesList[0] = (Texture2D)EditorGUILayout.ObjectField("Texture", texturesList[0], typeof(Texture), true);
                }
                texturesBasedOnHeightMap = EditorGUILayout.Toggle("Texturing based on heightmap", texturesBasedOnHeightMap);

                if (texturesBasedOnHeightMap)
                {
                    for (int i = 0; i < texturesList.Length; i++)
                    {
                        texturesList[i] = (Texture2D)EditorGUILayout.ObjectField("Texture " + i, texturesList[i], typeof(Texture), true);
                    }
                    for (int i = 0; i < texturesList.Length; i++)
                    {
                        texturesHeights[i] = EditorGUILayout.IntField("Texture " + i + " height", texturesHeights[i]);
                    }

                    EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Add texture"))
                    {
                        ArrayUtility.Add(ref texturesList, null);
                        ArrayUtility.Add(ref texturesHeights, 0);
                    }
                    if (GUILayout.Button("Remove texture") && texturesList.Length > 2)
                    {
                        ArrayUtility.RemoveAt(ref texturesList, texturesList.Length - 1);
                        ArrayUtility.RemoveAt(ref texturesHeights, texturesHeights.Length - 1);
                    }

                    EditorGUILayout.EndHorizontal();

                    useTrees = EditorGUILayout.Toggle("Use trees", useTrees);
                    if (useTrees)
                    {
                        tree = (GameObject)EditorGUILayout.ObjectField("Tree", tree, typeof(GameObject), true);
                        treeTextureNumber = EditorGUILayout.IntField("Texture number to put trees on", treeTextureNumber);
                        treeTextureNumber = Mathf.Clamp(treeTextureNumber, 0, texturesList.Length - 1);
                        minDistTreeFromHeight = EditorGUILayout.FloatField("Minimum distance from surrounding textures", minDistTreeFromHeight);
                        minDistTreeFromHeight = Mathf.Clamp01(minDistTreeFromHeight);
                        percentToMakeTree = EditorGUILayout.FloatField("Probability to create a tree", percentToMakeTree);
                        percentToMakeTree = Mathf.Clamp01(percentToMakeTree);
                    }
                }
            }
            else
            {

            }
            smoothHeightMap = EditorGUILayout.Toggle("Smooth heightmap", smoothHeightMap);

            if (smoothHeightMap)
            {
                gaussBlurRadius = EditorGUILayout.IntField("Radius of smooth", gaussBlurRadius);
                gaussBlurRadius = Mathf.Clamp(gaussBlurRadius, 0, heightmapResolution);
                gaussBlurPass = EditorGUILayout.IntField("Smooth passes", gaussBlurPass);
                gaussBlurPass = Mathf.Clamp(gaussBlurPass, 0, 10);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Path were to save TerrainData:");
            path = EditorGUILayout.TextField("Assets/", path);

            if (GUILayout.Button("Create"))
            {
                if (ValidatePath() && ValidateTextures() && ValidateBitVectors() && ValidateTrees())
                {
                    CreateTerrain();
                }
                path = string.Empty;
            }
            if (GUILayout.Button("Debug"))
            {
                Debugger();
            }
        }
        else if (useTiles)
        {

        }
		
		GUILayout.EndScrollView();
    }

    private bool ValidatePath()
    {
        if (path == string.Empty) path = "MazeGenerator/TerrainData/";

        string pathToCheck = Application.dataPath + "/" + path;
        if (Directory.Exists(pathToCheck) == false)
        {
            Directory.CreateDirectory(pathToCheck);
        }
		return true;
    }

    private bool ValidateTextures()
    {
        bool x = true;        
        if (isTextured)
        {            
            if (texturesList[0] == null)
            {
                x = false;
				Debug.Log ("Must have at least 1 texture");
            }            
        }
		else if (isTextured && texturesBasedOnHeightMap)
        {
            for(int i = 0; i < texturesList.Length; i++)
            {
                if(texturesList[i] == null)
                {
                    x= false;
					Debug.Log ("Missing texture number " + i);
                }
            }
        }
        return x;
    }
	private bool ValidateTrees(){
		bool x = true;
		if (useTrees) {
			if (tree == null) {
				x = false;
			} else if (treeTextureNumber < 0 || treeTextureNumber > texturesList.Length - 1) {
				x = false;
			}
		}
		return x;
	}
	private bool ValidateBitVectors(){
		bool x = true;
		if (VEBPString.Length != mazeWidth * (mazeHeight - 1)) {
			Debug.Log ("Bit vector doesn't match expected length based on maze width and height");
			x = false;
		} else if (HEBPString.Length != mazeHeight * (mazeWidth - 1)) {
			Debug.Log ("Bit vector doesn't match expected length based on maze width and height");
			x = false;
		}
		else {
			VEBP = new int[mazeWidth * (mazeHeight - 1)];
			HEBP = new int[mazeHeight * (mazeWidth - 1)];
			for (int i = 0; i < VEBPString.Length; i++) {				
				string a, b;
				a = VEBPString.Substring (i, 1);
				b = HEBPString.Substring (i, 1);
				if ((a != "0" && a != "1") || (b != "0" && b != "1")) {
					Debug.Log ("All entries in the bit vector should be 1 or 0");
					x = false;
				} else {
					VEBP [i] = int.Parse (a);
					HEBP [i] = int.Parse (b);
				}
			}
		}
		return x;
	}

    float[,] createMapFromEBP()
    {
		int numRows = (VEBP.Length / (mazeWidth - 1));
		int numCols = (HEBP.Length / (mazeHeight - 1));

        float[,] tempMap = new float[numRows * 2 + 1, numCols * 2 + 1];

        for (int i = 0; i < VEBP.Length; i++)
        {
            int r = i / numRows;
            int c = i % numCols;
            if (VEBP[i] == 1)
            {
                for (int j = r * 2 + 1; j <= (r + 1) * 2 + 1; j++)
                {
                    tempMap[j, c * 2 + 1] = VEBP[i];
                }
            }
        }
        for (int i = 0; i < HEBP.Length; i++)
        {
            int r = i % numRows;
            int c = i / numCols;
            if (HEBP[i] == 1)
            {
                for (int j = c * 2 + 1; j <= (c + 1) * 2 + 1; j++)
                {
                    tempMap[(r * 2 + 1), j] = HEBP[i];
                }
            }
        }
        tempMap[(int)start.y, (int)start.x] = 1;
        tempMap[(int)end.y, (int)end.x] = 1;
        return tempMap;
    }
	private int[,] createTilingFromEBP(){
		int[,] tilingMap = new int[mazeWidth, mazeHeight];
		for (int i = 0; i < mazeHeight; i++) {
			for (int j = 0; j < mazeWidth; j++) {
				int horizontalLeft = HEBP [mazeHeight * Mathf.Max(j - 1, 0) + i];
				int horizontalRight = HEBP [Mathf.Min(mazeHeight * j + i, HEBP.Length - 1)];
				int verticalTop = VEBP [ mazeWidth * Mathf.Max(i - 1, 0) + j];
				int verticalBot = VEBP [ Mathf.Min(mazeWidth * i + j, VEBP.Length - 1)];

				if (j == 0 && i == 0) {
					horizontalLeft = 0;
					verticalTop = 0;
					//Debug.Log ("top left");
				} else if (j == mazeWidth - 1 && i == mazeHeight - 1) {
					horizontalRight = 0;
					verticalBot = 0;
					//Debug.Log ("bot right");
				} else if (j == mazeWidth - 1 && i == 0) {
					verticalTop = 0;
					horizontalRight = 0;
					//Debug.Log ("top right");
				} else if (i == mazeHeight - 1 && j == 0) {
					verticalBot = 0;
					horizontalLeft = 0;
					//Debug.Log ("bot left");
				}else if (j == 0) {
					horizontalLeft = 0;
					//Debug.Log ("left");
				} else if (i == 0) {
					verticalTop = 0;
					//Debug.Log ("top");
				} else if (j == mazeWidth - 1) {
					horizontalRight = 0;
					//Debug.Log ("right");
				} else if (i == mazeHeight - 1) {
					verticalBot = 0;
					//Debug.Log ("bot");
				} else {
					//Debug.Log ("center");
				}

				if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 0) {
					tilingMap [i, j] = EMPTY;
				} else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 1) {
					tilingMap [i, j] = LEFT;
				} else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 0) {
					tilingMap [i, j] = RIGHT;
				} else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 1) {
					tilingMap [i, j] = THROUGH_HORIZONTAL;
				} else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 0) {
					tilingMap [i, j] = TOP;
				} else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 1) {
					tilingMap [i, j] = TOP_LEFT;
				} else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 0) {
					tilingMap [i, j] = TOP_RIGHT;
				} else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 1) {
					tilingMap [i, j] = TOP_T;
				} else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 0) {
					tilingMap [i, j] = BOT;
				} else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 1) {
					tilingMap [i, j] = BOT_LEFT;
				} else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 0) {
					tilingMap [i, j] = BOT_RIGHT;
				} else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 1) {
					tilingMap [i, j] = BOT_T;
				} else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 0) {
					tilingMap [i, j] = THROUGH_VERTICAL;
				} else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 1) {
					tilingMap [i, j] = LEFT_T;
				} else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 0) {
					tilingMap [i, j] = RIGHT_T;
				} else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 1) {
					tilingMap [i, j] = CROSS;
				}
				Debug.Log (tilingMap [i, j]);
			}
		}
		return tilingMap;
	}

    private float[,] createHeightMap(float[,] map)
    {
		int arrayWidthSpotsPerTile = heightmapResolution / (mazeWidth * 2 + 1);
		int arrayHeightSpotsPerTile = heightmapResolution / (mazeHeight * 2 + 1);
        float[,] heightMap = new float[heightmapResolution, heightmapResolution];

		for (int i = 0; i < mazeHeight * 2 + 1; i++)
        {            
			for (int j = 0; j < mazeWidth * 2 + 1; j++)
            {
                for (int k = 0; k < arrayWidthSpotsPerTile; k++)
                {                    
                    for (int m = 0; m < arrayHeightSpotsPerTile; m++)
                    {
                        if(map[i, j] == 0)
                        {
                            heightMap[j * arrayWidthSpotsPerTile + m, i * arrayHeightSpotsPerTile + k] = wallHeight / Mathf.Max(new float[] { wallHeight, height });
                        }
                        else
                        {
                            heightMap[j * arrayWidthSpotsPerTile + m, i * arrayHeightSpotsPerTile + k] = height / Mathf.Max(new float[] { wallHeight, height });
                        }                  
                    }                    
                }
            }            
        }
        return heightMap;
    }	

    int[] boxesForGauss(float sigma, int n)
    {
        float wIdeal = Mathf.Sqrt((12 * sigma * sigma / n) + 1);
        float w1 = Mathf.Floor(wIdeal);
        if (w1 % 2 == 0) w1--;
        float wu = w1 + 2;

        float mIdeal = (12 * sigma * sigma - n * w1 * w1 - 4 * n * w1 - 3 * n) / (-4 * w1 - 4);
        float m = Mathf.Round(mIdeal);

        int[] sizes = new int[n];
        for(int i = 0; i < n; i++)
        {
            sizes[i] = (int)(i < m ? w1 : wu);
        }
        return sizes;
    }
    float[,] gaussBlur(float[,] scl, int w, int h, int r)
    {
        int[] boxes = boxesForGauss(r, 3);
        float[,] pass1 = boxBlur(scl, w, h, (boxes[0] - 1) / 2);
        float[,] pass2 = boxBlur(pass1, w, h, (boxes[1] - 1) / 2);
        float[,] finalPass = boxBlur(pass2, w, h, (boxes[2] - 1) / 2);
        return finalPass;
    }
    float[,] boxBlur(float[,] scl, int w, int h, int r)
    {
        float[,] blur = new float[w, h];
        for (int i = 0; i < h; i++) { 
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
	private void Debugger(){
		int[,] tileMap = createTilingFromEBP ();			
	}
    private void CreateTerrain()
    {
        TerrainData terrainData = new TerrainData();        
        string name = "Terrain";

        terrainData.baseMapResolution = baseTextureReolution;
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = heightmapResolution;
        terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch); 


        float[,] EBPMap = createMapFromEBP();
        float[,] heightMap = createHeightMap(EBPMap);        
        
        if (smoothHeightMap)
        {
			for (int i = 0; i < gaussBlurPass; i++) {
				heightMap = gaussBlur (heightMap, heightmapResolution, heightmapResolution, gaussBlurRadius);
			}
        }

        terrainData.SetHeights(0, 0, heightMap);
        AssetDatabase.CreateAsset(terrainData, "Assets/" + path + name + ".asset");
        AssetDatabase.SaveAssets();

		SplatPrototype[] textures = new SplatPrototype[texturesList.Length];
        
        for(int i = 0; i < textures.Length; i++)
        {
            textures[i] = new SplatPrototype();
            textures[i].texture = texturesList[i];
        }

        terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + path + name + ".asset", typeof(TerrainData));
		if (useTrees) {
			TreePrototype[] trees = new TreePrototype[1];
			trees [0] = new TreePrototype ();
			trees [0].prefab = tree;
			trees [0].bendFactor = 0;
			terrainData.treePrototypes = trees;
		}

        if (isTextured)
        {
            terrainData.splatPrototypes = textures;

            if (texturesBasedOnHeightMap)
            {
                float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
                float maxHeight = texturesHeights[0];
                float minHeight = texturesHeights[0];
                int closestLowestIndexRef = 0;
                int closestHighestIndexRef = 0;
                for (int i = 0; i < textures.Length; i++)
                {
                    if(texturesHeights[i] > maxHeight)
                    {
                        maxHeight = texturesHeights[i];
                        closestHighestIndexRef = i;
                    }
                    else if(texturesHeights[i] < minHeight)
                    {
                        minHeight = texturesHeights[i];
                        closestLowestIndexRef = i;
                    }
                }

                for (int y = 0; y < terrainData.alphamapHeight; y++)
                {
                    for (int x = 0; x < terrainData.alphamapWidth; x++)
                    {
                        float currentHeight = terrainData.GetHeight(x, y);
                        int closestLowestIndex = closestLowestIndexRef;
                        int closestHighestIndex = closestHighestIndexRef;

                        for(int i = 0; i < textures.Length; i++)
                        {
                            if(currentHeight - texturesHeights[i] / maxHeight > 0 && texturesHeights[i] > texturesHeights[closestLowestIndex])
                            {
                                closestLowestIndex = i;
                            }
                            else if(texturesHeights[i] / maxHeight - currentHeight > 0 && texturesHeights[i] < texturesHeights[closestHighestIndex])
                            {
                                closestHighestIndex = i;
                            }
                        }
						Vector2 splat = Vector2.Lerp(new Vector2(0, 1), new Vector2(1, 0), (currentHeight * maxHeight - texturesHeights[closestLowestIndex]) / (texturesHeights[closestHighestIndex] - texturesHeights[closestLowestIndex]));

						if (closestLowestIndex == closestHighestIndex) {
							splat = new Vector2 (1, 1);
						}
                        splatmapData[y, x, closestLowestIndex] = splat.y;
                        splatmapData[y, x, closestHighestIndex] = splat.x; 
						if (useTrees) {
							if (closestLowestIndex == treeTextureNumber && minDistTreeFromHeight < splat.y && percentToMakeTree > Random.value) {								
								TreeInstance t = new TreeInstance ();
								t.prototypeIndex = 0;
								t.heightScale = 0.5f;
								t.widthScale = 0.5f;
								t.color = new Color32 (255, 255, 255, 255);
								t.lightmapColor = new Color32(255, 255, 255, 255);
								t.position = new Vector3 ((float)x / (float)terrainData.alphamapWidth, currentHeight, (float)y / (float)terrainData.alphamapHeight);
								TreeInstance[] tempTrees = terrainData.treeInstances;
								ArrayUtility.Add (ref tempTrees, t);
								terrainData.treeInstances = tempTrees;

							} else if (closestHighestIndex == treeTextureNumber && minDistTreeFromHeight < splat.x && percentToMakeTree > Random.value) {
								TreeInstance t = new TreeInstance ();
								t.prototypeIndex = 0;
								t.heightScale = 0.5f;
								t.widthScale = 0.5f;
								t.color = new Color32 (255, 255, 255, 255);
								t.lightmapColor = new Color32(255, 255, 255, 255);
								t.position = new Vector3 ((float)x / (float)terrainData.alphamapWidth, currentHeight, (float)y / (float)terrainData.alphamapHeight);
								TreeInstance[] tempTrees = terrainData.treeInstances;
								ArrayUtility.Add (ref tempTrees, t);
								terrainData.treeInstances = tempTrees;
							}
						}
                    }
                }
                terrainData.SetAlphamaps(0, 0, splatmapData);
            }
        }
        terrainData.size = new Vector3(tileWidth * mazeWidth, Mathf.Max(new float[] { wallHeight, height, 1 }), tileLength * mazeHeight);

		terrainData.name = name;
		GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);

        terrain.name = name;
        terrain.transform.position = new Vector3(0, 0, 0);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
	/*
	void createStraight()
	{
		TerrainData terrainData = new TerrainData();
		string name = "Straight";
		terrainData.baseMapResolution = baseTextureResolution;
		terrainData.heightmapResolution = heightmapResolution;
		terrainData.alphamapResolution = heightmapResolution;
		terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);

		// Save asset to database to avoid a bug
		if (!Directory.Exists(Application.dataPath + saveLocation + "/TerrainData"))
		{
			Directory.CreateDirectory(Application.dataPath + saveLocation + "/TerrainData");
		}
		AssetDatabase.CreateAsset(terrainData, "Assets/" + saveLocation + "TerrainData/" + name + ".asset");
		AssetDatabase.SaveAssets();
		terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + saveLocation + "TerrainData/Straight.asset", typeof(TerrainData));

		// Texture the terrain 
		if (isTextured)
		{
			SplatPrototype[] splat = new SplatPrototype[texturesList.Length];
			for (int i = 0; i < texturesList.Length && texturesList[i] != null; i++)
			{
				splat[i] = new SplatPrototype();
				splat[i].texture = textureList[i];
			}
			terrainData.splatPrototypes = splat;

			float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

			Vector2 startPoint = new Vector2(0, Random.Range(pathWidth, tileHeight - pathWidth));
			Vector2 endPoint = new Vector2(tileWidth, Random.Range(pathWidth, tileHeight - pathWidth));
			Vector2 currentPoint = startPoint;

			int numPathTextures = 0;
			int numOtherTextures = 0;
			for(int i = 0; i < textures.Length; i++)
			{
				if (textureOnPath[i]) numPathTextures++;   
				else numOtherTextures++;
			}
			for (int y = 0; y < terrainData.alphamapHeight; y++)
			{
				for(int x = 0; x < terrainData.alphamapWidth; x++)
				{
					for(int i = 0; i < textures.Length; i++)
					{
						if (!textureOnPath[i]) splatmapData[y, x, i] = 1.0f / numOtherTextures;
						else splatmapData[y, x, i] = 0.0f;
					}
				}
			}

			bool finished = false;
			HashSet<Vector2> visitedPoints = new HashSet<Vector2>();
			HashSet<Vector2> justVisitedPoints = new HashSet<Vector2>();
			while (!finished)
			{
				float r_x = currentPoint.x / tileWidth;
				float r_y = currentPoint.y / tileHeight;
				float r_path_x = pathWidth / tileWidth;
				float r_path_y = pathWidth / tileHeight;
				for (int y = (int)Mathf.Max(new float[] { terrainData.alphamapHeight * ((r_y) - (r_path_y / 2)), 0 });
					y < (int)Mathf.Min(new float[] { terrainData.alphamapHeight * ((r_y) + (r_path_y / 2)), terrainData.alphamapHeight });
					y++)
				{
					for (int x = (int)Mathf.Max(new float[] { terrainData.alphamapWidth * ((r_x) - (r_path_x / 2)), 0 });
						x < (int)Mathf.Min(new float[] { terrainData.alphamapWidth * ((r_x) + (r_path_x / 2)), terrainData.alphamapWidth });
						x++)
					{
						justVisitedPoints.Add(new Vector2(x, y));
						Vector2 spot = new Vector2(tileWidth * ((float)x / terrainData.alphamapWidth), tileHeight * ((float)y / terrainData.alphamapHeight));
						float distance = Vector2.Distance(currentPoint, spot);
						float textureMergeStart = 3.0f / 8.0f;
						float textureMergeEnd = 1.0f / 2.0f;

						if (distance < pathWidth * textureMergeStart)
						{
							for (int i = 0; i < textures.Length; i++)
							{
								if (textureOnPath[i]) splatmapData[y, x, i] = 1.0f / numPathTextures;
								else splatmapData[y, x, i] = 0;
							}
						}
						else if (distance < pathWidth * textureMergeEnd && distance >= pathWidth * textureMergeStart && !visitedPoints.Contains(new Vector2(x, y))) 
						{
							float normalizedDistance = (distance - pathWidth * textureMergeStart) / (textureMergeEnd - textureMergeStart);

							for (int i = 0; i < textures.Length; i++)
							{
								if (textureOnPath[i])
								{
									splatmapData[y, x, i] = (1 - normalizedDistance) / numPathTextures;
								}
								else splatmapData[y, x, i] = (normalizedDistance) / numOtherTextures;
							}
						} 
					}
				}
				if(currentPoint == endPoint)
				{
					finished = true;
				}
				else if(Vector2.Distance(endPoint, currentPoint) < pathWidth / 2)
				{
					currentPoint = endPoint;
				}
				else
				{
					float idealSlope = (endPoint.y - currentPoint.y) / (endPoint.x - currentPoint.x);
					float minSlope = idealSlope - 5;
					float maxSlope = idealSlope + 5;


					Vector2 possiblePoint = Vector2.MoveTowards(currentPoint, 
						new Vector2(currentPoint.x + 1, currentPoint.y + Random.Range(minSlope, maxSlope)),
						pathWidth / 2);
					int trials = 0;

					while(trials < 10000 && (possiblePoint.x < pathWidth / 4 || possiblePoint.x > tileWidth - pathWidth / 4 
						|| possiblePoint.y < pathWidth / 4 || possiblePoint.y - pathWidth / 4 > tileHeight || visitedPoints.Contains(possiblePoint)))
					{
						possiblePoint = Vector2.MoveTowards(currentPoint,
							new Vector2(currentPoint.x + 1, currentPoint.y + Random.Range(minSlope, maxSlope)),
							pathWidth / 2);
					}

					if(trials >= 10000)
					{
						Debug.Log("too many");
					}
					currentPoint = possiblePoint;
					foreach(Vector2 x in justVisitedPoints)
					{
						visitedPoints.Add(x);
					}
				}
			}

			terrainData.SetAlphamaps(0, 0, splatmapData);
		}

		// Create the heightmap 
		if (heightmapBasedPath)
		{ 
			float[,] heightMap = new float[heightmapResolution, heightmapResolution];


			terrainData.SetHeights(0, 0, heightMap);
		}



		if (objectsBasedPath)
		{
			TreePrototype[] trees = new TreePrototype[1];
			int treeCount = 0;
			for (int o = 0; o < objectsOnPath.Length && objectsOnPath[o] != null; o++)
			{
				if (objectOnPathIsTree[o])
				{
					ArrayUtility.Add(ref trees, new TreePrototype());
					trees[treeCount].prefab = objectsOnPath[o];
					trees[treeCount].bendFactor = 0;                    
					treeCount++;
				}
			}
			terrainData.treePrototypes = trees;
		}



		terrainData.size = new Vector3(tileWidth, Mathf.Max(new float[] { otherHeight, pathHeight, 1 }), tileHeight);
		terrainData.name = name;
		GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);

		terrain.name = name;
		terrain.transform.position = new Vector3(0, 0, 0);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	*/
}