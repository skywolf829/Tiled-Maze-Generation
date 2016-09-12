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
    private bool generateTiles = false;
    private bool textureTiles = false;
    private bool heightmapBasedPath = false;
    private bool objectsBasedPath = false;
    private bool heightmapSmoothing = false;
    private bool texturesBasedOnHeightmap = false;
    private bool texturesBasedOnPath = false;

    private float tileWidth = 10, tileHeight = 10, pathWidth = 2;
    private float pathHeight = 0, otherHeight = 5;
    private int heightmapResolution = 129, detailResolution = 128, detailResolutionPerPatch = 8, baseTextureResolution = 1024;

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
    private GameObject[] tiles = new GameObject[16];

    private int mazeWidth, mazeHeight;
    private int[] VEBP = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, HEBP = new int[] { 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
    private string VEBPString = "111111111111", HEBPString = "100000011000";

    private Vector2 start = new Vector2(0, 0), end = new Vector2(0, 0);

    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        EditorGUILayout.BeginHorizontal();
        generateTiles = EditorGUILayout.Toggle("Generate tiles", generateTiles);
        if (generateTiles) useCreatedTiles = false;
        useCreatedTiles = EditorGUILayout.Toggle("Use created tiles", useCreatedTiles);
        if (useCreatedTiles) generateTiles = false;
        EditorGUILayout.EndHorizontal();

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
            if (GUILayout.Button("Generate all tiles"))
            {
                GenerateTiles();
            }
            GUILayout.EndHorizontal();
        }
        else if (useCreatedTiles)
        {

        }


        GUILayout.EndScrollView();
    }

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
        if (textureTiles)
        {
            SplatPrototype[] splat = new SplatPrototype[textures.Length];
            for (int i = 0; i < textures.Length && textures[i] != null; i++)
            {
                splat[i] = new SplatPrototype();
                splat[i].texture = textures[i];
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
                        float textureMergeStart = 3f / 8;
                        float textureMergeEnd = 1 / 2;

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
                    float minSlope = idealSlope - 2;
                    float maxSlope = idealSlope + 2;
                    

                    Vector2 possiblePoint = Vector2.MoveTowards(currentPoint, 
                        new Vector2(currentPoint.x + 1, currentPoint.y + Random.Range(minSlope, maxSlope)),
                        pathWidth / 4);
                    int trials = 0;
                    while(trials < 10000 && (possiblePoint.x < pathWidth / 2 || possiblePoint.x > tileWidth - pathWidth / 2 
                        || possiblePoint.y < pathWidth / 2 || possiblePoint.y - pathWidth / 2 > tileHeight || visitedPoints.Contains(possiblePoint)))
                    {
                        possiblePoint = Vector2.MoveTowards(currentPoint,
                        new Vector2(currentPoint.x + 1, currentPoint.y + Random.Range(minSlope, maxSlope)),
                        pathWidth / 4);
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
    void createHalfStraight()
    {

    }
    void createTurn()
    {

    }
    void createCross()
    {

    }
    void createT()
    {

    }
    void createTilesWithTexturesBasedOnPath()
    {
        tiles = new GameObject[16];
        for(int t = 0; t < tiles.Length; t++)
        {
            
            
        }
    }

    private void GenerateTiles()
    {

        if (heightmapBasedPath)
        {

        }
        if (objectsBasedPath)
        {

        }
        if (textureTiles)
        {
            if (texturesBasedOnPath)
            {
                createStraight();
            }
            else if (heightmapBasedPath && texturesBasedOnHeightmap)
            {

            }
            else
            {

            }
        }
    }


}
