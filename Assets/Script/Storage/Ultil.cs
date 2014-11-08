﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ultil {

	private static int layerId = 0;
	private static int objId = 0;

	public static void ResetLayerId () {
		layerId = 0;
	}
	public static int GetNewLayerId () {
		layerId++;
		return layerId;
	}

	public static void ResetObjId (int defaultId = 0) {
		objId = defaultId;
	}
	public static int GetNewObjId () {
		objId++;
		return objId;
	}

	public static string GetString (string key, string defaul, Dictionary<string,string> dict) {
		string value = null;
		dict.TryGetValue (key, out value);

		if (string.IsNullOrEmpty (value)) {
			value = defaul;
			dict[key] = value;
		}

		return value;
	}

	public static Vector2 ParseToMapPosition (float tilex, float tiley) {
		return new Vector2 (tilex * Global.SCALE_TILE * Global.SCALE_SIZE, tiley * Global.SCALE_TILE * Global.SCALE_SIZE);
	}
}
