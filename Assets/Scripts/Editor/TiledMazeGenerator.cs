using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Assets.Scripts.Editor;

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

    private string saveLocation = "/GeneratedMazeTiles";
    private string usingPath = "/GeneratedMazeTiles/5_2/"; 
    private int perTileDetail = 5;
    private int numPossibleExits = 2;
    private int maxNumTilesPerType = 3;

    private float tileWidth = 10;
    private float tileHeight = 10;
    private float tileDepth = 10;

    private bool sampled = false;
    private int baseTextureResolution = 1024;
    private int heightmapResolution = 128;
    private float pathWidth = 2;
	private bool randomizeHeight = true;
	private float bumpiness = 0.2f;
	private bool smooth = true;
	private int smoothRadius = 3;
	private int smoothPass = 1;
	private bool calderas = false;
	private float calderasC = 0.2f;
	private bool terrace = false;
	private bool terraceWithValues = false;
	private bool terraceWithInterval = true;
	private float terraceInterval = 0.1f;
	private string terraceValuesString = "0 0.25 0.5 0.75 1";
	private List<float> terraceValues = new List<float>();

    private int selectedRow, selectedColumn;
    private Texture2D[,] displayTextures = new Texture2D[4, 4];
    private Texture2D[] textures = new Texture2D[1];
    private Tile[,] tiles = new Tile[4, 4];
    private int[,] tiling = new int[4, 4];
    private string[,] tilingFiles = new string[4, 4];

    private int mazeWidth = 4, mazeHeight = 4;
    private int[] VEBP = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, HEBP = new int[] { 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
    private string VEBPString = "111111111111", HEBPString = "000110000001";
    private string tilesName = "Test";

    private TextAsset t;
    List<int> exits;

    private void OnEnable()
    {
        updateArrays();
    }
    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
        saveLocation = EditorGUILayout.TextField("Save location", saveLocation);
        int oldDetail = perTileDetail;
        perTileDetail = Mathf.Clamp(EditorGUILayout.IntField("Detail per tile", perTileDetail), 3, 9);
        if (perTileDetail % 2 == 0)
        {
            perTileDetail++;
        }
        if (oldDetail != perTileDetail)
        {
            updateArrays();
        }
        numPossibleExits = Mathf.Clamp(EditorGUILayout.IntField("Possible exits", numPossibleExits), 1, (perTileDetail - 2));
        maxNumTilesPerType = Mathf.Clamp(EditorGUILayout.IntField("Number of tiles per exit combination", maxNumTilesPerType), 1, 10);
        if (GUILayout.Button("Generate all tiles")) {
            if (ValidateSaveLocation())
            {
                TilePathGenerator tpg = new TilePathGenerator();
                tpg.setDetail(perTileDetail);
                tpg.setSaveLocation(saveLocation);
                tpg.setNumPossibleExits(numPossibleExits);
                tpg.setMaxNumTilesPerType(maxNumTilesPerType);
                tpg.beginGenerator();
            }
        }

        VEBPString = EditorGUILayout.TextField("VEBP", VEBPString);
        HEBPString = EditorGUILayout.TextField("HEBP", HEBPString);

        int oldWidth = mazeWidth;
        int oldHeight = mazeHeight;

        mazeWidth = EditorGUILayout.IntField("Maze width", mazeWidth);
        if (mazeWidth < 2) mazeWidth = 2;
        mazeHeight = EditorGUILayout.IntField("Maze height", mazeHeight);
        if (mazeHeight < 2) mazeHeight = 2;

        if (oldWidth != mazeWidth || oldHeight != mazeHeight)
        {
            updateArrays();
        }

        if (GUILayout.Button("Randomize tiles") && ValidateBitVectors())
        {
            randomizeTiles();
        }
        GUILayout.BeginVertical();
        for (int i = 0; i < mazeHeight; i++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(1));
            for (int j = 0; j < mazeWidth; j++)
            {
                if (GUILayout.Button(displayTextures[i, j], "Label"))
                {
                    selectedColumn = j;
                    selectedRow = i;
                    string s = tiles[selectedRow, selectedColumn].getFileName();
                    s = s.Substring(s.LastIndexOf('\\') + 1);
                    if (s.Contains("/"))
                    {
                        s = s.Substring(s.LastIndexOf('/') + 1);
                    }
                    int x = int.Parse(s.Substring(s.Length - 5, 1));
                    s = s.Substring(0, s.Length - 5) + (x + 1) % maxNumTilesPerType;
                    loadTile(s);
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        DropAreaGUI();
        TileOptions();
        TextureArea();
        HeightmapArea();
        tilesName = EditorGUILayout.TextField("Name for tiles created", tilesName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load tiles into terrain"))
        {
            if (Directory.Exists(Application.dataPath + saveLocation + "/Terrains/" + tilesName) == false)
            {
                Directory.CreateDirectory(Application.dataPath + saveLocation + "/Terrains/" + tilesName);
            }
			if (terrace && terraceWithValues) {
				if (ValidatedTerraceValues ()) {					
					loadAllTiles ();   
				} else
					Debug.Log ("Could not parse terrace levels. Please enter floats from 0 - 1 separated with spaces");
			} else {
				loadAllTiles ();
			}
        }
        if (GUILayout.Button("Load selected tile into terrain"))
        {
            if (Directory.Exists(Application.dataPath + saveLocation + "/Terrains/" + tilesName) == false)
            {
                Directory.CreateDirectory(Application.dataPath + saveLocation + "/Terrains/" + tilesName);
            }
			if (terrace && terraceWithValues) {
				if (ValidatedTerraceValues ()) {
					loadSelectedTile (selectedRow, selectedColumn);
				} else
					Debug.Log ("Could not parse terrace levels. Please enter floats from 0 - 1 separated with spaces");
			} else {
				loadSelectedTile (selectedRow, selectedColumn);
			}
			
        }
		try{
        	GUILayout.EndHorizontal();
		}
		catch(System.InvalidOperationException e){
			//Debug.Log (e);
		}
		try{
        	GUILayout.EndScrollView();
		}
		catch(System.InvalidOperationException e){
			//Debug.Log (e);
		}
    }
	private bool ValidatedTerraceValues(){
		bool pass = true;
		string og = terraceValuesString;
		terraceValues = new List<float> ();
		while (terraceValuesString.Length > 0 && pass) {
			int spot = terraceValuesString.IndexOf (" ");

			string v;

			if (spot == -1) {
				v = terraceValuesString;
				terraceValuesString = "";
			} else {
				v = terraceValuesString.Substring (0, spot);
				terraceValuesString = terraceValuesString.Substring (spot + 1, 
					terraceValuesString.Length - spot - 1);
			}
			
			float value = 0;
			try{
				value = float.Parse (v);
			}
			catch(System.FormatException e){
				Debug.Log (e);
				pass = false;
			}
			if (pass) {
				terraceValues.Add (value);
			}
		}
		terraceValuesString = og;
		return pass;
	}
    private bool ValidateSaveLocation()
    {
        if (saveLocation == string.Empty) saveLocation = "GeneratedMazeTiles/";
        string pathToCheck = Application.dataPath + "/" + saveLocation + "/" + perTileDetail + "_" + numPossibleExits;
		if (Directory.Exists(Application.dataPath + saveLocation) == false)
        {
            Directory.CreateDirectory(saveLocation);
        }
        if (Directory.Exists(pathToCheck) == false)
        {
            Directory.CreateDirectory(pathToCheck);
        }
        usingPath = saveLocation + "/" + perTileDetail + "_" + numPossibleExits;
        return true;
    }
    private bool ValidateBitVectors()
    {
        bool x = true;
        if (VEBPString.Length != mazeWidth * (mazeHeight - 1))
        {
            //Debug.Log("Bit vector doesn't match expected length based on maze width and height");
            x = false;
        }
        else if (HEBPString.Length != mazeHeight * (mazeWidth - 1))
        {
            //Debug.Log("Bit vector doesn't match expected length based on maze width and height");
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
    private void updateArrays()
    {
        displayTextures = new Texture2D[mazeHeight, mazeWidth];
        tiles = new Tile[mazeHeight, mazeWidth];
        tiling = new int[mazeHeight, mazeWidth];
        tilingFiles = new string[mazeHeight, mazeWidth];
        for(int r = 0; r < mazeHeight; r++)
        {
            for(int c = 0; c < mazeWidth; c++)
            {
                displayTextures[r, c] = Texture2D.whiteTexture;
                tiles[r, c] = new Tile(r, c, tileWidth, tileHeight, tileDepth, perTileDetail);
                tiling[r, c] = 0;
                tilingFiles[r, c] = "";
            }
        }
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
    
    private void loadTile(string s)
    {
        s = Application.dataPath + "/" + usingPath + "/" + s + ".txt";
        int t = tiles[selectedRow, selectedColumn].getType();
        if (t == TOP && s.Contains("End"))
        {            
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        if (t == LEFT && s.Contains("End"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        if (t == RIGHT && s.Contains("End"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        if (t == BOT && s.Contains("End"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == BOT_LEFT  && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == BOT_RIGHT && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == TOP_LEFT && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == TOP_RIGHT && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if(t == THROUGH_HORIZONTAL && s.Contains("Through"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == THROUGH_VERTICAL && s.Contains("Through"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == LEFT_T  && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == RIGHT_T && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == TOP_T && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            p = rotateClockwise(p);
            p = rotateClockwise(p);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == BOT_T && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if(t == CROSS && s.Contains("Cross"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else
        {
            Debug.Log("Invalid input for selected tile");
        }
        tiles[selectedRow, selectedColumn].setFileName(s);
        updateDisplayTexture(selectedRow, selectedColumn);
    }
    private void randomizeTiles()
    {
        ValidateSaveLocation();
        usingPath = saveLocation + "/" + perTileDetail + "_" + numPossibleExits;
        for (int i = 0; i < mazeHeight; i++)
        {
            for(int j = 0; j < mazeWidth; j++)
            {
                tiles[i, j].setPath(new int[perTileDetail, perTileDetail]);
            }
        }
        float startingSpot = (float)(perTileDetail - 2) / (float)numPossibleExits;
        exits = new List<int>();
        for (int i = 1; i <= (numPossibleExits / 2); i++)
        {
            exits.Add((int)(i * startingSpot));
        }
        int length = exits.Count;
        if (numPossibleExits % 2 != 0)
        {
            exits.Add(perTileDetail / 2);
        }
        for (int i = length - 1; i >= 0; i--)
        {
            exits.Add(perTileDetail - 1 - exits[i]);
        }

        tiling = createTilingFromEBP();
        for (int i = 0; i < mazeHeight; i++)
        {
            for (int j = 0; j < mazeWidth; j++)
            {
                tiles[i, j].setComplete(false);
            }
        }
        int[,] p = new int[perTileDetail, perTileDetail];
        string[] possibleFiles = new string[1];
        chooseTile(0, 0);
    }
    private int[,] getPathFromFile(string s)
    {
        int[,] p = new int[perTileDetail, perTileDetail];
        
        StreamReader r = new StreamReader(s);
        string stringPath = r.ReadToEnd();
        for(int i = 0; i < perTileDetail; i++)
        {
            for(int j = 0; j < perTileDetail; j++)
            {
                p[i, j] = int.Parse(stringPath.Substring(i * perTileDetail + j, 1));
            }
        }
        return p;
    }
    private int[,] rotateClockwise(int[,] p)
    {
        int[,] rotated = new int[perTileDetail, perTileDetail];
        for(int i = 0; i < perTileDetail; ++i)
        {
            for(int j = 0; j < perTileDetail; ++j)
            {
                rotated[i, j] = p[perTileDetail - j - 1, i];
            }
        }
        return rotated;
    }
    
    private void chooseTile(int r, int c)
    {
        if (r < 0 || r == mazeHeight || c < 0 || c == mazeWidth || tiles[r, c].isCreated()) return;
        string filePath = "";
        string con = "";
        int lefty, righty, topx, botx;
        int[,] p = new int[perTileDetail, perTileDetail];
        string[] possibleFiles = new string[1];
        switch (tiles[r, c].getType())
        {
            case EMPTY:
                break;
            case LEFT:
                if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_" + lefty + "_?";
                }
                else
                {
                    con = "_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "EndDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c - 1);
                break;
            case RIGHT:
                if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c + 1].getLeftEntrance().y)];
                    con = "_" + righty + "_?";
                }
                else
                {
                    con = "_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "EndDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c + 1);
                break;
            case TOP:
                if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    con = "_" + topx + "_?";
                }
                else
                {
                    con = "_?_?";
                }

                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "EndDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                break;
            case BOT:
                if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = (int)tiles[r + 1, c].getTopEntrance().x;
                    con = "_" + botx + "_?";
                }
                else
                {
                    con = "_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "EndDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r + 1, c);
                break;
            case THROUGH_HORIZONTAL:
                con = "";
                if (tiles[r, c - 1].hasRightEntrance() && tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_" + righty + "_" + lefty + "_?";
                }
                else if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_?_" + lefty + "_?";
                }
                else if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    con = "_" + righty + "_?_?";
                }
                else
                {
                    con = "_?_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "ThroughDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c - 1);
                chooseTile(r, c + 1);
                break;
            case THROUGH_VERTICAL:
                con = "";
                if (tiles[r - 1, c].hasBotEntrance() && tiles[r + 1, c].hasTopEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    botx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    con = "_" + botx + "_" + topx + "_?";
                }
                else if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    con = "_" + botx + "_?_?";
                }
                else if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    con = "_?_" + topx + "_?";
                }
                else
                {
                    con = "_?_?_?";
                }

                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "ThroughDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                chooseTile(r + 1, c);
                break;
            case BOT_LEFT:
                if (tiles[r + 1, c].hasTopEntrance() && tiles[r, c - 1].hasRightEntrance())
                {
                    botx = exits[exits.Count - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_" + lefty + "_" + botx + "_?";
                }
                else if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = exits[exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    con = "_?_" + botx + "_?";
                }
                else if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = perTileDetail - 1 - (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_" + lefty + "_?_?";
                }
                else
                {
                    con = "_?_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "LDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c - 1);
                chooseTile(r + 1, c);
                break;
            case BOT_RIGHT:
                if (tiles[r + 1, c].hasTopEntrance() && tiles[r, c + 1].hasLeftEntrance())
                {
                    botx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    righty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c + 1].getLeftEntrance().y)];
                    con = "_" + botx + "_" + righty + "_?";
                }
                else if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    con = "_" + botx + "_?_?";
                }
                else if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c + 1].getLeftEntrance().y)];
                    con = "_?_" + righty + "_?";
                }
                else
                {
                    con = "_?_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "LDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c + 1);
                chooseTile(r + 1, c);
                break;
            case TOP_RIGHT:
                if (tiles[r - 1, c].hasBotEntrance() && tiles[r, c + 1].hasLeftEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    con = "_" + righty + "_" + topx + "_?";
                }
                else if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    con = "_?_" + topx + "_?";
                }
                else if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    con = "_" + righty + "_?_?";
                }
                else
                {
                    con = "_?_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "LDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c + 1);
                chooseTile(r - 1, c);
                break;
            case TOP_LEFT:
                if (tiles[r - 1, c].hasBotEntrance() && tiles[r, c - 1].hasRightEntrance())
                {
                    topx = (int)tiles[r - 1, c].getBotEntrance().x;
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_" + topx + "_" + lefty + "_?";
                }
                else if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = (int)tiles[r - 1, c].getBotEntrance().x;
                    con = "_" + topx + "_?_?";
                }
                else if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con = "_?_" + lefty + "_?";
                }
                else
                {
                    con = "_?_?_?";
                }
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "LDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r, c - 1);
                chooseTile(r - 1, c);
                break;
            case LEFT_T:
                if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con += "_" + lefty;
                }
                else
                {
                    con += "_?";
                }

                if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r + 1, c].getTopEntrance().x)];
                    con += "_" + botx;
                }
                else
                {
                    con += "_?";
                }

                if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    con += "_" + topx;
                }
                else
                {
                    con += "_?";
                }
                                
                con += "_?";
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "TDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                chooseTile(r + 1, c);
                chooseTile(r, c - 1);
                break;
            case RIGHT_T:

                if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c + 1].getLeftEntrance().y)];
                    con += "_" + righty;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = (int)tiles[r - 1, c].getBotEntrance().x;
                    con += "_" + topx;
                }
                else
                {
                    con += "_?";
                }
                
                if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = (int)tiles[r + 1, c].getTopEntrance().x;
                    con += "_" + botx;
                }
                else
                {
                    con += "_?";
                }
                
                con += "_?";
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "TDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                chooseTile(r + 1, c);
                chooseTile(r, c + 1);
                break;
            case TOP_T:
                if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r - 1, c].getBotEntrance().x)];
                    con += "_" + topx;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c - 1].getRightEntrance().y)];
                    con += "_" + lefty;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = exits[exits.Count - 1 - exits.IndexOf((int)tiles[r, c + 1].getLeftEntrance().y)];
                    con += "_" + righty;
                }
                else
                {
                    con += "_?";
                }
                con += "_?";
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "TDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                p = rotateClockwise(p);
                p = rotateClockwise(p);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                chooseTile(r, c + 1);
                chooseTile(r, c - 1);
                break;
            case BOT_T:
                if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = (int)tiles[r + 1, c].getTopEntrance().x;
                    con += "_" + botx;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    con += "_" + righty;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con += "_" + lefty;
                }
                else
                {
                    con += "_?";
                }
                con += "_?";
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "TDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r + 1, c);
                chooseTile(r, c + 1);
                chooseTile(r, c - 1);
                break;
            case CROSS:
                if (tiles[r + 1, c].hasTopEntrance())
                {
                    botx = (int)tiles[r + 1, c].getTopEntrance().x;
                    con += "_" + botx;
                }
                else
                {
                    con += "_?";
                }                
                if (tiles[r, c + 1].hasLeftEntrance())
                {
                    righty = (int)tiles[r, c + 1].getLeftEntrance().y;
                    con += "_" + righty;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r - 1, c].hasBotEntrance())
                {
                    topx = (int)tiles[r - 1, c].getBotEntrance().x;
                    con += "_" + topx;
                }
                else
                {
                    con += "_?";
                }
                if (tiles[r, c - 1].hasRightEntrance())
                {
                    lefty = (int)tiles[r, c - 1].getRightEntrance().y;
                    con += "_" + lefty;
                }
                else
                {
                    con += "_?";
                }
                con += "_?";
                possibleFiles = Directory.GetFiles(Application.dataPath + "/" + usingPath, "CrossDetail" + perTileDetail + con + ".txt");
                filePath = possibleFiles[Random.Range(0, possibleFiles.Length)]; p= getPathFromFile(filePath);
                tiles[r, c].setPath(p);
                tiles[r, c].setComplete(true);
                chooseTile(r - 1, c);
                chooseTile(r + 1, c);
                chooseTile(r, c + 1);
                chooseTile(r, c - 1);
                break;
            default:
                break;
        }
        tiles[r, c].setFileName(filePath);
        updateDisplayTexture(r, c);
    }
    private void updateDisplayTexture(int r, int c)
    {
        int scalar = 75 / perTileDetail;
        Texture2D temp = new Texture2D(perTileDetail * scalar, perTileDetail * scalar, TextureFormat.ARGB32, false);
        for(int i = 0; i < perTileDetail; i++)
        {
            for(int j = 0; j < perTileDetail; j++)
            {
                for (int k = 0; k < scalar; k++)
                {
                    for(int l = 0; l < scalar; l++)
                    {
                        if (tiles[r, c].getPath()[i, j] == 0)
                        {
                            temp.SetPixel(j * scalar + k, (scalar * perTileDetail) - (i * scalar + l) - 1, Color.black);
                        }
                        else
                        {
                            temp.SetPixel(j * scalar + k, (scalar * perTileDetail) - (i * scalar + l) - 1, Color.red);
                        }
                    }
                }                
            }
        }
        
        temp.Apply();
        displayTextures[r, c] = temp;
        displayTextures[r, c].Apply();
    }

    public void DropAreaGUI()
    {
        Event evt = Event.current;
        Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(drop_area, "Drop requested tile for row " + selectedRow + " column " + selectedColumn + " here");

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!drop_area.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object dragged_object in DragAndDrop.objectReferences)
                    {
                        if (ValidateBitVectors())
                        {
                            loadTile(dragged_object.name);
                        }
                    }
                }
                break;
        }
    }
    public void TileOptions()
    {
        tileWidth = Mathf.Clamp(EditorGUILayout.FloatField("Tile width(m)", tileWidth), 1, 1000);
        tileHeight = Mathf.Clamp(EditorGUILayout.FloatField("Tile height(m)", tileHeight), 1, 1000);
        tileDepth = Mathf.Clamp(EditorGUILayout.FloatField("Tile depth(m)", tileDepth), 1, 1000);
    }
    public void TextureArea()
    {
        EditorGUILayout.LabelField("Options for texturing");
        pathWidth = EditorGUILayout.FloatField("Path width", pathWidth);
        pathWidth = Mathf.Clamp(pathWidth, 0, Mathf.Min(tileWidth, tileHeight));
        sampled = EditorGUILayout.Toggle("Sample bezier curve", sampled);
        baseTextureResolution = EditorGUILayout.IntField("Base Texture Reolution", baseTextureResolution);
        baseTextureResolution = Mathf.ClosestPowerOfTwo(baseTextureResolution);
        baseTextureResolution = Mathf.Clamp(baseTextureResolution, 16, 2048);
       
        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = (Texture2D)EditorGUILayout.ObjectField("Texture " + i, textures[i], typeof(Texture), true);
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add texture"))
        {
            ArrayUtility.Add(ref textures, null);
        }
        if (GUILayout.Button("Remove texture"))
        {
            if (textures.Length > 0) ArrayUtility.RemoveAt(ref textures, textures.Length - 1);
        }
        GUILayout.EndHorizontal();
    }
    public void HeightmapArea()
    {
        heightmapResolution = Mathf.ClosestPowerOfTwo(EditorGUILayout.IntField("Heightmap resolution", heightmapResolution)) + 1;
		randomizeHeight = EditorGUILayout.Toggle ("Randomize heights", randomizeHeight);
		if (randomizeHeight) {
			bumpiness = Mathf.Clamp01 (EditorGUILayout.FloatField ("Bumpiness of randomized terrain", bumpiness));
			smooth = EditorGUILayout.Toggle ("Smooth heights", smooth);
			if (smooth) {
				smoothRadius = Mathf.Clamp (EditorGUILayout.IntField ("Smooth radius", smoothRadius), 1, heightmapResolution);
				smoothPass = Mathf.Clamp (EditorGUILayout.IntField ("Num smoothing passes", smoothPass), 1, 5);
			}
			calderas = EditorGUILayout.Toggle ("Caldera's inversion", calderas);
			if (calderas) {
				calderasC = Mathf.Clamp01 (EditorGUILayout.FloatField ("Height for inversion", calderasC));
			}
			terrace = EditorGUILayout.Toggle ("Terrace heightmap", terrace);
			if (terrace) {
				terraceWithValues =	EditorGUILayout.Toggle ("Use values", terraceWithValues);
				if (terraceWithValues)
					terraceWithInterval = false;
				else
					terraceWithInterval = true;
				terraceWithInterval = EditorGUILayout.Toggle ("Use interval", terraceWithInterval);
				if (terraceWithInterval)
					terraceWithValues = false;
				else
					terraceWithValues = true;
				if (terraceWithValues) {
					terraceValuesString = EditorGUILayout.TextField (terraceValuesString);
				}
				if (terraceWithInterval) {
					terraceInterval = Mathf.Clamp (EditorGUILayout.FloatField ("Terrace interval", terraceInterval), 0.01f, 1f);
				}
			}
		}
    }
	private void loadAllTiles(){
		for (int i = 0; i < mazeHeight; i++)
		{
			for (int j = 0; j < mazeWidth; j++)
			{
				string s = i * mazeHeight + j + " out of " + mazeHeight * mazeWidth + " tiles completed";
				float f = ((i * mazeHeight + j) / (float)(mazeHeight * mazeWidth));
				if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Creating tile", f)) {
					break;
				}
				tiles[i, j].instantiateTile("Assets" + saveLocation + "/Terrains/" + tilesName);
				if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Loading tile", f)) {
					break;
				}
				tiles[i, j].loadPathsFromPath();
					if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Setting tile values", f)) {
						break;
					}
				tiles[i, j].setPathWidth(pathWidth);
				tiles[i, j].setWidth(tileWidth);
				tiles[i, j].setHeight(tileHeight);
				tiles[i, j].setDepth(tileDepth);
				if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Texturing tile", f)) {
					break;
				}
				if (textures.Length > 0) tiles[i, j].createSplatMap(textures, baseTextureResolution, sampled, "Assets" + saveLocation + "/Terrains/" + tilesName);
				if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Creating heightmap", f)) {
					break;
				}
				tiles[i, j].createHeightMap(heightmapResolution, "Assets" + saveLocation + "/Terrains/" + tilesName);
				if (randomizeHeight) {
					if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Randomizing heights", f)) {
						break;
					}
					tiles [i, j].diamondSquares ("Assets" + saveLocation + "/Terrains/" + tilesName, bumpiness);
					if (smooth) {
						if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Smoothing heightmap", f)) {
							break;
						}
						for (int k = 0; k < smoothPass; k++) {
							tiles [i, j].smooth ("Assets" + saveLocation + "/Terrains/" + tilesName, smoothRadius);
						}
					}
					if (calderas) {
						if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Inverting heightmap", f)) {
							break;
						}
						tiles [i, j].calderas ("Assets" + saveLocation + "/Terrains/" + tilesName, calderasC);
					}
					if (terrace) {
						if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Terracing heightmap", f)) {
							break;
						}
						if (terraceWithValues) {
							tiles [i, j].terrace ("Assets" + saveLocation + "/Terrains/" + tilesName, terraceValues);
						}
						else
							tiles [i, j].terrace ("Assets" + saveLocation + "/Terrains/" + tilesName, terraceInterval);
					}
				}
				if (EditorUtility.DisplayCancelableProgressBar ("Loading tiles", s + " - Finishing tile", f)) {
					break;
				}
				tiles[i, j].createTile("Assets" + saveLocation + "/Terrains/" + tilesName);
			}
		}
		EditorUtility.ClearProgressBar ();
	}
	private void loadSelectedTile(int i, int j){
		tiles[i, j].instantiateTile("Assets" + saveLocation + "/Terrains/" + tilesName);
		tiles[i, j].loadPathsFromPath();
		tiles[i, j].setPathWidth(pathWidth);
		tiles[i, j].setWidth(tileWidth);
		tiles[i, j].setHeight(tileHeight);
		tiles[i, j].setDepth(tileDepth);
		if (textures.Length > 0) tiles[i, j].createSplatMap(textures, baseTextureResolution, sampled, "Assets" + saveLocation + "/Terrains/" + tilesName);
		tiles[i, j].createHeightMap(heightmapResolution, "Assets" + saveLocation + "/Terrains/" + tilesName);
		if (randomizeHeight) {
			tiles [i, j].diamondSquares ("Assets" + saveLocation + "/Terrains/" + tilesName, bumpiness);
			if (smooth) {
				for (int k = 0; k < smoothPass; k++) {
					tiles [i, j].smooth ("Assets" + saveLocation + "/Terrains/" + tilesName, smoothRadius);
				}
			}
			if (calderas) {
				tiles [i, j].calderas ("Assets" + saveLocation + "/Terrains/" + tilesName, calderasC);
			}
			if (terrace) {
				if (terraceWithValues) {
					tiles [i, j].terrace ("Assets" + saveLocation + "/Terrains/" + tilesName, terraceValues);				}
				else
					tiles [i, j].terrace ("Assets" + saveLocation + "/Terrains/" + tilesName, terraceInterval);
			}
		}
		tiles[i, j].createTile("Assets" + saveLocation + "/Terrains/" + tilesName);
	}
}

