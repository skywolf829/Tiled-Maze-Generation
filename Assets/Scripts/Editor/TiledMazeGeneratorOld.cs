using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Assets.Scripts.Editor;

public class TiledMazeGeneratorOld : EditorWindow
{
    [MenuItem("Window/Tiled Maze Generator Old")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TiledMazeGeneratorOld));
    }

    [MenuItem("Terrain/Tiled Maze Generator Old")]
    public static void CreateWindow()
    {
        window = EditorWindow.GetWindow(typeof(TiledMazeGeneratorOld));
        window.titleContent = new GUIContent("Tiled Maze Generator Old");
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
    private bool sampled = true;

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

    private Tile[,] tiles;
    private TextAsset[] files = new TextAsset[1];

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
            perTileDetail = Mathf.Clamp(perTileDetail, 3, 10);

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
                sampled = EditorGUILayout.Toggle("Sample bezier curve", sampled);
                baseTextureResolution = EditorGUILayout.IntField("Base Texture Reolution", baseTextureResolution);
                baseTextureResolution = Mathf.ClosestPowerOfTwo(baseTextureResolution);
                baseTextureResolution = Mathf.Clamp(baseTextureResolution, 16, 2048);
                EditorGUILayout.BeginHorizontal();
                if (heightmapBasedPath)
                {
                    texturesBasedOnHeightmap = EditorGUILayout.Toggle("Texture based on heightmap", texturesBasedOnHeightmap);
                    if (texturesBasedOnHeightmap) texturesBasedOnPath = false;
                }
                texturesBasedOnPath = EditorGUILayout.Toggle("Texturing based on path", texturesBasedOnPath);
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
            if (GUILayout.Button("Generate all tiles"))
            {
                if (ValidateBitVectors())
                {
                    if (saveLocation == string.Empty) saveLocation = "GeneratedMazeTiles/";
                    string pathToCheck = Application.dataPath + "/" + saveLocation;
                    if (Directory.Exists(pathToCheck) == false)
                    {
                        Directory.CreateDirectory(pathToCheck);
                    }

                    tiles = new Tile[mazeHeight, mazeWidth];
                    for (int r = 0; r < mazeHeight; r++)
                    {
                        for (int c = 0; c < mazeWidth; c++)
                        {
                            tiles[r, c] = new Tile(r, c, tileWidth, tileHeight, perTileDetail);
                            tiles[r, c].setPathWidth(pathWidth);
                            tiles[r, c].setName(r + "_" + c);
                        }
                    }
                    //GenerateTiles();
                    TilePathGenerator tpg = new TilePathGenerator();
                    tpg.setDetail(perTileDetail);
                    tpg.setSaveLocation(saveLocation);
                    tpg.setNumPossibleExits(2);
                    tpg.beginGenerator();
                    /*
                    for (int r = 0; r < mazeHeight; r++)
                    {
                        for (int c = 0; c < mazeWidth; c++)
                        {
                            tiles[r, c].createSplatMap(textures, baseTextureResolution, sampled);
                            tiles[r, c].createTile();
                            tiles[r, c].saveTileArray(saveLocation);
                        }
                    }
                    */
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.TextArea("");
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
            for (int i = 0; i < HEBPString.Length; i++)
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
                    tilingMap[r, c] = BOT_T;
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
                tiles[r, c].setType(tilingMap[r, c]);
            }
        }

        return tilingMap;
    }
    private void GenerateTiles()
    {
        int[,] tiling = createTilingFromEBP();
        int startx, starty;
        bool cont = true;
        if (tiles[(int)start.y, (int)start.x].getType() == BOT)
        {
            startx = Random.Range(1, perTileDetail - 1);
            starty = perTileDetail - 1;
            tiling[(int)start.y, (int)start.x] = THROUGH_VERTICAL;
            tiles[(int)start.y, (int)start.x].setType(THROUGH_VERTICAL);
        }
        else if (tiles[(int)start.y, (int)start.x].getType() == TOP)
        {
            startx = Random.Range(1, perTileDetail - 1);
            starty = 0;
            tiles[(int)start.y, (int)start.x].setType(THROUGH_VERTICAL);
        }
        else if (tiles[(int)start.y, (int)start.x].getType() == RIGHT)
        {
            starty = Random.Range(1, perTileDetail - 1);
            startx = 0;
            tiles[(int)start.y, (int)start.x].setType(THROUGH_HORIZONTAL);
        }
        else if (tiles[(int)start.y, (int)start.x].getType() == LEFT)
        {
            starty = Random.Range(1, perTileDetail - 1);
            startx = perTileDetail - 1;
            tiles[(int)start.y, (int)start.x].setType(THROUGH_HORIZONTAL);
        }
        else
        {
            Debug.Log("Start point is invalid.");
            cont = false;
        }

        if (tiles[(int)end.y, (int)end.x].getType() == BOT)
        {
            tiles[(int)end.y, (int)end.x].setType(THROUGH_VERTICAL);
        }
        else if (tiles[(int)end.y, (int)end.x].getType() == TOP)
        {
            tiles[(int)end.y, (int)end.x].setType(THROUGH_VERTICAL);
        }
        else if (tiles[(int)end.y, (int)end.x].getType() == RIGHT)
        {
            tiles[(int)end.y, (int)end.x].setType(THROUGH_HORIZONTAL);
        }
        else if (tiles[(int)end.y, (int)end.x].getType() == LEFT)
        {
            tiles[(int)end.y, (int)end.x].setType(THROUGH_HORIZONTAL);
        }
        else
        {
            Debug.Log("End point is invalid.");
            cont = false;
        }
        if (cont)
        {
            GenerateTile2((int)start.y, (int)start.x);
        }
    }
    private void GenerateTile2(int r, int c)
    {
        if (r < 0 || r >= mazeHeight || c < 0 || c >= mazeWidth || tiles[r, c].isCreated())
        {
            return;
        }
        int leftx = 0, rightx = perTileDetail - 1, topx, botx, lefty, righty, topy = perTileDetail - 1, boty = 0, midx = Random.Range(1, perTileDetail - 1), midy = Random.Range(1, perTileDetail - 1);
        if (r > 0 && tiles[r - 1, c].isCreated() && tiles[r - 1, c].hasBotEntrance())
        {
            Vector2 top = tiles[r - 1, c].getBotEntrance();
            topx = (int)top.x;
        }
        else
        {
            topx = Random.Range(1, perTileDetail - 1);
        }
        if (r < mazeHeight - 1 && tiles[r + 1, c].isCreated() && tiles[r + 1, c].hasTopEntrance())
        {
            Vector2 bot = tiles[r + 1, c].getTopEntrance();
            botx = (int)bot.x;
        }
        else
        {
            botx = Random.Range(1, perTileDetail - 1);
        }
        if (c > 0 && tiles[r, c - 1].isCreated() && tiles[r, c - 1].hasRightEntrance())
        {
            Vector2 left = tiles[r, c - 1].getRightEntrance();
            lefty = (int)left.y;
        }
        else
        {
            lefty = Random.Range(1, perTileDetail - 1);
        }
        if (c < mazeWidth - 1 && tiles[r, c + 1].isCreated() && tiles[r, c + 1].hasLeftEntrance())
        {
            Vector2 right = tiles[r, c + 1].getLeftEntrance();
            righty = (int)right.y;
        }
        else
        {
            righty = Random.Range(1, perTileDetail - 1);
        }

        switch (tiles[r, c].getType())
        {
            case EMPTY:
                break;
            case LEFT:
                tiles[r, c].generatePath(leftx, lefty, midx, midy);
                tiles[r, c].setRightEntrance(new Vector2(leftx, lefty));
                GenerateTile2(r, c - 1);
                break;
            case RIGHT:
                tiles[r, c].generatePath(rightx, righty, midx, midy);
                tiles[r, c].setLeftEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c + 1);
                break;
            case TOP:
                tiles[r, c].generatePath(topx, topy, midx, midy);
                tiles[r, c].setBotEntrance(new Vector2(topx, topy));
                GenerateTile2(r - 1, c);
                break;
            case BOT:
                tiles[r, c].generatePath(botx, boty, midx, midy);
                tiles[r, c].setTopEntrance(new Vector2(botx, boty));
                GenerateTile2(r + 1, c);
                break;
            case THROUGH_HORIZONTAL:
                tiles[r, c].generatePath(rightx, righty, leftx, lefty);
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r, c + 1);
                break;
            case THROUGH_VERTICAL:
                tiles[r, c].generatePath(topx, topy, botx, boty);
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                GenerateTile2(r - 1, c);
                GenerateTile2(r + 1, c);
                break;
            case BOT_LEFT:
                tiles[r, c].generatePath(botx, boty, leftx, lefty);
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r + 1, c);
                break;
            case BOT_RIGHT:
                tiles[r, c].generatePath(botx, boty, rightx, righty);
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c + 1);
                GenerateTile2(r + 1, c);
                break;
            case TOP_RIGHT:
                tiles[r, c].generatePath(topx, topy, rightx, righty);
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                GenerateTile2(r, c + 1);
                GenerateTile2(r - 1, c);
                break;
            case TOP_LEFT:
                tiles[r, c].generatePath(topx, topy, leftx, lefty);
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r - 1, c);
                break;
            case LEFT_T:
                tiles[r, c].generatePath(leftx, lefty, midx, midy);
                tiles[r, c].adjoinPath(topx, topy);
                tiles[r, c].adjoinPath(botx, boty);
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                GenerateTile2(r, c - 1);
                GenerateTile2(r - 1, c);
                GenerateTile2(r + 1, c);
                break;
            case RIGHT_T:
                tiles[r, c].generatePath(rightx, righty, midx, midy);
                tiles[r, c].adjoinPath(topx, topy);
                tiles[r, c].adjoinPath(botx, boty);
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c + 1);
                GenerateTile2(r - 1, c);
                GenerateTile2(r + 1, c);
                break;
            case TOP_T:
                tiles[r, c].generatePath(topx, topy, midx, midy);
                tiles[r, c].adjoinPath(leftx, lefty);
                tiles[r, c].adjoinPath(rightx, righty);
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r, c + 1);
                GenerateTile2(r - 1, c);
                break;
            case BOT_T:
                tiles[r, c].generatePath(botx, boty, midx, midy);
                tiles[r, c].adjoinPath(leftx, lefty);
                tiles[r, c].adjoinPath(rightx, righty);
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r, c + 1);
                GenerateTile2(r + 1, c);
                break;
            case CROSS:
                tiles[r, c].generatePath(topx, topy, midx, midy);
                tiles[r, c].adjoinPath(botx, boty);
                tiles[r, c].adjoinPath(leftx, lefty);
                tiles[r, c].adjoinPath(rightx, righty);
                tiles[r, c].setBotEntrance(new Vector2(botx, boty));
                tiles[r, c].setTopEntrance(new Vector2(topx, topy));
                tiles[r, c].setLeftEntrance(new Vector2(leftx, lefty));
                tiles[r, c].setRightEntrance(new Vector2(rightx, righty));
                GenerateTile2(r, c - 1);
                GenerateTile2(r, c + 1);
                GenerateTile2(r - 1, c);
                GenerateTile2(r + 1, c);
                break;
            default:
                break;
        }
    }
}

