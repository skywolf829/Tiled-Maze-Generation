// Stitchscape 2.0
// ©2016 Starscene Software. All rights reserved. Redistribution without permission not allowed.

import System.Collections.Generic;
import UnityEngine.GUI;

enum Direction {Across, Down}

class Stitchscape extends ScriptableWizard {
	static var across : int;
	static var down : int;
	static var tWidth : int;
	static var tHeight : int;
	static var terrains : Terrain[];
	static var stitchWidthPercent : float;
	static var stitchWidth : int;
	static var message : String;
	static var terrainRes : int;
	static var lineTex : Texture2D;
	static var strength : float;
	static var playError = false;
	static var gridPixelHeight = 28;
	static var gridPixelWidth = 121;
	static var window : Stitchscape;
	
	@MenuItem ("GameObject/Stitch Terrains... &#t")
	static function CreateWizard () {
		if (lineTex == null) {	// across/down etc. defined here, so closing and re-opening wizard doesn't reset vars
			across = down = tWidth = tHeight = 2;
			stitchWidthPercent = .1;
			strength = .5;
			SetNumberOfTerrains();
			lineTex = EditorGUIUtility.whiteTexture;
		}
		message = "";
		playError = false;
		window = ScriptableWizard.DisplayWizard ("Stitch Terrains", Stitchscape) as Stitchscape;
		window.minSize = Vector2(270, 245);
	}
	
	new function OnGUI () {
		if (Application.isPlaying) {
			playError = true;
		}
		if (playError) {	// Needs to continue showing this even if play mode is stopped
			Label (Rect(5, 5, 250, 16), "Stitchscape can't run in play mode");
			return;
		}
		
		Label (Rect(5, 5, 160, 16), "Number of terrains across:");
		across = Mathf.Max (EditorGUI.IntField (Rect(170, 5, 30, 16), across), 1);
		Label (Rect(5, 25, 160, 16), "Number of terrains down:");
		down = Mathf.Max (EditorGUI.IntField (Rect(170, 25, 30, 16), down), 1);
		if (Button(Rect(210, 14, 50, 18), "Apply")) {
			tWidth = across;
			tHeight = down;
			SetNumberOfTerrains();
		}
		
		if (Button (Rect(16, 52, gridPixelWidth*tWidth + 1, 18), "Autofill from scene") ) {
			AutoFill();
		}
		
		var counter = 0;
		for (var h = 0; h < tHeight; h++) {
			for (var w = 0; w < tWidth; w++) {
				terrains[counter] = EditorGUI.ObjectField (Rect(20 + w*gridPixelWidth, 82 + h*gridPixelHeight, 112, 16), terrains[counter++], Terrain, true) as Terrain;
			}
		}
		DrawGrid (Color.black, 1, 75);
		DrawGrid (Color.white, 0, 75);
		
		Label (Rect(2, 71, 20, 20), "Z");
		Label (Rect(gridPixelWidth*tWidth + 10, 77 + gridPixelHeight*tHeight, 20, 20), "X");
		color = Color.black;
		DrawTexture (Rect(7, 87, 1, gridPixelHeight*tHeight - 2), lineTex);
		DrawTexture (Rect(7, 85 + gridPixelHeight*tHeight, gridPixelWidth*tWidth, 1), lineTex);
		color = Color.white;
		
		Label (Rect(5, 95 + gridPixelHeight*tHeight, 115, 16), "Stitch width %: " + (stitchWidthPercent * 100).ToString("f0"));
		stitchWidthPercent = HorizontalSlider (Rect(120, 95 + gridPixelHeight*tHeight, window.position.width - 127, 16), stitchWidthPercent, .01, .5);
		
		Label (Rect(5, 111 + gridPixelHeight*tHeight, 115, 16), "Blend strength: " + (strength * 100).ToString("f0"));
		strength = HorizontalSlider (Rect(120, 111 + gridPixelHeight*tHeight, window.position.width - 127, 16), strength, 0.0, 1.0);
		
		Label (Rect(5, 136 + gridPixelHeight*tHeight, window.position.width - 10, 20), message);
		
		var buttonWidth = window.position.width/2 - 10;
		if (Button (Rect(7, 156 + gridPixelHeight*tHeight, buttonWidth, 18), "Clear")) {
			SetNumberOfTerrains();
		}
		if (Button (Rect(12 + buttonWidth, 156 + gridPixelHeight*tHeight, buttonWidth, 18), "Stitch")) {
			StitchTerrains();
		}
	}
	
	private static function AutoFill () {
		var sceneTerrains = FindObjectsOfType (Terrain);
		if (sceneTerrains.Length == 0) {
			message = "No terrains found";
			return;
		}
		
		var xPositions = new List.<float>();
		var zPositions = new List.<float>();
		var tPosition = sceneTerrains[0].transform.position;
		xPositions.Add (tPosition.x);
		zPositions.Add (tPosition.z);
		for (var i = 0; i < sceneTerrains.Length; i++) {
			tPosition = sceneTerrains[i].transform.position;
			if (!ListContains(xPositions, tPosition.x)) {
				xPositions.Add (tPosition.x);
			}
			if (!ListContains(zPositions, tPosition.z)) {
				zPositions.Add (tPosition.z);
			}
		}
		if (xPositions.Count * zPositions.Count != sceneTerrains.Length) {
			message = "Unable to autofill. Terrains should line up closely in the form of a grid.";
			return;
		}
		
		xPositions.Sort();
		zPositions.Sort();
		zPositions.Reverse();
		across = tWidth = xPositions.Count;
		down = tHeight = zPositions.Count;
		terrains = new Terrain[tWidth * tHeight];
		var count = 0;
		for (var z = 0; z < zPositions.Count; z++) {
			for (var x = 0; x < xPositions.Count; x++) {
				for (i = 0; i < sceneTerrains.Length; i++) {
					tPosition = sceneTerrains[i].transform.position;
					if (Approx(tPosition.x, xPositions[x]) && Approx(tPosition.z, zPositions[z])) {
						terrains[count++] = sceneTerrains[i];
						break;
					}
				}
			}
		}
		message = "";
	}
	
	private static function ListContains (list : List.<float>, pos : float) : boolean {
		for (var i = 0; i < list.Count; i++) {
			if (Approx(pos, list[i])) {
				return true;
			}
		}
		return false;
	}

	private static function Approx (pos1 : float, pos2 : float) : boolean {
		if (pos1 >= pos2-1.0 && pos1 <= pos2+1.0) {
			return true;
		}
		return false;
	}
	
	private function DrawGrid (color : Color, offset : int, top : int) {
		GUI.color = color;
		for (var i = 0; i < tHeight+1; i++) {
			GUI.DrawTexture (Rect(15 + offset, top + offset + gridPixelHeight*i, gridPixelWidth*tWidth, 1), lineTex);
		}
		for (i = 0; i < tWidth+1; i++) {
			GUI.DrawTexture (Rect(15 + offset + gridPixelWidth*i, top + offset, 1, gridPixelHeight*tHeight + 1), lineTex);		
		}
	}
	
	private static function SetNumberOfTerrains () {
		terrains = new Terrain[tWidth * tHeight];
		message = "";
	}
	
	private static function StitchTerrains () {
		for (t in terrains) {
			if (t == null) {
				message = "All terrain slots must have a terrain assigned";
				return;
			}
		}
		
		terrainRes = terrains[0].terrainData.heightmapWidth;
		if (terrains[0].terrainData.heightmapHeight != terrainRes) {
			message = "Heightmap width and height must be the same";
			return;
		}
		
		for (t in terrains) {
			if (t.terrainData.heightmapWidth != terrainRes || t.terrainData.heightmapHeight != terrainRes) {
				message = "All heightmaps must be the same resolution";
				return;
			}
		}
		
		for (t in terrains) {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
			Undo.RegisterCompleteObjectUndo (t.terrainData, "Stitch Terrains");
#else
			Undo.RegisterUndo (t.terrainData, "Stitch Terrains");
#endif
		}
		
		stitchWidth = Mathf.Clamp (terrainRes * stitchWidthPercent, 2, (terrainRes-1)/2);
		var counter = 0;
		var total = tHeight*(tWidth-1) + (tHeight-1)*tWidth;
		
		if (tWidth == 1 && tHeight == 1) {
			BlendData (terrains[0].terrainData, terrains[0].terrainData, Direction.Across, true);
			BlendData (terrains[0].terrainData, terrains[0].terrainData, Direction.Down, true);
			message = "Terrain has been made repeatable with itself";
		}
		else {
			for (var h = 0; h < tHeight; h++) {
				for (var w = 0; w < tWidth-1; w++) {
					EditorUtility.DisplayProgressBar ("Stitching...", "", Mathf.InverseLerp (0, total, ++counter));
					BlendData (terrains[h*tWidth + w].terrainData, terrains[h*tWidth + w + 1].terrainData, Direction.Across, false);
				}
			}
			for (h = 0; h < tHeight-1; h++) {
				for (w = 0; w < tWidth; w++) {
					EditorUtility.DisplayProgressBar ("Stitching...", "", Mathf.InverseLerp (0, total, ++counter));
					BlendData (terrains[h*tWidth + w].terrainData, terrains[(h+1)*tWidth + w].terrainData, Direction.Down, false);
				}
			}
			message = "Terrains stitched successfully";
		}
		
		EditorUtility.ClearProgressBar();
	}
	
	private static function BlendData (terrain1 : TerrainData, terrain2 : TerrainData, thisDirection : Direction, singleTerrain : boolean) {
		var heightmapData = terrain1.GetHeights (0, 0, terrainRes, terrainRes);
		var heightmapData2 = terrain2.GetHeights (0, 0, terrainRes, terrainRes);
		var width = terrainRes-1;
		
		if (thisDirection == Direction.Across) {
			for (var i = 0; i < terrainRes; i++) {
				var midpoint = (heightmapData[i, width] + heightmapData2[i, 0]) * .5;
				for (var j = 1; j < stitchWidth; j++) {
					var mix = Mathf.Lerp (heightmapData[i, width-j], heightmapData2[i, j], .5);
					if (j == 1) {
						heightmapData[i, width] = Mathf.Lerp (mix, midpoint, strength);
						heightmapData2[i, 0] = Mathf.Lerp (mix, midpoint, strength);
					}
					var t = Mathf.SmoothStep (0.0, 1.0, Mathf.InverseLerp (1, stitchWidth-1, j));
					var mixdata = Mathf.Lerp (mix, heightmapData[i, width-j], t);
					heightmapData[i, width-j] = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData[i, width-j], t), strength);
										
					mixdata = Mathf.Lerp (mix, heightmapData2[i, j], t);
					var blend = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData2[i, j], t), strength);
					if (!singleTerrain) {
						heightmapData2[i, j] = blend;
					}
					else {
						heightmapData[i, j] = blend;
					}
				}
			}
			if (singleTerrain) {
				for (i = 0; i < terrainRes; i++) {
					heightmapData[i, 0] = heightmapData[i, width];
				}
			}
		}
		else {
			for (i = 0; i < terrainRes; i++) {
				midpoint = (heightmapData2[width, i] + heightmapData[0, i]) * .5;
				for (j = 1; j < stitchWidth; j++) {
					mix = Mathf.Lerp (heightmapData2[width-j, i], heightmapData[j, i], .5);
					if (j == 1) {
						heightmapData2[width, i] = Mathf.Lerp (mix, midpoint, strength);
						heightmapData[0, i] = Mathf.Lerp (mix, midpoint, strength);
					}
					t = Mathf.SmoothStep (0.0, 1.0, Mathf.InverseLerp (1, stitchWidth-1, j));
					mixdata = Mathf.Lerp (mix, heightmapData[j, i], t);
					heightmapData[j, i] = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData[j, i], t), strength);
					
					mixdata = Mathf.Lerp (mix, heightmapData2[width-j, i], t);
					blend = Mathf.Lerp (mixdata, Mathf.Lerp (midpoint, heightmapData2[width-j, i], t), strength);
					if (!singleTerrain) {
						heightmapData2[width-j, i] = blend;
					}
					else {
						heightmapData[width-j, i] = blend;
					}
				}
			}
			if (singleTerrain) {
				for (i = 0; i < terrainRes; i++) {
					heightmapData[width, i] = heightmapData[0, i];
				}
			}
		}
		
		terrain1.SetHeights (0, 0, heightmapData);
		if (!singleTerrain) {
			terrain2.SetHeights (0, 0, heightmapData2);
		}
	}
}