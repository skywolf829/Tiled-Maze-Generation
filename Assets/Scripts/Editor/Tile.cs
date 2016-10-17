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
        private const float DIVISION_THRESHOLD = -0.99f;
        private const float MINIMUM_SQR_DISTANCE = 0.01f;
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

        private TerrainData terrainData;
        private Vector2 leftEntrance, rightEntrance, topEntrance, botEntrance;
        private string name;
        private string fileName;
        private int[,] path;
        private List<List<Vector2>> paths;
        private List<Vector2> controlPoints;
        private int segmentsPerCurve = 20;
        private int row, column, tileDetail, tileType;
        private float width, height, depth, heightmapRes;
        private float pathWidth;
        private bool completed;

        private int curveCount;

        public Tile()
        {
            completed = false;
            path = new int[0, 0];
            name = "";
            controlPoints = new List<Vector2>();
            terrainData = new TerrainData();
            paths = new List<List<Vector2>>();
        }
        public Tile(int r, int c, float w, float h, float d, int detail)
        {
            row = r;
            column = c;
            width = w;
            height = h;
            depth = d;
            tileDetail = detail;
            completed = false;
            name = r +"_" + c;
            path = new int[tileDetail, tileDetail];
            controlPoints = new List<Vector2>();
            //terrainData = new TerrainData();
            paths = new List<List<Vector2>>();
            //AssetDatabase.CreateAsset(terrainData, "Assets/" + name + r + "_" + c + ".asset");
            //AssetDatabase.SaveAssets();
        }
        public void instantiateTile(string s)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, s + "/" + name + ".asset");
            AssetDatabase.SaveAssets();
        }
        public bool isCreated() { return completed; }
        public int getRow() { return row; }
        public int getCol() { return column; }
        public string getFileName() { return fileName; }
        public int getDetail() { return tileDetail; }
        public int getType() { return tileType; }
        public bool hasLeftEntrance() { return !(leftEntrance.x == 0 && leftEntrance.y == 0); }
        public bool hasRightEntrance() { return !(rightEntrance.x == 0 && rightEntrance.y == 0); }
        public bool hasBotEntrance() { return !(botEntrance.x == 0 && botEntrance.y == 0); }
        public bool hasTopEntrance() { return !(topEntrance.x == 0 && topEntrance.y == 0); }
        public int numOfEntrances()
        {
            int x = 0;
            if(!(leftEntrance.x == 0 && leftEntrance.y == 0))
            {
                x++;
            }
            if (!(rightEntrance.x == 0 && rightEntrance.y == 0))
            {
                x++;
            }
            if (!(botEntrance.x == 0 && botEntrance.y == 0))
            {
                x++;
            }
            if (!(topEntrance.x == 0 && topEntrance.y == 0))
            {
                x++;
            }
            return x;
        }
        public Vector2 getRightEntrance() { return rightEntrance; }
        public Vector2 getLeftEntrance() { return leftEntrance; }
        public Vector2 getTopEntrance() { return topEntrance; }
        public Vector2 getBotEntrance() { return botEntrance; }
        public int[,] getPath() { return path; }

        public void setRow(int r) { row = r; }
        public void setCol(int c) { column = c; }
        public void setDetail(int d) { tileDetail = d; }
        public void setType(int t) { tileType = t; }
        public void setWidth(float f) { width = f; }
        public void setHeight(float f) { height = f; }
        public void setDepth(float f) { depth = f; }
        public void setPathWidth(float p) { pathWidth = p; }
        public void setFileName(string s) { fileName = s; }
        public void setName(string n) { name = n; }
        public void setRightEntrance(Vector2 location) { rightEntrance = location; }
        public void setLeftEntrance(Vector2 location) { leftEntrance = location; }
        public void setTopEntrance(Vector2 location) { topEntrance = location; }
        public void setBotEntrance(Vector2 location) { botEntrance = location; }
        public void setPath(int[,] p) { path = p; updateEntrances(); }
        public void setComplete(bool b) { completed = b; }
        private void updateEntrances()
        {
            topEntrance = leftEntrance = rightEntrance = botEntrance = new Vector2(0, 0);            
            for(int r = 0; r < tileDetail; r++)
            {
                for(int c = 0; c < tileDetail; c++)
                {                                       
                    if (r == 0 && path[r, c] == 1)
                    {
                        topEntrance = new Vector2(c, r);
                    }
                    if(r == tileDetail - 1 && path[r, c] == 1)
                    {
                        botEntrance = new Vector2(c, r);
                    }
                    if(c == 0 && path[r, c] == 1)
                    {
                        leftEntrance = new Vector2(c, r);
                    }
                    if(c == tileDetail - 1 && path[r, c] == 1)
                    {
                        rightEntrance = new Vector2(c, r);
                    }
                }
            }
        }
        
        private void Interpolate(List<Vector2> segmentPoints, float scale)
        {
            controlPoints.Clear();

            if (segmentPoints.Count < 2)
            {
                return;
            }

            for (int i = 0; i < segmentPoints.Count; i++)
            {
                if (i == 0) // is first
                {
                    Vector2 p1 = segmentPoints[i];
                    Vector2 p2 = segmentPoints[i + 1];

                    Vector2 tangent = (p2 - p1);
                    Vector2 q1 = p1 + scale * tangent;

                    controlPoints.Add(p1);
                    controlPoints.Add(q1);
                }
                else if (i == segmentPoints.Count - 1) //last
                {
                    Vector2 p0 = segmentPoints[i - 1];
                    Vector2 p1 = segmentPoints[i];
                    Vector2 tangent = (p1 - p0);
                    Vector2 q0 = p1 - scale * tangent;

                    controlPoints.Add(q0);
                    controlPoints.Add(p1);
                }
                else
                {
                    Vector2 p0 = segmentPoints[i - 1];
                    Vector2 p1 = segmentPoints[i];
                    Vector2 p2 = segmentPoints[i + 1];
                    Vector2 tangent = (p2 - p0).normalized;
                    Vector2 q0 = p1 - scale * tangent * (p1 - p0).magnitude;
                    Vector2 q1 = p1 + scale * tangent * (p2 - p1).magnitude;

                    controlPoints.Add(q0);
                    controlPoints.Add(p1);
                    controlPoints.Add(q1);
                }
            }

            curveCount = (controlPoints.Count - 1) / 3;
        }
        private void SamplePoints(List<Vector2> sourcePoints, float minSqrDistance, float maxSqrDistance, float scale)
        {
            if (sourcePoints.Count < 2)
            {
                Debug.Log("issue");
                return;
            }

            Stack<Vector2> samplePoints = new Stack<Vector2>();

            samplePoints.Push(sourcePoints[0]);

            Vector2 potentialSamplePoint = sourcePoints[1];

            int i = 2;

            for (i = 2; i < sourcePoints.Count; i++)
            {
                if (
                    ((potentialSamplePoint - sourcePoints[i]).sqrMagnitude > minSqrDistance) &&
                    ((samplePoints.Peek() - sourcePoints[i]).sqrMagnitude > maxSqrDistance))
                {
                    samplePoints.Push(potentialSamplePoint);
                }

                potentialSamplePoint = sourcePoints[i];
            }

            //now handle last bit of curve
            Vector2 p1 = samplePoints.Pop(); //last sample point
            if (samplePoints.Count > 0)
            {
                Vector2 p0 = samplePoints.Peek(); //second last sample point
                Vector2 tangent = (p0 - potentialSamplePoint).normalized;
                float d2 = (potentialSamplePoint - p1).magnitude;
                float d1 = (p1 - p0).magnitude;
                p1 = p1 + tangent * ((d1 - d2) / 2);

                samplePoints.Push(p1);
                samplePoints.Push(potentialSamplePoint);
                Interpolate(new List<Vector2>(samplePoints), scale);
            }
            
        }
        private Vector2 CalculateBezierPoint(int curveIndex, float t)
        {
            int nodeIndex = curveIndex * 3;

            Vector2 p0 = controlPoints[nodeIndex];
            Vector2 p1 = controlPoints[nodeIndex + 1];
            Vector2 p2 = controlPoints[nodeIndex + 2];
            Vector2 p3 = controlPoints[nodeIndex + 3];

            return CalculateBezierPoint(t, p0, p1, p2, p3);
        }
        private List<Vector2> GetDrawingPoints0()
        {
            List<Vector2> drawingPoints = new List<Vector2>();

            for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
            {
                if (curveIndex == 0) //Only do this for the first end point. 
                                     //When i != 0, this coincides with the 
                                     //end point of the previous segment,
                {
                    drawingPoints.Add(CalculateBezierPoint(curveIndex, 0));
                }

                for (int j = 1; j <= segmentsPerCurve; j++)
                {
                    float t = j / (float)segmentsPerCurve;
                    drawingPoints.Add(CalculateBezierPoint(curveIndex, t));
                }
            }

            return drawingPoints;
        }
        private List<Vector2> GetDrawingPoints1()
        {
            List<Vector2> drawingPoints = new List<Vector2>();

            for (int i = 0; i < controlPoints.Count - 3; i += 3)
            {
                Vector2 p0 = controlPoints[i];
                Vector2 p1 = controlPoints[i + 1];
                Vector2 p2 = controlPoints[i + 2];
                Vector2 p3 = controlPoints[i + 3];

                if (i == 0) //only do this for the first end point. When i != 0, this coincides with the end point of the previous segment,
                {
                    drawingPoints.Add(CalculateBezierPoint(0, p0, p1, p2, p3));
                }

                for (int j = 1; j <= segmentsPerCurve; j++)
                {
                    float t = j / (float)segmentsPerCurve;
                    drawingPoints.Add(CalculateBezierPoint(t, p0, p1, p2, p3));
                }
            }

            return drawingPoints;
        }
        private List<Vector2> GetDrawingPoints2()
        {
            List<Vector2> drawingPoints = new List<Vector2>();

            for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
            {
                List<Vector2> bezierCurveDrawingPoints = FindDrawingPoints(curveIndex);

                if (curveIndex != 0)
                {
                    //remove the fist point, as it coincides with the last point of the previous Bezier curve.
                    bezierCurveDrawingPoints.RemoveAt(0);
                }

                drawingPoints.AddRange(bezierCurveDrawingPoints);
            }

            return drawingPoints;
        }
        private List<Vector2> FindDrawingPoints(int curveIndex)
        {
            List<Vector2> pointList = new List<Vector2>();

            Vector2 left = CalculateBezierPoint(curveIndex, 0);
            Vector2 right = CalculateBezierPoint(curveIndex, 1);

            pointList.Add(left);
            pointList.Add(right);

            FindDrawingPoints(curveIndex, 0, 1, pointList, 1);

            return pointList;
        }
        private int FindDrawingPoints(int curveIndex, float t0, float t1,
            List<Vector2> pointList, int insertionIndex)
        {
            Vector2 left = CalculateBezierPoint(curveIndex, t0);
            Vector2 right = CalculateBezierPoint(curveIndex, t1);

            if ((left - right).sqrMagnitude < MINIMUM_SQR_DISTANCE)
            {
                return 0;
            }

            float tMid = (t0 + t1) / 2;
            Vector2 mid = CalculateBezierPoint(curveIndex, tMid);

            Vector2 leftDirection = (left - mid).normalized;
            Vector2 rightDirection = (right - mid).normalized;

            if (Vector2.Dot(leftDirection, rightDirection) > DIVISION_THRESHOLD || Mathf.Abs(tMid - 0.5f) < 0.0001f)
            {
                int pointsAddedCount = 0;

                pointsAddedCount += FindDrawingPoints(curveIndex, t0, tMid, pointList, insertionIndex);
                pointList.Insert(insertionIndex + pointsAddedCount, mid);
                pointsAddedCount++;
                pointsAddedCount += FindDrawingPoints(curveIndex, tMid, t1, pointList, insertionIndex + pointsAddedCount);

                return pointsAddedCount;
            }

            return 0;
        }
        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0; //first term

            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term

            return p;
        }

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
            paths.Add(jaggedPath);
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
            paths.Add(jaggedPath);

            /*
             * Split the path that was intercepted
             */
            for(int i = 0; i < paths.Count - 1; i++)
            {
                if(paths[i].Contains(jaggedPath[jaggedPath.Count - 1]) && paths[i].IndexOf(jaggedPath[jaggedPath.Count - 1]) != paths[i].Count - 1)
                {
                    List<Vector2> p1 = new List<Vector2>();
                    List<Vector2> p2 = new List<Vector2>();
                    int spot = paths[i].IndexOf(jaggedPath[jaggedPath.Count - 1]);
                    for(int j = 0; j < paths[i].Count; j++)
                    {
                        if (j < spot)
                        {
                            p1.Add(paths[i][j]);
                        }
                        else if (j > spot)
                        {
                            p2.Add(paths[i][j]);
                        }
                        else
                        {
                            p1.Add(paths[i][j]);
                            p2.Add(paths[i][j]);
                        }
                    }
                    paths.Remove(paths[i]);
                    paths.Insert(i, p1);
                    paths.Insert(i + 1, p2);
                    i = paths.Count;
                    
                }
            }
            
            
        }
        public void cleanPath()
        {
            Vector2 pos = paths[0][paths[0].Count - 1];
            while (AliveNeighbors(path, (int)pos.y, (int)pos.x) < 2)
            {
                path[(int)pos.y, (int)pos.x] = 0;
                paths[0].RemoveAt(paths[0].Count - 1);
                pos = paths[0][paths[0].Count - 1];
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
        
        public void loadPathsFromPath()
        {
            paths = new List<List<Vector2>>();
            Vector2 start = new Vector2(0, 0);
            List<Vector2> intersections = new List<Vector2>();
            for(int y = 0; y < tileDetail; y++)
            {
                for(int x = 0; x < tileDetail; x++)
                {
                    if(AliveNeighbors(path, y, x) > 2)
                    {
                        intersections.Add(new Vector2(x, y));
                    }
                    if (path[y, x] == 1 && (y == 0 || y == tileDetail - 1 || x == 0 || x == tileDetail - 1))
                    {
                        start = new Vector2(x, y);
                    }
                }
            }
            List<Vector2> walk = createPath(path, (int)start.x, (int)start.y);
            paths.Add(walk);
        }
        private List<Vector2> createPath(int[,] path, int x, int y)
        {
            int[,] pCopy = (int[,])path.Clone();
            
            List<Vector2> walk = new List<Vector2>();
            while(pCopy[y, x] == 1 || AliveNeighbors(pCopy, y, x) > 0)
            {                
                walk.Add(new Vector2(x, y));
                pCopy[y, x] = 0;
                if (AliveNeighbors(pCopy, y, x) > 1)
                {
                    if (x - 1 >= 0 && pCopy[y, x-1] == 1)
                    {
                        List<Vector2> subWalk = createPath(pCopy, x - 1, y);
                        pCopy[y, x-1] = 0;
                        subWalk.Insert(0, new Vector2(x, y));
                        paths.Add(subWalk);
                    }
                    if (x + 1 < tileDetail && pCopy[y, x+1] == 1)
                    {
                        List<Vector2> subWalk = createPath(pCopy, x + 1, y);
                        pCopy[y, x+1] = 0;
                        subWalk.Insert(0, new Vector2(x, y));
                        paths.Add(subWalk);
                    }
                    if (y - 1 >= 0 && pCopy[y-1, x] == 1)
                    {
                        List<Vector2> subWalk = createPath(pCopy, x, y - 1);
                        pCopy[y-1, x] = 0;
                        subWalk.Insert(0, new Vector2(x, y));
                        paths.Add(subWalk);
                    }
                    if (y + 1 < tileDetail && pCopy[y+1, x] == 1)
                    {
                        List<Vector2> subWalk = createPath(pCopy, x, y + 1);
                        pCopy[y+1, x] = 0;
                        subWalk.Insert(0, new Vector2(x, y));
                        paths.Add(subWalk);
                    }
                }
                else if(x - 1 >= 0 && pCopy[y, x-1] == 1)
                {
                    x--;
                }
                else if(x+1 < tileDetail && pCopy[y, x+1] == 1)
                {
                    x++;
                }
                else if(y - 1 >= 0 && pCopy[y-1, x] == 1)
                {
                    y--;
                }
                else if(y + 1 < tileDetail && pCopy[y+1, x] == 1)
                {
                    y++;
                }
            }
            return walk;
        }
        public void createHeightMap(int heightMapResolution, string loc)
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath(loc + "/" + name + ".asset", typeof(TerrainData));
            float[,] heightmap = new float[heightMapResolution, heightMapResolution];
            heightmapRes = heightMapResolution;
            terrainData.heightmapResolution = heightMapResolution;
            heightmap[0, 0] = UnityEngine.Random.value;
            heightmap[0, heightMapResolution - 1] = UnityEngine.Random.value;
            heightmap[heightMapResolution - 1, 0] = UnityEngine.Random.value;
            heightmap[heightMapResolution - 1, heightMapResolution - 1] = UnityEngine.Random.value;
            divide(ref heightmap, heightMapResolution);
            heightmap = gaussBlur(heightmap, heightMapResolution, heightMapResolution, 3);
            terrainData.SetHeights(0, 0, heightmap);
            AssetDatabase.SaveAssets();
        }
        private void divide(ref float[,] hm, int size)
        {
            int x, y, half = size / 2;
            float scale = (size / heightmapRes) * 0.2f;

            if (half < 1) return;

            for (y = half; y < heightmapRes - 1; y += size)
            {
                for(x = half; x < heightmapRes - 1; x += size)
                {
                    square(ref hm, x, y, half, UnityEngine.Random.value * scale * 2 - scale);
                }
            }
            for (y = 0; y < heightmapRes; y += half)
            {
                for (x = (y + half) % size; x < heightmapRes; x += size)
                {
                    diamond(ref hm, x, y, half, UnityEngine.Random.value * scale * 2 - scale);
                }
            }
            divide(ref hm, size / 2);
        }
        private float[,] square(ref float[,] hm, int x, int y, int size, float offset)
        {
            //Debug.Log(x + " " + y + " " + size);
            float avg = (hm[x - size, y - size] + hm[x + size, y - size] + hm[x - size, y + size] + hm[x + size, y + size]) / 4.0f;
            hm[x, y] = avg + offset;

            return hm;
        }
        private float[,] diamond(ref float[,] hm, int x, int y, int size, float offset)
        {
            //Debug.Log(x + " " + y + " " + size);
            int c = 0;
            float avg = 0;
            if(y - size >= 0)
            {
                avg += hm[x, y - size];
                c++;
            }
            if(y + size < heightmapRes)
            {
                avg += hm[x, y + size];
                c++;
            }
            if(x - size >= 0)
            {
                avg += hm[x - size, y];
                c++;
            }
            if(x + size < heightmapRes)
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
        public void createSplatMap(Texture2D[] textures, int textureResolution, bool sampled, string loc)
        {
            
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath(loc + "/" + name + ".asset", typeof(TerrainData));
            
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
            
            for (int j = 0; j < paths.Count; j++)
            {                
                for (int i = 0; i < paths[j].Count; i++)
                {
                    Vector2 currentPixel = new Vector2((terrainData.alphamapWidth * (paths[j][i].x / tileDetail) + (terrainData.alphamapWidth / (2 * tileDetail))),
                                                        terrainData.alphamapHeight - (terrainData.alphamapHeight *(paths[j][i].y / tileDetail) + (terrainData.alphamapHeight / (2 * tileDetail))));

                    float xRatio = ((currentPixel.x - (terrainData.alphamapWidth / 2))) / (terrainData.alphamapWidth / 2);
                    float yRatio = ((currentPixel.y - (terrainData.alphamapHeight / 2))) / (terrainData.alphamapHeight / 2);
                    paths[j][i] = new Vector2(currentPixel.x + xRatio * (terrainData.alphamapWidth / (2f * tileDetail)),
                        currentPixel.y + yRatio * (terrainData.alphamapHeight / (2f * tileDetail)));
                }

                if (sampled && paths[j].Count > 3)
                {
                    while(paths[j].Count % 4 != 0)
                    {
                        paths[j].Add(paths[j][paths[j].Count-1]);
                    }

                    for(int q = 3; q < paths[j].Count - 2; q+=2)
                    {
                        paths[j].Insert(q, new Vector2((paths[j][q].x + paths[j][q - 1].x) / 2, (paths[j][q].y + paths[j][q - 1].y) / 2));
                        q++;
                    }
                    
                    controlPoints = paths[j];
                    curveCount = (controlPoints.Count - 1) / 3;
                }
                else
                {
                    Interpolate(paths[j], 0.33f);
                }
                List<Vector2> drawingPoints = GetDrawingPoints0();
                float adjustedPathWidth = terrainData.alphamapWidth * (pathWidth / width) / 2;
                
                foreach (Vector2 point in drawingPoints)
                {
                    for (int y = Mathf.Max(0, (int)(point.y - adjustedPathWidth * 2)); y < Mathf.Min(terrainData.alphamapHeight, (int)(point.y + adjustedPathWidth * 2)); y++)
                    {
                        for (int x = Mathf.Max(0, (int)(point.x - adjustedPathWidth * 2)); x < Mathf.Min(terrainData.alphamapWidth, (int)(point.x + adjustedPathWidth * 2)); x++)
                        {
                            if (Vector2.Distance(new Vector2(x, y), point) < adjustedPathWidth)
                            {
                                splatmapData[y, x, 0] = 1;
                                splatmapData[y, x, 1] = 0;
                            }
                            else
                            {
                                if(textures.Length > 1)
                                {
                                    Vector2 the = Vector2.Lerp(new Vector2(1, 0), new Vector2(0, 1), (Vector2.Distance(new Vector2(x, y), point) - adjustedPathWidth) / (adjustedPathWidth));
                                    if (the.x > splatmapData[y, x, 0])
                                    {
                                        splatmapData[y, x, 1] = the.y;
                                        splatmapData[y, x, 0] = the.x;
                                    }
                                }
                            }
                        }
                    }
                }
                for (int y = 0; y < terrainData.alphamapHeight; y++)
                {
                    for (int x = 0; x < terrainData.alphamapWidth; x++)
                    {
                        if(splatmapData[y, x, 0] == 0 && textures.Length > 1)
                        {
                            splatmapData[y, x, 1] = 1;
                        }
                    }
                }

            }
            terrainData.SetAlphamaps(0, 0, splatmapData);
            AssetDatabase.SaveAssets();
        }
        public void createObjects()
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath("Assets/" +  name + ".asset", typeof(TerrainData));

            AssetDatabase.CreateAsset(terrainData, "Assets/" + name + ".asset");
            AssetDatabase.SaveAssets();
        }

        public void createTile(string s)
        {
            terrainData = (TerrainData)AssetDatabase.LoadAssetAtPath(s + "/" + name + ".asset", typeof(TerrainData));
            terrainData.size = new Vector3(width, depth, height);
            terrainData.name = name;
            if (GameObject.Find(name))
            {
                GameObject.DestroyImmediate(GameObject.Find(name));
            }
            GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = name;
            terrain.transform.position = new Vector3(column * width, 0, row * -height);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
