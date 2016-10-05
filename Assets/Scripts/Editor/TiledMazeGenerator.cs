﻿using UnityEngine;
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

    private int selectedRow, selectedColumn;
    private Texture2D[,] displayTextures = new Texture2D[4, 4];
    private Tile[,] tiles = new Tile[4, 4];
    private int[,] tiling = new int[4, 4];
    private string[,] tilingFiles = new string[4, 4];

    private int mazeWidth = 4, mazeHeight = 4;
    private int[] VEBP = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, HEBP = new int[] { 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
    private string VEBPString = "111111111111", HEBPString = "000110000001";

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
        if(perTileDetail % 2 == 0)
        {
            perTileDetail++;
        }
        if(oldDetail != perTileDetail)
        {
            updateArrays();
        }
        numPossibleExits = Mathf.Clamp(EditorGUILayout.IntField("Possible exits", numPossibleExits), 1, (perTileDetail - 2));

        if (GUILayout.Button("Generate all tiles")) {
            if (ValidateSaveLocation())
            {                     
                TilePathGenerator tpg = new TilePathGenerator();
                tpg.setDetail(perTileDetail);
                tpg.setSaveLocation(saveLocation);
                tpg.setNumPossibleExits(numPossibleExits);
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

        if(oldWidth != mazeWidth || oldHeight != mazeHeight)
        {
            updateArrays();
        } 

        if(GUILayout.Button("Randomize tiles") && ValidateBitVectors())
        {
            randomizeTiles();
        }
        GUILayout.BeginVertical();
        for(int i  = 0; i < mazeHeight; i++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(1));
            for(int j = 0; j < mazeWidth; j++)
            {
                if (GUILayout.Button(displayTextures[i, j], "Label")) 
                {
                    selectedColumn = j;
                    selectedRow = i;
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        DropAreaGUI();


        GUILayout.EndScrollView();
    }

    private bool ValidateSaveLocation()
    {
        if (saveLocation == string.Empty) saveLocation = "GeneratedMazeTiles/";
        string pathToCheck = Application.dataPath + "/" + saveLocation + "/" + perTileDetail + "_" + numPossibleExits;
        if (Directory.Exists(saveLocation) == false)
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
                tiles[r, c] = new Tile();
                tiles[r, c].setDetail(perTileDetail);
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
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        if (t == BOT && s.Contains("End"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == BOT_LEFT  && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == BOT_RIGHT && s.Contains("L"))
        {
            int[,] p = getPathFromFile(s);
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
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == LEFT_T  && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == RIGHT_T && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
            tiles[selectedRow, selectedColumn].setPath(p);
            tiles[selectedRow, selectedColumn].setComplete(true);
        }
        else if (t == TOP_T && s.Contains("T"))
        {
            int[,] p = getPathFromFile(s);
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
        updateDisplayTexture(selectedRow, selectedColumn);
    }
    private void randomizeTiles()
    {
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
                p = getPathFromFile(possibleFiles[Random.Range(0, possibleFiles.Length)]);
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
}

