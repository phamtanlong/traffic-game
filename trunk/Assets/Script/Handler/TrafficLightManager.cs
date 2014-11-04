﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrafficLightManager : Singleton <TrafficLightManager> {

	public const float RED_TIME = 10;
	public const float YELLOW_TIME = 3;
	public const float GREEN_TIME = RED_TIME - YELLOW_TIME;
	public const float TOTAL_TIME = RED_TIME + YELLOW_TIME + GREEN_TIME;

	public List<TrafficLightHandler> listLight = new List<TrafficLightHandler> ();

	float time = 0;

	public void Update () {
		time += Time.deltaTime;
		if (time >= TOTAL_TIME) {
			time = 0;
		}

		//up-down
		LightStatus ud = GetStatus (time);
		LightStatus lr = GetStatus (time + RED_TIME);

		for (int i = 0; i < listLight.Count; ++i) {
			if (listLight[i].Direction == MyDirection.UP || listLight[i].Direction == MyDirection.DOWN) {
				listLight[i].Status = ud;
			} else {
				listLight[i].Status = lr;
			}
		}
	}

	public void AddLight (TrafficLightHandler light) {
		listLight.Add (light);
	}

	private LightStatus GetStatus (float t) {
		int t2 = (int)t;
		int total = (int) TOTAL_TIME;

		t2 %= total;

		if (t2 < GREEN_TIME) {
			return LightStatus.green;
		} else if (t2 < GREEN_TIME + YELLOW_TIME) {
			return LightStatus.yellow;
		} else {
			return LightStatus.red;
		}
	}
}