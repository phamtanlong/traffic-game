﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AutoVehicleHandler : TileHandler {
	
	public const float START_DELAY = 4.0f;
	public const float UPDATE_INTERVAL = 0.2f;
	const float STOP_SPEED = 0.001f;
	const float DELTA_TO_ROAD = 8;
	const float RANDOM_INROAD = 4.0f / 5.0f;
	const int MIN_SPEED = 20;
	const int MAX_SPEED = 40;

	public List<GameObject> listModel = new List<GameObject> ();
	public List<AutoCollider> listCollider = new List<AutoCollider> ();
	public List<Collider> listCollision = new List<Collider> ();
	public float SPEED = 30;
	public float currentSpeed;
	public MoveDirection direction;

	public bool isInJunction = false; //giao lo
	public TweenRotation tweenFront;
	public TweenRotation tweenRear;
	
	public bool isRun = false;
	public int currentPos = -1;
	public int currentDest = 0;
	public List<Vector3> listDest = new List<Vector3> ();

	const float ACCEL_UP = 1.0f;
	const float ACCEL_DOWN = -1.0f;
	const float ACCEL_NORMAL = 0;
	public float accelerate = ACCEL_UP;
	
	void Start () {
		SPEED = Ultil.random.Next (MIN_SPEED, MAX_SPEED);
		currentSpeed = 0;
		accelerate = ACCEL_UP;
		for (int i = 0; i < listCollider.Count; ++i) {
			listCollider[i].onCollideEnter = this.CallbackCollideEnter;
			listCollider[i].onCollideExit = this.CallbackCollideExit;
		}

		//Random model
		int count = listModel.Count;
		if (count > 0) {
			int rd = Ultil.random.Next (0, count);
			for (int i = 0; i < count; ++i) {
				listModel[i].SetActive (false);
			}
			listModel[rd].SetActive (true);
		}

		Invoke ("StartRun", START_DELAY);
	}
	
	void StartRun () {
		InitDestination ();
		Invoke ("ScheduleUpdate", 0);
		//InvokeRepeating ("ScheduleUpdate", 0, UPDATE_INTERVAL);
	}
	
	void InitDestination () {
		
		listDest.Add (transform.position);
		currentPos = -1;
		currentDest = 0;
		
		RoadHandler road = Ultil.RayCastRoad (this.transform.position + new Vector3 (0,1,0));
		if (road != null) {
			Vector3 p = transform.position;
			Vector3 v;
			
			switch (direction) {
			case MoveDirection.UP:
				v = new Vector3 (p.x, p.y, p.z);
				v.z = road.anchorUp.transform.position.z - DELTA_TO_ROAD;
				listDest.Add (v); 
				break;
				
			case MoveDirection.DOWN:
				v = new Vector3 (p.x, p.y, p.z);
				v.z = road.anchorDown.transform.position.z + DELTA_TO_ROAD;
				listDest.Add (v); 
				break;
				
			case MoveDirection.LEFT:
				v = new Vector3 (p.x, p.y, p.z);
				v.x = road.anchorLeft.transform.position.x + DELTA_TO_ROAD;
				listDest.Add (v); 
				break;
				
			case MoveDirection.RIGHT:
				v = new Vector3 (p.x, p.y, p.z);
				v.x = road.anchorRight.transform.position.x - DELTA_TO_ROAD;
				listDest.Add (v); 
				break;
				
			default:
				Debug.Log ("Something wrong here!");
				break;
			}
		} else {
			Debug.LogError ("Put the car in a road, plz!");
		}
		
		NextStep ();
	}
	
	void Update () {
		if (isRun) {

			currentSpeed = currentSpeed + accelerate;

			if (currentSpeed > SPEED) {
				currentSpeed = SPEED;
				accelerate = ACCEL_NORMAL;
			} else if (currentSpeed < 0) {
				currentSpeed = 0;
				if (listCollision.Count == 0) {
					accelerate = ACCEL_UP;
				} else {
					accelerate = ACCEL_NORMAL;
				}
			}

			Vector3 step = (listDest[currentDest] - transform.position) / Vector3.Distance (transform.position, listDest[currentDest]);
			Vector3 move = step * currentSpeed * Time.deltaTime;
			transform.position = transform.position + move;
			transform.LookAt (listDest[currentDest]);
			
			if (Vector3.Distance (transform.position, listDest[currentDest]) < move.magnitude) {
				transform.position = listDest[currentDest];
				
				ScheduleUpdate ();
				NextStep ();
			}
			
			if (currentSpeed != 0) {
				tweenFront.duration = tweenRear.duration = 20.0f / currentSpeed;
			}
		} else {
			tweenFront.duration = tweenRear.duration = 1000;
		}
	}
	
	public void Init () {
		direction = Ultil.ToMoveDirection ( Ultil.GetString (TileKey.AUTOCAR_DIR, "UP", tile.properties));
		RotateToDirection ();
	}
	
	private void NextStep () {
		//Debug.Log ("Next Step");
		
		if (listDest.Count > currentDest + 1) {
			currentDest++;
			currentPos++;
			isRun = true;
			
			transform.position = listDest[currentPos];
			transform.LookAt (listDest[currentDest]);
			
			if (Vector3.Distance (listDest[currentDest], listDest[currentPos]) < 0.001f) {
				NextStep ();
			}
			
		} else {
			Debug.LogError ("No next destination => Stop Car");
			isRun = false;
			
			isInJunction = false;
			this.ScheduleUpdate ();
		}
	}
	
	public void ScheduleUpdate () {
		//Change direction if need
		
		RoadHandler road = Ultil.RayCastRoad (this.transform.position + new Vector3 (0,1,0));
		if (road != null) {
			if (road.tile.typeId == TileID.ROAD_NONE) {
				
				//Debug.Log ("In NONE Road");
				
				if (isInJunction == false) {
					
					List<RoadHandler> listAvai = new List<RoadHandler> ();
					for (int i = 0; i < road.listCollisionRoads.Count; ++i) {
						RoadHandler.CollisionRoad c = road.listCollisionRoads[i];
						RoadHandler r = c.road;
						if (r.Direction == c.dir) {
							listAvai.Add (r);
						}
					}
					
					int count = listAvai.Count;
					if (count > 0) {
						isInJunction = true;
						
						int randomIndex = Ultil.random.Next (0, count);
						RoadHandler nextRoad = listAvai[randomIndex];
						
						//Move in bezier
						CalculateNextDest (nextRoad, road);
						
						//------
						direction = nextRoad.Direction;
						//------
					} else {
						//Debug.Log ("No way to run");
						currentSpeed = STOP_SPEED;
					}
				}
			} else {
				//Debug.LogError ("Not NONE road");
				isInJunction = false;
			}
		} else {
			isInJunction = false;
			//Debug.LogError ("Null road");
		}
	}
	
	private void CalculateNextDest (RoadHandler nextRoad, RoadHandler nowRoad) {
		//Debug.Log ("Calculate Next Dest");
		bool isOpposite = false;
		if (Ultil.IsOpposite (nextRoad.Direction, direction)) {
			isOpposite = true;
		}
		
		//get point A1, A2, A3, A4
		Vector3 a1 = transform.position;
		Vector3 a2 = a1;
		
		float hRoad = Mathf.Abs (nowRoad.anchorUp.transform.position.z - nowRoad.anchorDown.transform.position.z);
		float wRoad = Mathf.Abs (nowRoad.anchorLeft.transform.position.x - nowRoad.anchorRight.transform.position.x);
		
		switch (direction) {
		case MoveDirection.UP:
			a2.z -= hRoad/4;
			break;
			
		case MoveDirection.DOWN:
			a2.z += hRoad/4;
			break;
			
		case MoveDirection.LEFT:
			a2.x += wRoad/4;
			break;
			
		case MoveDirection.RIGHT:
			a2.x -= wRoad/4;
			break;
		}
		
		Vector3 a3 = Vector3.one;
		Vector3 a4 = Vector3.one;
		
		float hNextRoad = Mathf.Abs (nextRoad.anchorUp.transform.position.z - nextRoad.anchorDown.transform.position.z);
		float wNextRoad = Mathf.Abs (nextRoad.anchorLeft.transform.position.x - nextRoad.anchorRight.transform.position.x);
		
		float deltaX = (float)(Ultil.random.NextDouble () * wNextRoad * RANDOM_INROAD);
		float deltaZ = (float)(Ultil.random.NextDouble () * hNextRoad * RANDOM_INROAD);
		deltaX -= deltaX/2;
		deltaZ -= deltaZ/2;
		
		switch (nextRoad.Direction) {
		case MoveDirection.DOWN: //quay dau
			a3 = nextRoad.anchorUp.transform.position;
			a4 = a3;
			a4.z += hRoad/4;
			
			//random
			a3.x += deltaX;
			a4.x += deltaX;
			
			if (isOpposite) {
				a3.z -= hRoad/4;
				a4.z -= hRoad/4;
			}
			
			break;
			
		case MoveDirection.UP: //quay dau
			a3 = nextRoad.anchorDown.transform.position;
			a4 = a3;
			a4.z -= hRoad/4;
			
			//random
			a3.x += deltaX;
			a4.x += deltaX;
			
			if (isOpposite) {
				a3.z += hRoad/4;
				a4.z += hRoad/4;
			}
			
			break;
			
		case MoveDirection.LEFT: //quay dau
			a3 = nextRoad.anchorRight.transform.position;
			a4 = a3;
			a4.x += wRoad/4;
			
			//random
			a3.z += deltaZ;
			a4.z += deltaZ;
			
			if (isOpposite) {
				a3.x -= wRoad/4;
				a4.x -= wRoad/4;
			}
			
			break;
			
		case MoveDirection.RIGHT: //quay dau
			a3 = nextRoad.anchorLeft.transform.position;
			a4 = a3;
			a4.x -= wRoad/4;
			
			//random
			a3.z += deltaZ;
			a4.z += deltaZ;
			
			if (isOpposite) {
				a3.x += wRoad/4;
				a4.x += wRoad/4;
			}
			
			break;
		}
		
		//------------
		
		BezierCurve bc = new BezierCurve ();
		
		List<double> ptList = new List<double>();
		ptList.Add (a1.x); //
		ptList.Add (a1.z);
		ptList.Add (a2.x); //
		ptList.Add (a2.z);
		ptList.Add (a3.x); //
		ptList.Add (a3.z);
		ptList.Add (a4.x); //
		ptList.Add (a4.z);
		
		// how many points do you need on the curve?
		int POINTS_ON_CURVE = 80;
		
		double[] ptind = new double[ptList.Count];
		double[] curvePoints = new double[POINTS_ON_CURVE];
		ptList.CopyTo (ptind, 0);
		
		bc.Bezier2D(ptind, (POINTS_ON_CURVE) / 2, curvePoints);
		
		//remove 2 first + 1 last element
		for (int i = 3; i != POINTS_ON_CURVE-3; i += 2)
		{
			//p[i+1]
			//y
			//p[i]
			Vector3 newDest = new Vector3 ((float)curvePoints[i+1], transform.position.y, (float)curvePoints[i]);
			listDest.Add (newDest); 
		}
		
		a4.y = transform.position.y;
		listDest.Add (a4); 
		
		//-----------------------------------------------
		//Destination from next road
		Vector3 curPos = a4;
		Vector3 v2;
		
		switch (nextRoad.Direction) {
		case MoveDirection.UP:
			v2 = new Vector3 (curPos.x, curPos.y, curPos.z);
			v2.z = nextRoad.anchorUp.transform.position.z - DELTA_TO_ROAD;
			listDest.Add (v2); 
			break;
			
		case MoveDirection.DOWN:
			v2 = new Vector3 (curPos.x, curPos.y, curPos.z);
			v2.z = nextRoad.anchorDown.transform.position.z + DELTA_TO_ROAD;
			listDest.Add (v2); 
			break;
			
		case MoveDirection.LEFT:
			v2 = new Vector3 (curPos.x, curPos.y, curPos.z);
			v2.x = nextRoad.anchorLeft.transform.position.x + DELTA_TO_ROAD;
			listDest.Add (v2); 
			break;
			
		case MoveDirection.RIGHT:
			v2 = new Vector3 (curPos.x, curPos.y, curPos.z);
			v2.x = nextRoad.anchorRight.transform.position.x - DELTA_TO_ROAD;
			listDest.Add (v2); 
			break;
			
		default:
			Debug.Log ("Something wrong here!");
			break;
		}
		
		if (isRun == false) {
			NextStep ();
		}
	}
	
	private void RotateToDirection () {
		
		//Rotation
		switch (direction) {
		case MoveDirection.UP:
			transform.eulerAngles = new Vector3 (0, 180, 0);
			break;
			
		case MoveDirection.DOWN:
			transform.eulerAngles = new Vector3 (0, 0, 0);
			break;
			
		case MoveDirection.LEFT:
			transform.eulerAngles = new Vector3 (0, 90, 0);
			break;
			
		case MoveDirection.RIGHT:
			transform.eulerAngles = new Vector3 (0, -90, 0);
			break;
		}
	}
	
	public void CallbackCollideEnter (Collider col, AutoCollider side) {
		string sideName = side.gameObject.name;
		string colName = col.gameObject.name;

//		Debug.Log (col.gameObject.name + " >< " + side.name);

		if (colName != OBJ.START_POINT &&
		    colName != OBJ.FINISH_POINT &&
		    colName != OBJ.CHECK_POINT) 
		{
			if (sideName == AutoCollider.FAR_FRONT) {
				if (listCollision.Count == 0) {
					//currentSpeed = SPEED / 2;
					if (isInJunction == false) {
						accelerate = ACCEL_DOWN;
					}
				}
			}
			
			if (sideName == AutoCollider.FRONT) {
				listCollision.Add (col);
				currentSpeed = STOP_SPEED;
				accelerate = ACCEL_NORMAL;
			}
		}
	}
	
	public void CallbackCollideExit (Collider col, AutoCollider side) {
		
		string sideName = side.gameObject.name;
		string colName = col.gameObject.name;
		
		if (colName != OBJ.START_POINT &&
		    colName != OBJ.FINISH_POINT &&
		    colName != OBJ.CHECK_POINT) 
		{
			if (sideName == AutoCollider.FRONT) {
				listCollision.Remove (col);
				if (listCollision.Count == 0) {
					//currentSpeed = SPEED;
					accelerate = ACCEL_UP;
				}
			}
			
			if (sideName == AutoCollider.FAR_FRONT) {
				if (listCollision.Count == 0) {
					//currentSpeed = SPEED;
					accelerate = ACCEL_UP;
				}
			}
		}
	}
}
