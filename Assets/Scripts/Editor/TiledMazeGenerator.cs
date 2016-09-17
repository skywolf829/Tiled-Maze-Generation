using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class TiledMazeGenerator : EditorWindow
{
    [MenuItem("Window/Tiled Maze Generator")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TiledMazeGenerator));
    }

    [MenuItem("Terrain/Tiled Maze Generator")]
    public static void CreateWindow()
    {
        window = EditorWindow.GetWindow(typeof(TiledMazeGenerator));
        window.titleContent = new GUIContent("Tiled Maze Generator");
        window.minSize = new Vector2(500f, 700f);
    } 
     
    // Editor variables
    private static EditorWindow window;
    private Vector2 scrollPosition;

    // Variables related to the generated tiles
    private const int EMPTY = 0;
    private const int THROUGH_HORIZONTAL = 1;
    private const int THROUGH_VERTICAL = 2;
    private const int CROSS = 3;
    private const int TOP_LEFT = 4;
    private const int TOP_RIGHT = 5;
    private const int BOT_LEFT = 6;
    private const int BOT_RIGHT = 7;
    private const int LEFT_T = 8;
    private const int TOP_T = 9;
    private const int RIGHT_T = 10;
    private const int BOT_T = 11;
    private const int LEFT = 12;
    private const int RIGHT = 13;
    private const int TOP = 14;
    private const int BOT = 15;

    private bool useCreatedTiles = false;
    private bool generateTiles = true;
    private bool textureTiles = true;
    private bool heightmapBasedPath = false;
    private bool objectsBasedPath = false;
    private bool heightmapSmoothing = false;
    private bool texturesBasedOnHeightmap = false;
    private bool texturesBasedOnPath = true;

    private float tileWidth = 10, tileHeight = 10, pathWidth = 2;
    private float pathHeight = 0, otherHeight = 5;
    private int heightmapResolution = 129, detailResolution = 128, detailResolutionPerPatch = 8, baseTextureResolution = 128, perTileDetail = 5;

    private int gaussBlurRadius = 3, gaussBlurPass = 1;

    private Texture2D[] textures = new Texture2D[1];
    private float[] textureProportion = new float[1];
    private float[] textureHeight = new float[1];
    private bool[] textureOnPath = new bool[1];

    private string saveLocation;

    private GameObject[] objectsOnPath = new GameObject[1];
    private bool[] objectOnPathIsTree = new bool[1];
    private bool[] objectOnPath = new bool[1];
    private float[] pToMakeObject = new float[1];
    private float[] objectPlacementStrictness = new float[1];

    // Variables related to creating the map from tiles.
    private GameObject[] tiles = new GameObject[0];

    private int mazeWidth = 4, mazeHeight = 4;
    private int[] VEBP = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, HEBP = new int[] { 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
    private string VEBPString = "111111111111", HEBPString = "000110000001";

    private Vector2 start = new Vector2(0, 0), end = new Vector2(3, 0);

    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        EditorGUILayout.BeginHorizontal();
        generateTiles = EditorGUILayout.Toggle("Generate tiles", generateTiles);
        if (generateTiles) useCreatedTiles = false;
        useCreatedTiles = EditorGUILayout.Toggle("Use created tiles", useCreatedTiles);
        if (useCreatedTiles) generateTiles = false;
        EditorGUILayout.EndHorizontal();

        mazeWidth = EditorGUILayout.IntField("Maze width", mazeWidth);
        if (mazeWidth < 2) mazeWidth = 2;
        mazeHeight = EditorGUILayout.IntField("Maze height", mazeHeight);
        if (mazeHeight < 2) mazeHeight = 2;

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        start = EditorGUILayout.Vector2Field("Start tile", start);
        EditorGUILayout.EndHorizontal();
        start.x = Mathf.Clamp(start.x, 0, mazeWidth - 1);
        start.y = Mathf.Clamp(start.y, 0, mazeHeight - 1);
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
        end = EditorGUILayout.Vector2Field("End tile", end);
        EditorGUILayout.EndHorizontal();
        end.x = Mathf.Clamp(end.x, 0, mazeWidth - 1);
        end.y = Mathf.Clamp(end.y, 0, mazeHeight - 1);
         
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

        if (generateTiles)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options for generated tiles");
            EditorGUILayout.BeginHorizontal();
            textureTiles = EditorGUILayout.Toggle("Texture tiles", textureTiles);
            heightmapBasedPath = EditorGUILayout.Toggle("Heightmap based on path", heightmapBasedPath);
            objectsBasedPath = EditorGUILayout.Toggle("Objects based on path", objectsBasedPath);
            EditorGUILayout.EndHorizontal();

            tileWidth = EditorGUILayout.FloatField("Tile width", tileWidth);
            if (tileWidth < 0) tileWidth = 0;
            tileHeight = EditorGUILayout.FloatField("Tile height", tileHeight);
            if (tileHeight < 0) tileHeight = 0;
            pathWidth = EditorGUILayout.FloatField("Path width", pathWidth);
            pathWidth = Mathf.Clamp(pathWidth, 0, Mathf.Min(new float[] { tileWidth, tileHeight }));
            perTileDetail = EditorGUILayout.IntField("Detail per tile", perTileDetail);
            perTileDetail = Mathf.Clamp(perTileDetail, 3, 30);

            if (heightmapBasedPath)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Options for heightmap");

                pathHeight = EditorGUILayout.FloatField("Terrain path Height", pathHeight);
                otherHeight = EditorGUILayout.FloatField("Terrain non-path Height", otherHeight);
                heightmapResolution = EditorGUILayout.IntField("Heightmap Resolution", heightmapResolution);
                heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution) + 1;
                heightmapResolution = Mathf.Clamp(heightmapResolution, 33, 4097);
                detailResolution = EditorGUILayout.IntField("Detail Resolution", detailResolution);
                detailResolution = Mathf.ClosestPowerOfTwo(detailResolution);
                detailResolution = Mathf.Clamp(detailResolution, 0, 4096);
                detailResolutionPerPatch = EditorGUILayout.IntField("Detail Resolution Per Patch", detailResolutionPerPatch);
                detailResolutionPerPatch = Mathf.ClosestPowerOfTwo(detailResolutionPerPatch);
                detailResolutionPerPatch = Mathf.Clamp(detailResolutionPerPatch, 8, 128);


                EditorGUILayout.BeginHorizontal();
                heightmapSmoothing = EditorGUILayout.Toggle("Smooth heightmap", heightmapSmoothing);
                EditorGUILayout.EndHorizontal();
                if (heightmapSmoothing)
                {
                    gaussBlurRadius = EditorGUILayout.IntField("Radius of smooth", gaussBlurRadius);
                    gaussBlurRadius = Mathf.Clamp(gaussBlurRadius, 0, heightmapResolution);
                    gaussBlurPass = EditorGUILayout.IntField("Smooth passes", gaussBlurPass);
                    gaussBlurPass = Mathf.Clamp(gaussBlurPass, 0, 10);
                }
            }

            if (textureTiles)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Options for texturing");
                baseTextureResolution = EditorGUILayout.IntField("Base Texture Reolution", baseTextureResolution);
                baseTextureResolution = Mathf.ClosestPowerOfTwo(baseTextureResolution);
                baseTextureResolution = Mathf.Clamp(baseTextureResolution, 16, 2048);
                EditorGUILayout.BeginHorizontal();
                if (heightmapBasedPath)
                {
                    texturesBasedOnHeightmap = EditorGUILayout.Toggle("Texture based on heightmap", texturesBasedOnHeightmap);
                    if (texturesBasedOnHeightmap) texturesBasedOnPath = false;
                }
                texturesBasedOnPath = EditorGUILayout.Toggle("Textuing based on path", texturesBasedOnPath);
                if (texturesBasedOnPath) texturesBasedOnHeightmap = false;
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < textures.Length; i++)
                {
                    textures[i] = (Texture2D)EditorGUILayout.ObjectField("Texture " + i, textures[i], typeof(Texture), true);
                    if (!texturesBasedOnHeightmap && !texturesBasedOnPath)
                    {
                        textureProportion[i] = EditorGUILayout.FloatField("Proportion of texture " + i, textureProportion[i]);
                        textureProportion[i] = Mathf.Clamp01(textureProportion[i]);
                    }
                    else if (texturesBasedOnHeightmap)
                    {
                        textureHeight[i] = EditorGUILayout.FloatField("Height for texture " + i, textureHeight[i]);
                    }
                    else if (texturesBasedOnPath)
                    {
                        textureOnPath[i] = EditorGUILayout.Toggle("Texture for path", textureOnPath[i]);
                        textureProportion[i] = EditorGUILayout.FloatField("Proportion of texture " + i + " on/off path", textureProportion[i]);
                        textureProportion[i] = Mathf.Clamp01(textureProportion[i]);
                    }
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add texture"))
                {
                    ArrayUtility.Add(ref textures, null);
                    ArrayUtility.Add(ref textureProportion, 0);
                    ArrayUtility.Add(ref textureHeight, 0);
                    ArrayUtility.Add(ref textureOnPath, false);
                }
                if (GUILayout.Button("Remove texture"))
                {
                    if (textures.Length > 0) ArrayUtility.RemoveAt(ref textures, textures.Length - 1);
                    if (textureProportion.Length > 0) ArrayUtility.RemoveAt(ref textureProportion, textureProportion.Length - 1);
                    if (textureHeight.Length > 0) ArrayUtility.RemoveAt(ref textureHeight, textureHeight.Length - 1);
                    if (textureOnPath.Length > 0) ArrayUtility.RemoveAt(ref textureOnPath, textureOnPath.Length - 1);
                }
                GUILayout.EndHorizontal();
            }

            if (objectsBasedPath)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Options for objects");
                for (int i = 0; i < objectsOnPath.Length; i++)
                {
                    objectsOnPath[i] = (GameObject)EditorGUILayout.ObjectField("Object " + i, objectsOnPath[i], typeof(GameObject), true);
                    objectOnPathIsTree[i] = EditorGUILayout.Toggle("Is a tree", objectOnPathIsTree[i]);
                    objectOnPath[i] = EditorGUILayout.Toggle("Object lies on path", objectOnPath[i]);
                    pToMakeObject[i] = EditorGUILayout.FloatField("% to make (recommend 0.001", pToMakeObject[i]);
                    pToMakeObject[i] = Mathf.Clamp01(pToMakeObject[i]);
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add object"))
                {
                    ArrayUtility.Add(ref objectsOnPath, null);
                    ArrayUtility.Add(ref objectOnPathIsTree, false);
                    ArrayUtility.Add(ref objectOnPath, false);
                    ArrayUtility.Add(ref pToMakeObject, 0);
                }
                if (GUILayout.Button("Remove object"))
                {
                    if (objectsOnPath.Length > 0) ArrayUtility.RemoveAt(ref objectsOnPath, objectsOnPath.Length - 1);
                    if (objectOnPathIsTree.Length > 0) ArrayUtility.RemoveAt(ref objectOnPathIsTree, objectOnPathIsTree.Length - 1);
                    if (objectOnPath.Length > 0) ArrayUtility.RemoveAt(ref objectOnPath, objectOnPath.Length - 1);
                    if (pToMakeObject.Length > 0) ArrayUtility.RemoveAt(ref pToMakeObject, pToMakeObject.Length - 1);
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Path were to save TerrainData:");
            saveLocation = EditorGUILayout.TextField("Assets/", saveLocation);
            GUILayout.BeginHorizontal();
			if (GUILayout.Button ("Generate all tiles")) {
                if (ValidateBitVectors())
                {
                    GenerateTiles();
                }
			}
            GUILayout.EndHorizontal();
        } 
        else if (useCreatedTiles)
        {

        }


        GUILayout.EndScrollView();
    }

    private bool ValidateBitVectors()
    {
        bool x = true;
        if (VEBPString.Length != mazeWidth * (mazeHeight - 1))
        {
            Debug.Log("Bit vector doesn't match expected length based on maze width and height");
            x = false;
        }
        else if (HEBPString.Length != mazeHeight * (mazeWidth - 1))
        {
            Debug.Log("Bit vector doesn't match expected length based on maze width and height");
            x = false;
        }
        else
        {
            VEBP = new int[mazeWidth * (mazeHeight - 1)];
            HEBP = new int[mazeHeight * (mazeWidth - 1)];
            for (int i = 0; i < VEBPString.Length; i++)
            {
                string a;
                a = VEBPString.Substring(i, 1);
                if ((a != "0" && a != "1"))
                {
                    Debug.Log("All entries in the bit vector should be 1 or 0");
                    x = false;
                }
                else
                {
                    VEBP[i] = int.Parse(a);
                }
            }
            for(int i = 0; i < HEBPString.Length; i++)
            {
                string a;
                a = HEBPString.Substring(i, 1);
                if ((a != "0" && a != "1"))
                {
                    Debug.Log("All entries in the bit vector should be 1 or 0");
                    x = false;
                }
                else
                {
                    HEBP[i] = int.Parse(a);
                }
            }
        }
        return x;
    }

    private int[,] createTilingFromEBP()
    {
        int[,] tilingMap = new int[mazeHeight, mazeWidth];
        for (int r = 0; r < mazeHeight; r++)
        {
            for (int c = 0; c < mazeWidth; c++)
            {
                int horizontalLeft = HEBP[mazeHeight * Mathf.Max(c - 1, 0) + r];
                int horizontalRight = HEBP[Mathf.Min(mazeHeight * c + r, HEBP.Length - 1)];
                int verticalTop = VEBP[mazeWidth * Mathf.Max(r - 1, 0) + c];
                int verticalBot = VEBP[Mathf.Min(mazeWidth * r + c, VEBP.Length - 1)];

                if (c == 0 && r == 0)
                {
                    horizontalLeft = 0;
                    verticalTop = 0;
                    //Debug.Log ("top left");
                }
                else if (c == mazeWidth - 1 && r == mazeHeight - 1)
                {
                    horizontalRight = 0;
                    verticalBot = 0;
                    //Debug.Log ("bot right");
                }
                else if (c == mazeWidth - 1 && r == 0)
                {
                    verticalTop = 0;
                    horizontalRight = 0;
                    //Debug.Log ("top right");
                }
                else if (r == mazeHeight - 1 && c == 0)
                {
                    verticalBot = 0;
                    horizontalLeft = 0;
                    //Debug.Log ("bot left");
                }
                else if (c == 0)
                {
                    horizontalLeft = 0;
                    //Debug.Log ("left");
                }
                else if (r == 0)
                {
                    verticalTop = 0;
                    //Debug.Log ("top");
                }
                else if (c == mazeWidth - 1)
                {
                    horizontalRight = 0;
                    //Debug.Log ("right");
                }
                else if (r == mazeHeight - 1)
                {
                    verticalBot = 0;
                    //Debug.Log ("bot");
                }
                else
                {
                    //Debug.Log ("center");
                }

                if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 0)
                {
					tilingMap[r, c] = EMPTY;
                }
                else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 1)
                {
					tilingMap[r, c] = LEFT;
                }
                else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 0)
                {
					tilingMap[r, c] = RIGHT;
                }
                else if (verticalBot == 0 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 1)
                {
					tilingMap[r, c] = THROUGH_HORIZONTAL;
                }
                else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 0)
                {
					tilingMap[r, c] = TOP;
                }
                else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 1)
                {
					tilingMap[r, c] = TOP_LEFT;
                }
                else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 0)
                {
					tilingMap[r, c] = TOP_RIGHT;
                }
                else if (verticalBot == 0 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 1)
                {
					tilingMap[r, c] = TOP_T;
                }
                else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 0)
                {
					tilingMap[r, c] = BOT;
                }
                else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 0 && horizontalLeft == 1)
                {
					tilingMap[r, c] = BOT_LEFT;
                }
                else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 0)
                {
					tilingMap[r, c] = BOT_RIGHT;
                }
                else if (verticalBot == 1 && verticalTop == 0 && horizontalRight == 1 && horizontalLeft == 1)
                {
					tilingMap[r, c] = TOP_T;
                }
                else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 0)
                {
					tilingMap[r, c] = THROUGH_VERTICAL;
                }
                else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 0 && horizontalLeft == 1)
                {
					tilingMap[r, c] = LEFT_T;
                }
                else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 0)
                {
					tilingMap[r, c] = RIGHT_T;
                }
                else if (verticalBot == 1 && verticalTop == 1 && horizontalRight == 1 && horizontalLeft == 1)
                {
                    tilingMap[r, c] = CROSS;
                }
            }
        }

        return tilingMap;
    }

    private void GenerateTiles()
    {
        int[,] tiling = createTilingFromEBP();        
        int startx, starty;
        if(tiling[(int)start.y, (int)start.x] == BOT)
        {
            startx = Random.Range(1, perTileDetail - 1);
            starty = perTileDetail - 1;
            tiling[(int)start.y, (int)start.x] = THROUGH_VERTICAL;
        }
        else if(tiling[(int)start.y, (int)start.x] == TOP)
        {
            startx = Random.Range(1, perTileDetail - 1);
            starty = 0;
            tiling[(int)start.y, (int)start.x] = THROUGH_VERTICAL;
        }
        else if(tiling[(int)start.y, (int)start.x] == RIGHT)
        {
            starty = Random.Range(1, perTileDetail - 1);
            startx = 0;
            tiling[(int)start.y, (int)start.x] = THROUGH_HORIZONTAL;
        }
        else if(tiling[(int)start.y, (int)start.x] == LEFT)
        {
            starty = Random.Range(1, perTileDetail - 1);
            startx = perTileDetail - 1;
            tiling[(int)start.y, (int)start.x] = THROUGH_HORIZONTAL;
        }
        else
        {
            Debug.Log("Start point is invalid.");
            return;
        }

        if (tiling[(int)end.y, (int)end.x] == BOT)
        {
            tiling[(int)end.y, (int)end.x] = THROUGH_VERTICAL;
        }
        else if (tiling[(int)end.y, (int)end.x] == TOP)
        {
            tiling[(int)end.y, (int)end.x] = THROUGH_VERTICAL;
        }
        else if (tiling[(int)end.y, (int)end.x] == RIGHT)
        {
			tiling[(int)end.y, (int)end.x] = THROUGH_HORIZONTAL;
        }
        else if (tiling[(int)end.y, (int)end.x] == LEFT)
        {
			tiling[(int)end.y, (int)end.x] = THROUGH_HORIZONTAL;
        }
        else
        {
            Debug.Log("End point is invalid.");
            return;
        }
        GenerateTile(tiling, startx, starty, (int)start.y, (int)start.x);
    }

    void GenerateTile(int[,] tiling, int startx, int starty, int r, int c)
    {
        int endx, endy;
        int midx, midy, topx, topy, leftx, lefty, botx, boty, rightx, righty;
        int[,] p = new int[perTileDetail, perTileDetail];
        Debug.Log(r + " " + c);
        Debug.Log(tiling[r, c]);
        switch (tiling[r, c])
        {
            case EMPTY:
                Debug.Log("Empty tile, shouldn't happen.");
                break;
            case LEFT:
                endy = Random.Range(1, perTileDetail - 1);
                endx = Mathf.FloorToInt((perTileDetail - 1) / 2);
                p = GenerateRandomPath(p, startx, starty, endx, endy);
                break;
            case RIGHT:
                endy = Random.Range(1, perTileDetail - 1);
                endx = Mathf.FloorToInt((perTileDetail - 1) / 2);
                p = GenerateRandomPath(p, startx, starty, endx, endy);
                break;
            case TOP:
                endy = Mathf.FloorToInt((perTileDetail - 1) / 2);
                endx = Random.Range(1, perTileDetail - 1);
                p = GenerateRandomPath(p, startx, starty, endx, endy);
                break;
            case BOT:
                endy = Mathf.FloorToInt((perTileDetail - 1) / 2);
                endx = Random.Range(1, perTileDetail - 1);
                p = GenerateRandomPath(p, startx, starty, endx, endy);
                break;
            case THROUGH_HORIZONTAL:
                endx = perTileDetail - 1 - startx;
                endy = Random.Range(1, perTileDetail - 1);
                p = GenerateRandomPath(p, startx, starty, endx, endy);
                if (r != end.y || c != end.x)
                {
					if(startx == 0)
					{
						GenerateTile (tiling, perTileDetail - 1 - endx, endy, r, c + 1);
					}
					else
					{
						GenerateTile (tiling, perTileDetail - 1 - endx, endy, r, c - 1);
					}
                }
                break;
            case THROUGH_VERTICAL:
                endy = perTileDetail - 1 - starty;
                endx = Random.Range(1, perTileDetail - 1);
                p = GenerateRandomPath(p, startx, starty, endx, endy);               
                if (r != end.y || c != end.x)
                {
					if(starty == 0)
					{
						GenerateTile (tiling, endx, perTileDetail - 1 - endy, r - 1, c);			
					}
					else
					{
						GenerateTile (tiling, endx, perTileDetail - 1 - endy, r + 1, c);
					}
                }
                break;
            case BOT_LEFT:
                if (starty == 0)
                {
                    endx = 0;
                    endy = Random.Range(1, perTileDetail - 1); 
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, perTileDetail - 1, endy, r, c - 1);
                }
                else
                {
                    endy = 0;
                    endx = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, endx, perTileDetail - 1 - endy, r + 1, c);
                }
				break;
			case BOT_RIGHT:
                if (starty == 0)
                {
                    endx = perTileDetail - 1;
                    endy = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, 0, endy, r, c + 1);
                }
                else
                {
                    endy = 0;
                    endx = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, endx, perTileDetail - 1, r + 1, c);	
                }


                break;
            case TOP_RIGHT:
                if (starty == perTileDetail - 1)
                {
                    endx = perTileDetail - 1;
                    endy = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, 0, endy, r, c + 1);
                }
                else
                {
                    endy = perTileDetail - 1;
                    endx = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, endx, perTileDetail - 1 - endy, r - 1, c);
                }

                 
                break;
            case TOP_LEFT:
                if (starty == perTileDetail - 1)
                {
                    endx = 0;
                    endy = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
					GenerateTile (tiling, perTileDetail - 1, endy, r, c - 1);
                }
                else
                {
                    endy = perTileDetail - 1;
                    endx = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, endx, endy);
				    GenerateTile (tiling, endx, perTileDetail - 1 - endy, r - 1, c);
                }


                break;
            case LEFT_T:
                midx = Random.Range(1, perTileDetail - 1);
                midy = Random.Range(1, perTileDetail - 1);
                if(starty == 0)
                {
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = CleanPath(p);
                    GenerateTile (tiling, topx, 0, r - 1, c);
					GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
                }
                else if(starty == perTileDetail - 1)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
					GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
                }
                else
                {
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = 0;
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = CleanPath(p);
                    GenerateTile (tiling, topx, 0, r - 1, c);
					GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
                }
                break;
            case RIGHT_T:
                midx = Random.Range(1, perTileDetail - 1);
                midy = Random.Range(1, perTileDetail - 1);
                if (starty == 0)
                {
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = CleanPath(p);
					GenerateTile (tiling, topx, 0, r - 1, c);
					GenerateTile (tiling, 0, righty, r, c + 1);
                }
                else if (starty == perTileDetail - 1)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
					GenerateTile (tiling, 0, righty, r, c + 1);
                }
                else
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r - 1, c);
					GenerateTile (tiling, topx, 0, r + 1, c);
                }
                break;
            case TOP_T:
                midx = Random.Range(1, perTileDetail - 1);
                midy = Random.Range(1, perTileDetail - 1);
                if(startx == 0)
                {
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = CleanPath(p);
					GenerateTile (tiling, 0, righty, r, c + 1);
					GenerateTile (tiling, topx, 0, r - 1, c);
                }
                else if(startx == perTileDetail - 1)
                {
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = CleanPath(p);
                    GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
					GenerateTile (tiling, topx, 0, r - 1, c);
                } 
                else
                { 
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = CleanPath(p);
                    GenerateTile (tiling, 0, righty, r, c + 1);
					GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
                }

                break;
            case BOT_T:
                midx = Random.Range(1, perTileDetail - 1);
                midy = Random.Range(1, perTileDetail - 1);
                if (startx == 0)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
					GenerateTile (tiling, 0, righty, r, c + 1);
                }
                else if (startx == perTileDetail - 1)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = CleanPath(p);
                    GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
					GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
                }
                else
                {
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, leftx, lefty);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = CleanPath(p);
                    GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
					GenerateTile (tiling, 0, righty, r, c + 1);
                }

                break;
            case CROSS:
                midx = Random.Range(1, perTileDetail - 1);
                midy = Random.Range(1, perTileDetail - 1);
                if (startx == 0)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomConnectingPath(p, botx, boty);
                    p = GenerateRandomConnectingPath(p, rightx, righty);
                    p = GenerateRandomConnectingPath(p, topx, topy);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
					GenerateTile (tiling, 0, righty, r, c + 1);
					GenerateTile (tiling, topx, 0, r - 1, c);
                }
                else if (startx == perTileDetail - 1)
                {
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail - 1;
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomPath(p, midx, midy, botx, boty);
                    p = GenerateRandomPath(p, midx, midy, leftx, lefty);
                    p = GenerateRandomPath(p, midx, midy, topx, topy);
                    p = CleanPath(p);
                    GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
					GenerateTile (tiling, leftx, lefty, r, c - 1);
					GenerateTile (tiling, topx, 0, r - 1, c);
                }
                else if(starty == 0)
                {
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    rightx = perTileDetail - 1;
                    righty = Random.Range(1, perTileDetail - 1);
                    topx = Random.Range(1, perTileDetail - 1);
                    topy = perTileDetail;
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomPath(p, midx, midy, topx, topy);
                    p = GenerateRandomPath(p, midx, midy, rightx, righty);
                    p = GenerateRandomPath(p, midx, midy, leftx, lefty);
                    p = CleanPath(p);
                    GenerateTile (tiling, topx, 0, r - 1, c);
					GenerateTile (tiling, 0, righty, r, c + 1);
					GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
                }
                else
                {
                    leftx = 0;
                    lefty = Random.Range(1, perTileDetail - 1);
                    rightx = perTileDetail;
                    righty = Random.Range(1, perTileDetail - 1);
                    botx = Random.Range(1, perTileDetail - 1);
                    boty = 0;
                    p = GenerateRandomPath(p, startx, starty, midx, midy);
                    p = GenerateRandomPath(p, midx, midy, leftx, lefty);
                    p = GenerateRandomPath(p, midx, midy, rightx, righty);
                    p = GenerateRandomPath(p, midx, midy, botx, boty);
                    p = CleanPath(p);
                    GenerateTile (tiling, perTileDetail - 1, lefty, r, c - 1);
					GenerateTile (tiling, 0, righty, r, c + 1);
					GenerateTile (tiling, botx, perTileDetail - 1, r + 1, c);
                }
                break;
            default:
                Debug.Log("Error with tiling.");
                return;
        }
        
		TerrainData terrainData = new TerrainData();        
		string name = "Terrain";

		terrainData.baseMapResolution = baseTextureResolution;
		terrainData.heightmapResolution = heightmapResolution;
		terrainData.alphamapResolution = baseTextureResolution;
		terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch); 

        if (heightmapBasedPath)
        {
			float[,] heightmap = new float[heightmapResolution, heightmapResolution];


			terrainData.SetHeights (0, 0, heightmap);
        }
		AssetDatabase.CreateAsset(terrainData, "Assets/" + saveLocation + name + r + "_" + c + ".asset");
		AssetDatabase.SaveAssets();
		terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + saveLocation + name + r + "_" + c + ".asset", typeof(TerrainData));
        if (objectsBasedPath)
        {

        }
        if (textureTiles)
        {
			SplatPrototype[] splats = new SplatPrototype[textures.Length];
			for(int i = 0; i < textures.Length; i++)
			{
				splats[i] = new SplatPrototype();
				splats[i].texture = textures[i];
			}
			terrainData.splatPrototypes = splats;
			float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

            if (texturesBasedOnPath)
            {			
				
				for(int row = 0; row < perTileDetail; row++){
					for (int column = 0; column < perTileDetail; column++) {
						if (p [row, column] > 0) {
							for (int y = Mathf.FloorToInt (((float)row / perTileDetail) * baseTextureResolution); 
								y < Mathf.FloorToInt (((float)(row + 1) / perTileDetail) * baseTextureResolution); y++) {
							for (int x = Mathf.FloorToInt (((float)column / perTileDetail) * baseTextureResolution); 
								x < Mathf.FloorToInt (((float)(column + 1) / perTileDetail) * baseTextureResolution); x++) {
									splatmapData [y, x, 0] = 1.0f;
								}
								
							}
						}

					}
				}

            }
            else if (heightmapBasedPath && texturesBasedOnHeightmap)
            { 

            }
            else
            {

            }
			terrainData.SetAlphamaps(0, 0, splatmapData);
        }
		terrainData.size = new Vector3(tileWidth, 1, tileHeight);
		terrainData.name = name + r + "_" + c;
		GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);

		terrain.name = name + r + "_" + c;
		terrain.transform.position = new Vector3(c * tileWidth, 0, r * -tileHeight);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
    }

	public int[,] GenerateRandomPath(int[,] pathArray, int startx, int starty, int endx, int endy)
	{
		Stack<int[,]> pathArrays = new Stack<int[,]> ();
		Stack<int[]> positions = new Stack<int[]> ();
		List<int[]> solutions = new List<int[]> ();
		/*
		 * Deletes the edges of the tile from the solution set.
		 */
		for (int y = 0; y < perTileDetail; y++) {
			for (int x = 0; x < perTileDetail; x++) {
				if ((x == 0 || x == perTileDetail - 1 || y == 0 || y == perTileDetail - 1) && pathArray[y, x] == 0) {
					pathArray [y, x] = -1;
				}
			}
		}

		pathArray [starty, startx] = 1;
		pathArray [endy, endx] = 0;
		pathArrays.Push (pathArray);

		positions.Push (new int[] { startx, starty });

		while (positions.Peek()[0] != endx || positions.Peek()[1] != endy) {

			solutions = new List<int[]> ();
			pathArray = (int[,])pathArrays.Peek ().Clone();
			if (positions.Peek()[1] > 0 && pathArray [positions.Peek()[1] - 1, positions.Peek()[0]] == 0) {
				solutions.Add (new int[] { positions.Peek()[0], positions.Peek()[1] - 1});
			}
			if (positions.Peek()[1] < perTileDetail - 1 && pathArray [positions.Peek()[1] + 1, positions.Peek()[0]] == 0) {
				solutions.Add (new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
			}
			if (positions.Peek()[0] > 0 && pathArray [positions.Peek()[1], positions.Peek()[0] - 1] == 0) {
				solutions.Add (new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
			}
			if (positions.Peek()[0] < perTileDetail - 1 && pathArray [positions.Peek()[1], positions.Peek()[0] + 1] == 0) {
				solutions.Add (new int[] { positions.Peek()[0] + 1, positions.Peek()[1] });
			}

			if (solutions.Count == 0) {				
				pathArrays.Pop ();
				pathArray = pathArrays.Pop ();
				pathArray [positions.Peek()[1], positions.Peek()[0]] = -1;
				pathArrays.Push (pathArray);
				positions.Pop ();
			} else {
				foreach(int[] sol in solutions){
					if (sol [1] == endy && sol [0] == endx) {
						pathArray [endy, endx] = 1;
						positions.Push (new int[] { endx, endy });
					}
				}

				if (positions.Peek()[0] != endx || positions.Peek()[1] != endy) {
					int[][] solutionArray = solutions.ToArray ();
					int[] chosen = solutionArray[Random.Range(0, solutions.Count)];
					pathArray [chosen [1], chosen [0]] = 1;
					positions.Push (new int[] { chosen[0], chosen[1] });
					foreach(int[] sol in solutions){
						if (pathArray [sol [1], sol [0]] != 1) {
							pathArray [sol [1], sol [0]] = -1;
						}
					}
				}
				pathArrays.Push (pathArray);
			}
		}
		return pathArrays.Pop ();
	}
    public int[,] GenerateRandomConnectingPath(int[,] pathArray, int startx, int starty)
    {
        Stack<int[,]> pathArrays = new Stack<int[,]>();
        Stack<int[]> positions = new Stack<int[]>();
        List<int[]> solutions = new List<int[]>();
        /*
         * Reset the -1's to available spots and 2s to 1s
         */
        for (int y = 0; y < perTileDetail; y++)
        {
            for (int x = 0; x < perTileDetail; x++)
            {
                if (pathArray[y, x] == -1)
                {
                    pathArray[y, x] = 0;
                }
                else if(pathArray[y, x] == 2)
                {
                    pathArray[y, x] = 1;
                }
            }
        }
        
        
        /*
		 * Deletes the edges of the tile from the solution set.
		 */
        for (int y = 0; y < perTileDetail; y++)
        {
            for (int x = 0; x < perTileDetail; x++)
            {
                if ((x == 0 || x == perTileDetail - 1 || y == 0 || y == perTileDetail - 1) && pathArray[y, x] == 0)
                {
                    pathArray[y, x] = -1;
                }
            }
        }

        pathArray[starty, startx] = 2;
        pathArrays.Push(pathArray);

        positions.Push(new int[] { startx, starty });
        
        while (pathArrays.Peek()[ positions.Peek()[1], positions.Peek()[0]] != 1)
        {

            solutions = new List<int[]>();
            pathArray = (int[,])pathArrays.Peek().Clone();
            if (positions.Peek()[1] > 0 && (pathArray[positions.Peek()[1] - 1, positions.Peek()[0]] == 0 || pathArray[positions.Peek()[1] - 1, positions.Peek()[0]] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] - 1 });
            }
            if (positions.Peek()[1] < perTileDetail - 1 && (pathArray[positions.Peek()[1] + 1, positions.Peek()[0]] == 0 || pathArray[positions.Peek()[1] + 1, positions.Peek()[0]] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
            }
            if (positions.Peek()[0] > 0 && (pathArray[positions.Peek()[1], positions.Peek()[0] - 1] == 0 || pathArray[positions.Peek()[1], positions.Peek()[0] - 1] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
            }
            if (positions.Peek()[0] < perTileDetail - 1 && (pathArray[positions.Peek()[1], positions.Peek()[0] + 1] == 0 || pathArray[positions.Peek()[1], positions.Peek()[0] + 1] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0] + 1, positions.Peek()[1] });
            }

            if (solutions.Count == 0)
            {
                pathArrays.Pop();
                pathArray = pathArrays.Pop();
                pathArray[positions.Peek()[1], positions.Peek()[0]] = -1;
                pathArrays.Push(pathArray);
                positions.Pop();
            }
            else
            {
                foreach (int[] sol in solutions)
                {
                    if (pathArray[sol[1], sol[0]] == 1)
                    {                        
                        positions.Push(new int[] { sol[0], sol[1] });
                    }
                }

                if (pathArray[positions.Peek()[1], positions.Peek()[0]] != 1)
                {
                    int[][] solutionArray = solutions.ToArray();
                    int[] chosen = solutionArray[Random.Range(0, solutions.Count)];
                    pathArray[chosen[1], chosen[0]] = 2;
                    positions.Push(new int[] { chosen[0], chosen[1] });
                    foreach (int[] sol in solutions)
                    {
                        if (pathArray[sol[1], sol[0]] != 2)
                        {
                            pathArray[sol[1], sol[0]] = -1;
                        }
                    }
                }
                pathArrays.Push(pathArray);
            }
        }
        return pathArrays.Pop();
    }
    public int[,] CleanPath(int[,] pathArray)
    {
        bool finished = false;
        while (!finished)
        {
            finished = true;
            for (int r = 1; r < perTileDetail - 1; r++)
            {
                for (int c = 1; c < perTileDetail - 1; c++)
                {
                    if (pathArray[r, c] == 1 && AliveNeighbors(pathArray, r, c) < 2)
                    {
                        finished = false;
                        pathArray[r, c] = 0;
                    }
                    if(pathArray[r, c] < 1 && AliveNeighbors(pathArray, r, c) > 3)
                    {
                        finished = false;
                        pathArray[r, c] = 1;
                    }
                }
            }
        }
        return pathArray;
    }
    public int AliveNeighbors(int[,] pathArray, int r, int c)
    {
        int alive = 0;
        if(r > 0)
        {
            if(pathArray[r - 1, c] > 0)
            {
                alive++;
            }
        }
        if(r < perTileDetail - 1)
        {
            if(pathArray[r + 1, c] > 0)
            {
                alive++;
            }
        }
        if(c > 0)
        {
            if(pathArray[r, c - 1] > 0)
            {
                alive++;
            }
        }
        if(c < perTileDetail - 1)
        {
            if(pathArray[r, c + 1] > 0)
            {
                alive++;
            }
        }

        return alive;
    }
}

