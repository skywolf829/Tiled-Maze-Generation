using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Assets.Scripts.Editor;

public class TilePathGenerator : MonoBehaviour {

    private int maxNumTilesPerType;
    private int numPossibleExits;
    private int detail;

    private string saveLocation;

	public TilePathGenerator()
    {
        detail = 5;
        maxNumTilesPerType = 3;
        numPossibleExits = 3;
        saveLocation = "";
    }

    public void setMaxNumTilesPerType(int t) { maxNumTilesPerType = t; }
    public void setNumPossibleExits(int x) { numPossibleExits = x; }
    public void setDetail(int d) { detail = d; }
    public void setSaveLocation(string s) { saveLocation = s; }

    public int getDetail() { return detail; }
    public int getNumPossibleExits() { return numPossibleExits; }
    public int getMaxNumTilesPerType() { return maxNumTilesPerType; }
    public string getSaveLocation() { return saveLocation; }

    public void beginGenerator()
    {
        float startingSpot = (float)(detail - 2) / (float)numPossibleExits;
        List<int> exits = new List<int>();       
        for(int i = 1; i <= (numPossibleExits / 2); i++)
        {
            exits.Add((int)(i * startingSpot));
        }
        int length = exits.Count;
        if(numPossibleExits % 2 != 0)
        {
            exits.Add(detail / 2);
        }
        for (int i = length - 1; i >= 0; i--)
        {
            exits.Add(detail - 1 - exits[i]);
        }
        saveLocation += "/" + detail + "_" + numPossibleExits;
        for(int i = 0; i < exits.Count; i++)
        {
            for (int j = 0; j < exits.Count; j++)
            {
                for (int a = 0; a < exits.Count; a++)
                {
                    for (int b = 0; b < exits.Count; b++)
                    {
                        for (int k = 0; k < maxNumTilesPerType; k++)
                        {                          
                            int[,] path = new int[detail, detail];
                            path = generatePath(path, 0, exits[i], detail - 1, exits[j]);
                            path = adjoinPath(path, exits[a], detail - 1);
                            path = adjoinPath(path, exits[b], 0);
                            string s = "";
                            for (int l = 0; l < path.GetLength(0); l++)
                            {
                                for (int m = 0; m < path.GetLength(1); m++)
                                {
                                    s += path[l, m];
                                }
                            }
                            File.WriteAllText(Application.dataPath + "/" + saveLocation + "/CrossDetail" + detail + "_" + exits[a] + "_" + exits[j] + "_" + exits[b] + "_" + exits[i] + "_" + k + ".txt", s);
                        }
                    }
                    for (int k = 0; k < maxNumTilesPerType; k++)
                    {
                        int[,] path = new int[detail, detail];
                        path = generatePath(path, 0, exits[i], detail - 1, exits[j]);
                        path = adjoinPath(path, exits[a], detail - 1);                        
                        string s = "";
                        for (int l = 0; l < path.GetLength(0); l++)
                        {
                            for (int m = 0; m < path.GetLength(1); m++)
                            {
                                s += path[l, m];
                            }
                        }
                        File.WriteAllText(Application.dataPath + "/" + saveLocation + "/TDetail" + detail + "_" + exits[a] + "_" + exits[j] + "_" + exits[i] + "_" + k + ".txt", s);
                    }
                }
                for(int k = 0; k < maxNumTilesPerType; k++)
                {
                    int[,] path = new int[detail, detail];
                    path = generatePath(path, 0, exits[i], detail - 1, exits[j]);
                    string s = "";
                    for (int l = 0; l < path.GetLength(0); l++)
                    {
                        for (int m = 0; m < path.GetLength(1); m++)
                        {
                            s += path[l, m];
                        }
                    }
                    File.WriteAllText(Application.dataPath + "/" + saveLocation + "/ThroughDetail" + detail + "_" + exits[j] + "_" + exits[i] + "_" + k + ".txt", s);
                }
                for (int k = 0; k < maxNumTilesPerType; k++)
                {
                    int[,] path = new int[detail, detail];
                    path = generatePath(path, 0, exits[i], exits[j], 0);
                    string s = "";
                    for (int l = 0; l < path.GetLength(0); l++)
                    {
                        for (int m = 0; m < path.GetLength(1); m++)
                        {
                            s += path[l, m];
                        }
                    }
                    File.WriteAllText(Application.dataPath + "/" + saveLocation + "/LDetail" + detail + "_" + exits[j] + "_" + exits[i] + "_" + k + ".txt", s);
                }
            }
            for (int k = 0; k < maxNumTilesPerType; k++)
            {
                int[,] path = new int[detail, detail];
                path = generatePath(path, 0, exits[i], Random.Range(1, detail - 1), Random.Range(1, detail - 1));
                string s = "";
                for (int l = 0; l < path.GetLength(0); l++)
                {
                    for (int m = 0; m < path.GetLength(1); m++)
                    {
                        s += path[l, m];
                    }
                }
                File.WriteAllText(Application.dataPath + "/" + saveLocation + "/EndDetail" + detail + "_" + exits[i] + "_" + k + ".txt", s);
            }
        }


    }
    private void generateTileArray(int numExits)
    {

    }
    public int[,] generatePath(int[,] path, int startx, int starty, int endx, int endy)
    {
        Stack<int[,]> pathArrays = new Stack<int[,]>();
        Stack<int[]> positions = new Stack<int[]>();
        List<int[]> solutions = new List<int[]>();
        /*
         * Deletes the edges of the tile from the solution set.
         */
        for (int y = 0; y < detail; y++)
        {
            for (int x = 0; x < detail; x++)
            {
                if ((x == 0 || x == detail - 1 || y == 0 || y == detail - 1) && path[y, x] == 0)
                {
                    path[y, x] = -1;
                }
            }
        }        
        path[starty, startx] = 1;
        path[endy, endx] = 0;
        pathArrays.Push(path);
        List<Vector2> jaggedPath = new List<Vector2>();
        jaggedPath.Add(new Vector2(startx, starty));
        positions.Push(new int[] { startx, starty });

        while (positions.Peek()[0] != endx || positions.Peek()[1] != endy)
        {
            solutions = new List<int[]>();
            path = (int[,])pathArrays.Peek().Clone();
            if (positions.Peek()[1] > 0 && path[positions.Peek()[1] - 1, positions.Peek()[0]] == 0)
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] - 1 });
            }
            if (positions.Peek()[1] < detail - 1 && path[positions.Peek()[1] + 1, positions.Peek()[0]] == 0)
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
            }
            if (positions.Peek()[0] > 0 && path[positions.Peek()[1], positions.Peek()[0] - 1] == 0)
            {
                solutions.Add(new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
            }
            if (positions.Peek()[0] < detail - 1 && path[positions.Peek()[1], positions.Peek()[0] + 1] == 0)
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
                jaggedPath.RemoveAt(jaggedPath.Count - 1);
            }
            else
            {
                foreach (int[] sol in solutions)
                {
                    if (sol[1] == endy && sol[0] == endx)
                    {
                        path[endy, endx] = 1;
                        positions.Push(new int[] { endx, endy });
                        jaggedPath.Add(new Vector2(endx, endy));
                    }
                }

                if (positions.Peek()[0] != endx || positions.Peek()[1] != endy)
                {
                    int[][] solutionArray = solutions.ToArray();
                    int[] chosen = solutionArray[UnityEngine.Random.Range(0, solutions.Count)];
                    path[chosen[1], chosen[0]] = 1;
                    positions.Push(new int[] { chosen[0], chosen[1] });
                    jaggedPath.Add(new Vector2(chosen[0], chosen[1]));
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
        /*
         * Reset the -1's to available spots and 2s to 1s
         */
        for (int y = 0; y < detail; y++)
        {
            for (int x = 0; x < detail; x++)
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
        return path;
    }
    public int[,] adjoinPath(int[,] path, int startx, int starty)
    {
        
        Stack<int[,]> pathArrays = new Stack<int[,]>();
        Stack<int[]> positions = new Stack<int[]>();
        List<int[]> solutions = new List<int[]>();
        /*
         * Deletes the edges of the tile from the solution set.
         */
        for (int y = 0; y < detail; y++)
        {
            for (int x = 0; x < detail; x++)
            {
                if ((x == 0 || x == detail - 1 || y == 0 || y == detail - 1) && path[y, x] == 0)
                {
                    path[y, x] = -1;
                }
            }
        }

        path[starty, startx] = 2;
        pathArrays.Push(path);
        List<Vector2> jaggedPath = new List<Vector2>();
        jaggedPath.Add(new Vector2(startx, starty));
        positions.Push(new int[] { startx, starty });

        while (pathArrays.Peek()[positions.Peek()[1], positions.Peek()[0]] != 1)
        {

            solutions = new List<int[]>();
            path = (int[,])pathArrays.Peek().Clone();
            if (positions.Peek()[1] > 0 && (path[positions.Peek()[1] - 1, positions.Peek()[0]] == 0 || path[positions.Peek()[1] - 1, positions.Peek()[0]] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] - 1 });
            }
            if (positions.Peek()[1] < detail - 1 && (path[positions.Peek()[1] + 1, positions.Peek()[0]] == 0 || path[positions.Peek()[1] + 1, positions.Peek()[0]] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0], positions.Peek()[1] + 1 });
            }
            if (positions.Peek()[0] > 0 && (path[positions.Peek()[1], positions.Peek()[0] - 1] == 0 || path[positions.Peek()[1], positions.Peek()[0] - 1] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0] - 1, positions.Peek()[1] });
            }
            if (positions.Peek()[0] < detail - 1 && (path[positions.Peek()[1], positions.Peek()[0] + 1] == 0 || path[positions.Peek()[1], positions.Peek()[0] + 1] == 1))
            {
                solutions.Add(new int[] { positions.Peek()[0] + 1, positions.Peek()[1] });
            }

            if (solutions.Count == 0)
            {
                pathArrays.Pop();
                path = pathArrays.Pop();
                path[positions.Peek()[1], positions.Peek()[0]] = -1;
                pathArrays.Push(path);
                jaggedPath.RemoveAt(jaggedPath.Count - 1);
                positions.Pop();
            }
            else
            {
                foreach (int[] sol in solutions)
                {
                    if (path[sol[1], sol[0]] == 1)
                    {
                        positions.Push(new int[] { sol[0], sol[1] });
                        jaggedPath.Add(new Vector2(sol[0], sol[1]));
                    }
                }

                if (path[positions.Peek()[1], positions.Peek()[0]] != 1)
                {
                    int[][] solutionArray = solutions.ToArray();
                    int[] chosen = solutionArray[UnityEngine.Random.Range(0, solutions.Count)];
                    path[chosen[1], chosen[0]] = 2;
                    positions.Push(new int[] { chosen[0], chosen[1] });
                    jaggedPath.Add(new Vector2(chosen[0], chosen[1]));
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
        path = pathArrays.Pop();
        /*
         * Reset the -1's to available spots and 2s to 1s
         */
        for (int y = 0; y < detail; y++)
        {
            for (int x = 0; x < detail; x++)
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
        return path;
    }   
}
