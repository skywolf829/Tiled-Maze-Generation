using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Assets.Scripts.Editor
{
    class Tile
    {
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

        private GameObject terrain;
        private TerrainData terrainData;
        private Vector2 leftEntrance, rightEntrance, topEntrance, botEntrance;
        private string name;
        private int[,] path;
        private int row, column, tileDetail, tileType;
        private float width, height;
        private bool completed;

        public Tile()
        {
            completed = false;
            path = new int[0, 0];
            name = "Terrain";
            terrainData = new TerrainData();
        }
        public Tile(int r, int c, float w, float h, int detail)
        {
            row = r;
            column = c;
            width = w;
            height = h;
            tileDetail = detail;
            completed = false;
            name = "Terrain";
            path = new int[tileDetail, tileDetail];
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, "Assets/" + name + r + "_" + c + ".asset");
            AssetDatabase.SaveAssets();
        }

        public bool isCreated() { return completed; }
        public int getRow() { return row; }
        public int getCol() { return column; }
        public int getDetail() { return tileDetail; }
        public int getType() { return tileType; }
        public bool hasLeftEntrance() { return !(leftEntrance.x == 0 && leftEntrance.y == 0); }
        public bool hasRightEntrance() { return !(rightEntrance.x == 0 && rightEntrance.y == 0); }
        public bool hasBotEntrance() { return !(botEntrance.x == 0 && botEntrance.y == 0); }
        public bool hasTopEntrance() { return !(topEntrance.x == 0 && topEntrance.y == 0); }
        public Vector2 getRightEntrance() { return rightEntrance; }
        public Vector2 getLeftEntrance() { return leftEntrance; }
        public Vector2 getTopEntrance() { return topEntrance; }
        public Vector2 getBotEntrance() { return botEntrance; }

        public void setRow(int r) { row = r; }
        public void setCol(int c) { column = c; }
        public void setDetail(int d) { tileDetail = d; }
        public void setType(int t) { tileType = t; }
        public void setRightEntrance(Vector2 location) { rightEntrance = location; }
        public void setLeftEntrance(Vector2 location) { leftEntrance = location; }
        public void setTopEntrance(Vector2 location) { topEntrance = location; }
        public void setBotEntrance(Vector2 location) { botEntrance = location; }

        public void generatePath(int startx, int starty, int endx, int endy)
        {
            Stack<int[,]> pathArrays = new Stack<int[,]>();
            Stack<int[]> positions = new Stack<int[]>();
            List<int[]> solutions = new List<int[]>();
            /*
             * Deletes the edges of the tile from the solution set.
             */
            for (int y = 0; y < tileDetail; y++)
            {
                for (int x = 0; x < tileDetail; x++)
                {
                    if ((x == 0 || x == tileDetail - 1 || y == 0 || y == tileDetail - 1) && path[y, x] == 0)
                    {
                        path[y, x] = -1;
                    }
                }
            }

            path[starty, startx] = 1;
            path[endy, endx] = 0;
            pathArrays.Push(path);

            positions.Push(new int[] { startx, starty });

            while (positions.Peek()[0] != endx || positions.Peek()[1] != endy)
            {
                /*
                string a = "";
                for (int y = 0; y < tileDetail; y++)
                {
                    for (int x = 0; x < tileDetail; x++)
                    {
                        if (path[y, x] > 0)
                        {
                            a += "  # ";
                        }
                        else if(path[y, x] == 0)
                        {
                            a += "  0 ";
                        }
                        else
                        {
                            a += " -1 ";
                        }                     
                    }
                    Debug.Log(a);
                    a = "";
                }
                Debug.Log("BREAK");
                */
                solutions = new List<int[]>();
                path = (int[,])pathArrays.Peek().Clone();
                if (positions.Peek()[1] > 0 && path[positions.Peek()[1] - 1, positions.Peek()[0]] == 0)
                {
                    solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] - 1 });
                }
                if (positions.Peek()[1] < tileDetail - 1 && path[positions.Peek()[1] + 1, positions.Peek()[0]] == 0)
                {
                    solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
                }
                if (positions.Peek()[0] > 0 && path[positions.Peek()[1], positions.Peek()[0] - 1] == 0)
                {
                    solutions.Add(new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
                }
                if (positions.Peek()[0] < tileDetail - 1 && path[positions.Peek()[1], positions.Peek()[0] + 1] == 0)
                {
                    solutions.Add(new int[] { positions.Peek()[0] + 1, positions.Peek()[1] });
                }

                if (solutions.Count == 0)
                {
                    pathArrays.Pop();
                    path = pathArrays.Pop();
                    path[positions.Peek()[1], positions.Peek()[0]] = -1;
                    pathArrays.Push(path);
                    positions.Pop();
                }
                else
                {
                    foreach (int[] sol in solutions)
                    {
                        if (sol[1] == endy && sol[0] == endx)
                        {
                            path[endy, endx] = 1;
                            positions.Push(new int[] { endx, endy });
                        }
                    }

                    if (positions.Peek()[0] != endx || positions.Peek()[1] != endy)
                    {
                        int[][] solutionArray = solutions.ToArray();
                        int[] chosen = solutionArray[UnityEngine.Random.Range(0, solutions.Count)];
                        path[chosen[1], chosen[0]] = 1;
                        positions.Push(new int[] { chosen[0], chosen[1] });
                        foreach (int[] sol in solutions)
                        {
                            if (path[sol[1], sol[0]] != 1)
                            {
                                path[sol[1], sol[0]] = -1;
                            }
                        }
                    }
                    pathArrays.Push(path);
                }
            }
            path = pathArrays.Pop();
            completed = true;
        }
        public void adjoinPath(int startx, int starty)
        {
            Stack<int[,]> pathArrays = new Stack<int[,]>();
            Stack<int[]> positions = new Stack<int[]>();
            List<int[]> solutions = new List<int[]>();
            /*
             * Reset the -1's to available spots and 2s to 1s
             */
            for (int y = 0; y < tileDetail; y++)
            {
                for (int x = 0; x < tileDetail; x++)
                {
                    if (path[y, x] == -1)
                    {
                        path[y, x] = 0;
                    }
                    else if (path[y, x] == 2)
                    {
                        path[y, x] = 1;
                    }
                }
            }


            /*
             * Deletes the edges of the tile from the solution set.
             */
            for (int y = 0; y < tileDetail; y++)
            {
                for (int x = 0; x < tileDetail; x++)
                {
                    if ((x == 0 || x == tileDetail - 1 || y == 0 || y == tileDetail - 1) && path[y, x] == 0)
                    {
                        path[y, x] = -1;
                    }
                }
            }

            path[starty, startx] = 2;
            pathArrays.Push(path);

            positions.Push(new int[] { startx, starty });

            while (pathArrays.Peek()[positions.Peek()[1], positions.Peek()[0]] != 1)
            {

                solutions = new List<int[]>();
                path = (int[,])pathArrays.Peek().Clone();
                if (positions.Peek()[1] > 0 && (path[positions.Peek()[1] - 1, positions.Peek()[0]] == 0 || path[positions.Peek()[1] - 1, positions.Peek()[0]] == 1))
                {
                    solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] - 1 });
                }
                if (positions.Peek()[1] < tileDetail - 1 && (path[positions.Peek()[1] + 1, positions.Peek()[0]] == 0 || path[positions.Peek()[1] + 1, positions.Peek()[0]] == 1))
                {
                    solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
                }
                if (positions.Peek()[0] > 0 && (path[positions.Peek()[1], positions.Peek()[0] - 1] == 0 || path[positions.Peek()[1], positions.Peek()[0] - 1] == 1))
                {
                    solutions.Add(new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
                }
                if (positions.Peek()[0] < tileDetail - 1 && (path[positions.Peek()[1], positions.Peek()[0] + 1] == 0 || path[positions.Peek()[1], positions.Peek()[0] + 1] == 1))
                {
                    solutions.Add(new int[] { positions.Peek()[0] + 1, positions.Peek()[1] });
                }

                if (solutions.Count == 0)
                {
                    pathArrays.Pop();
                    path = pathArrays.Pop();
                    path[positions.Peek()[1], positions.Peek()[0]] = -1;
                    pathArrays.Push(path);
                    positions.Pop();
                }
                else
                {
                    foreach (int[] sol in solutions)
                    {
                        if (path[sol[1], sol[0]] == 1)
                        {
                            positions.Push(new int[] { sol[0], sol[1] });
                        }
                    }

                    if (path[positions.Peek()[1], positions.Peek()[0]] != 1)
                    {
                        int[][] solutionArray = solutions.ToArray();
                        int[] chosen = solutionArray[UnityEngine.Random.Range(0, solutions.Count)];
                        path[chosen[1], chosen[0]] = 2;
                        positions.Push(new int[] { chosen[0], chosen[1] });
                        foreach (int[] sol in solutions)
                        {
                            if (path[sol[1], sol[0]] != 2)
                            {
                                path[sol[1], sol[0]] = -1;
                            }
                        }
                    }
                    pathArrays.Push(path);
                }
            }
            path =  pathArrays.Pop();
            cleanPath();
        }
        public void cleanPath()
        {
            bool finished = false;
            while (!finished)
            {
                finished = true;
                for (int r = 1; r < tileDetail - 1; r++)
                {
                    for (int c = 1; c < tileDetail - 1; c++)
                    {
                        if (path[r, c] == 1 && AliveNeighbors(path, r, c) < 2)
                        {
                            finished = false;
                            path[r, c] = 0;
                        }
                        if (path[r, c] < 1 && AliveNeighbors(path, r, c) > 3)
                        {
                            finished = false;
                            path[r, c] = 1;
                        }
                    }
                }
            }
        }
        public int AliveNeighbors(int[,] pathArray, int r, int c)
        {
            int alive = 0;
            if (r > 0)
            {
                if (pathArray[r - 1, c] > 0)
                {
                    alive++;
                }
            }
            if (r < tileDetail - 1)
            {
                if (pathArray[r + 1, c] > 0)
                {
                    alive++;
                }
            }
            if (c > 0)
            {
                if (pathArray[r, c - 1] > 0)
                {
                    alive++;
                }
            }
            if (c < tileDetail - 1)
            {
                if (pathArray[r, c + 1] > 0)
                {
                    alive++;
                }
            }

            return alive;
        }

        public void createHeightMap(int heightMapResolution)
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + name + row + "_" + column + ".asset", typeof(TerrainData));
            float[,] heightmap = new float[heightMapResolution, heightMapResolution];

            terrainData.SetHeights(0, 0, heightmap);
        }
        public void createSplatMap(Texture2D[] textures, int textureResolution)
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + name + row + "_" + column + ".asset", typeof(TerrainData));
            terrainData.baseMapResolution = textureResolution;
            terrainData.alphamapResolution = textureResolution;

            SplatPrototype[] splats = new SplatPrototype[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                splats[i] = new SplatPrototype();
                splats[i].texture = textures[i];
            }
            terrainData.splatPrototypes = splats;
            float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

            for (int r = 0; r < tileDetail; r++)
            {
                for (int c = 0; c < tileDetail; c++)
                {
                    if (path[r, c] > 0)
                    {
                        for (int y = Mathf.FloorToInt(((float)r / tileDetail) * textureResolution);
                            y < Mathf.FloorToInt(((float)(r + 1) / tileDetail) * textureResolution); y++)
                        {
                            for (int x = Mathf.FloorToInt(((float)c / tileDetail) * textureResolution);
                                x < Mathf.FloorToInt(((float)(c + 1) / tileDetail) * textureResolution); x++)
                            {
                                splatmapData[y, x, 0] = 1.0f;
                            }
                        }
                    }

                }
            }
            terrainData.SetAlphamaps(0, 0, splatmapData);
            AssetDatabase.SaveAssets();
        }
        public void createObjects()
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" +  name + row + "_" + column + ".asset", typeof(TerrainData));

            AssetDatabase.CreateAsset(terrainData, "Assets/" + name + row + "_" + column + ".asset");
            AssetDatabase.SaveAssets();
        }

        public void createTile()
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" + name + row + "_" + column + ".asset", typeof(TerrainData));
            terrainData.size = new Vector3(width, 1, height);
            terrainData.name = name + row + "_" + column;
            GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = name + row + "_" + column;
            terrain.transform.position = new Vector3(column * width, 0, row * -height);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
