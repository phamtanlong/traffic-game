using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class PlayerHandler : SingletonMono <PlayerHandler> {

	public const float DELAY_TIME = 1;
	public const float SCHEDULE_TIME = 0.25f;

	private BikeHandler bikeHandler;
	private BikeMovement bikeMovement;

	#region Kiem tra loi

	public const int QUEUE_SIZE = 50;
	private Queue queueState = new Queue (QUEUE_SIZE); 			//Danh sach sau moi SCHEDULE_TIME
	private Queue queueStateDiff = new Queue (QUEUE_SIZE);		//Danh sach state khi co chuyen lan duong

	private bool viphamTocDo = false;
	private bool viphamTocDo1 = false;
	private bool viphamTocDo2 = false;
	private bool viphamTocDo3 = false;

	private bool viphamNguocChieu = false;

	private bool viphamLanTuyen = false;

	private bool viphamMuBaoHiem = false;

	private bool viphamBatDen = false;

	private bool viphamDungGiuaDuong = false;

	private bool viphamDuongCam = false;

	private bool viphamTocDoDuoi = false;

	#endregion
	
	void Awake () {
		bikeHandler = gameObject.GetComponent <BikeHandler> ();
		bikeMovement = gameObject.GetComponent <BikeMovement> ();
	}

	void Start () {
		InvokeRepeating ("UpdateState", DELAY_TIME, SCHEDULE_TIME);
	}

	void Update () {
		//Started
		if (Main.Instance.isStarted == false) {
			if (bikeMovement.Speed > 0.1f) {
				Main.Instance.OnStartGame ();
			}
		}
		
		if (Input.GetKeyUp (KeyCode.L)) {
			bikeHandler.LightOnOff ();
			
			//Bat den chieu xa trong do thi
			if (bikeHandler.isLightOn == true && bikeHandler.isNearLight == false) {
				ErrorManager.Instance.PushError (13, Main.Instance.time);
			}
		}

		if (Main.Instance.isStarted == false || Main.Instance.isEndGame == true) {return;}

		//Sound ----------------------------------
		if (Input.GetKeyUp (KeyCode.B)) {
			SoundManager.Instance.PlayHorn ();

			if (Main.Instance.time >= Global.TIME_STOP_HORN || Main.Instance.time <= Global.TIME_START_HORN) {
				ErrorManager.Instance.PushError (12, Main.Instance.time);
			}
		}
		
		//Light Near/Far--------------------------

		if (Input.GetKeyUp (KeyCode.F)) {
			bikeHandler.LightFar ();

			//Bat den chieu xa trong do thi
			if (bikeHandler.isLightOn == true && bikeHandler.isNearLight == false) {
				ErrorManager.Instance.PushError (13, Main.Instance.time);
			}
		}
		
		if (Input.GetKeyUp (KeyCode.N)) {
			bikeHandler.LightNear ();
		}
		
		//Light Left/Right -----------------------
		
		if (Input.GetKeyUp (KeyCode.Q) || Input.GetKeyUp (KeyCode.Less)) {
			bikeHandler.LeftLight ();
		}
		
		if (Input.GetKeyUp (KeyCode.E) || Input.GetKeyUp (KeyCode.Greater)) {
			bikeHandler.RightLight ();
		}
	}

	public void StopRunning () {
		bikeMovement.StopRunning ();
	}

	//Check road to run right direction
	public void CheckRoad () {
		RoadHandler road = Ultil.RayCastRoad (this.transform.position + new Vector3 (0,1,0));
		if (road != null) {
			switch (road.tile.typeId) {
			case TileID.ROAD_UP:
				transform.localEulerAngles = new Vector3 (0, 180, 0);
				break;

			case TileID.ROAD_DOWN:
				transform.localEulerAngles = new Vector3 (0, 0, 0);
				break;

			case TileID.ROAD_LEFT:
				transform.localEulerAngles = new Vector3 (0, 90, 0);
				break;

			case TileID.ROAD_RIGHT:
				transform.localEulerAngles = new Vector3 (0, -90, 0);
				break;
			}
		} else {
			Debug.LogError ("Some thing wrong here!");
			//default run UP
		}
	}

	
	#region COLLISION, CHECK POINT
	void OnTriggerEnter(Collider other) {
		if (Main.Instance.isEndGame == true) {return;}

		//Debug.Log (other.name);

		switch (other.name) {
		case OBJ.FINISH_POINT:
			if (GameManager.Instance.IsCompleted) {
				Main.Instance.OnCompleteGame ();
			}
			break;

		case OBJ.CHECK_POINT:
			TileHandler th2 = other.gameObject.GetComponent<TileHandler> ();
			if (th2 != null) {
				GameManager.Instance.CompleteCheckpoint (th2.tile.objId);
			}
			break;

		case OBJ.RoadBorderRight:
		case OBJ.RoadBorderLeft:
		case OBJ.RoadBorderUp:
		case OBJ.RoadBorderDown:
			ErrorManager.Instance.PushError (4, Main.Instance.time);
			SoundManager.Instance.PlayCrash ();
			break;
		}
	}
	#endregion

	#region PLAYER STATE
	private void OnRoadChange (PlayerState oldState, PlayerState newState) {

		#region Lan tuyen + Duong cam
		if (newState.road.BikeAvailable == false) {
			if (newState.road.Direction == oldState.road.Direction) {
				if (viphamLanTuyen == false) {
					viphamLanTuyen = true;
					ErrorManager.Instance.PushError (1, Main.Instance.time);
				}
			} else {
				if (viphamDuongCam == false) {
					viphamDuongCam = true;
					ErrorManager.Instance.PushError (21, Main.Instance.time);
				}
			}
		} else { viphamLanTuyen = false; viphamDuongCam = false; }
		#endregion

		#region Vuot Den Do
		if (Ultil.IsRoad (newState.road.tile.typeId) 
		    && newState.road.Direction != oldState.road.Direction //Ko di cung chieu
		    && newState.road.Direction != Ultil.OppositeOf (oldState.road.Direction) //Ko di nguoc chieu
		    && oldState.road.LightStatus == TrafficLightStatus.red //Den dang do
		    && oldState.direction == oldState.road.Direction) { //Di dung chieu

			ErrorManager.Instance.PushError (2, Main.Instance.time);
		}
		#endregion

		#region Vuot Den Vang
		if (Ultil.IsRoad (newState.road.tile.typeId) 
		    && newState.road.Direction != oldState.road.Direction
		    && newState.road.Direction != Ultil.OppositeOf (oldState.road.Direction)
		    && oldState.road.LightStatus == TrafficLightStatus.yellow
		    && oldState.direction == oldState.road.Direction) {

			ErrorManager.Instance.PushError (28, Main.Instance.time);
		}
		#endregion

		#region Re tai noi ko cho re
		//1: re trai tai nga 3, 4
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) { 							//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {
						if (newState.road.Direction == Ultil.LeftOf (prev.road.Direction)) {
							if (prev.road.CanTurnLeft == false) {
								ErrorManager.Instance.PushError (27, Main.Instance.time);
							}
						}
					}
				} else if (newState.road.Direction == Ultil.LeftOf (oldState.road.Direction)) { //Ko Co Giao Lo
					PlayerState prev = oldState;
					if (prev.road.CanTurnLeft == false) {
						ErrorManager.Instance.PushError (27, Main.Instance.time);
					}
				}
			}
		}
		
		//2: re phai tai nga 3, 4
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) { 							//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {
						if (newState.road.Direction == Ultil.RightOf (prev.road.Direction)) {
							if (prev.road.CanTurnRight == false) {
								ErrorManager.Instance.PushError (26, Main.Instance.time);
							}
						}
					}
				} else if (newState.road.Direction == Ultil.RightOf (oldState.road.Direction)) { //Ko Co Giao Lo
					PlayerState prev = oldState;
					if (prev.road.CanTurnRight == false) {
						ErrorManager.Instance.PushError (26, Main.Instance.time);
					}
				}
			}
		}
		
		//3: quay dau xe
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) { 							//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {
						if (newState.road.Direction == Ultil.OppositeOf (prev.road.Direction)) {//Quay nguoc lai
							if (prev.road.CanTurnBack == false || prev.road.CanTurnLeft == false) {
								ErrorManager.Instance.PushError (16, Main.Instance.time);
							}
						}
					}
				} else { //Ko Co Giao Lo
					ErrorManager.Instance.PushError (24, Main.Instance.time);
				}
			}
		}
		#endregion

		#region Re ko xi nhanh
		//1: re trai tai nga 3, 4
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) {					//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {
						if (newState.road.Direction == Ultil.LeftOf (prev.road.Direction)) {

							//KO xi nhanh
							if (prev.turnLight != TurnLight.LEFT && oldState.turnLight != TurnLight.LEFT && newState.turnLight != TurnLight.LEFT) {
								ErrorManager.Instance.PushError (5, Main.Instance.time);
							}

							//KO giam toc do
							if (prev.lastState != null) {
								if (prev.lastState.speed <= prev.speed && prev.speed > Global.MAX_TURNING_SPEED) {
									ErrorManager.Instance.PushError (18, Main.Instance.time);
								}
							}
						}
					}
				} else if (newState.road.Direction == Ultil.LeftOf (oldState.road.Direction)) { //Ko Co Giao Lo
					PlayerState prev = oldState;

					//Ko xi nhanh
					if (prev.turnLight != TurnLight.LEFT  && newState.turnLight != TurnLight.LEFT) {
						ErrorManager.Instance.PushError (5, Main.Instance.time);
					}
					
					//KO giam toc do
					if (prev.lastState != null) {
						if (prev.lastState.speed <= prev.speed && prev.speed > Global.MAX_TURNING_SPEED) {
							ErrorManager.Instance.PushError (18, Main.Instance.time);
						}
					}
				}
			}
		}

		//2: re phai tai nga 3, 4
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) {					//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {

						if (newState.road.Direction == Ultil.RightOf (prev.road.Direction)) {
							
							//Ko xi nhanh
							if (prev.turnLight != TurnLight.RIGHT  && oldState.turnLight != TurnLight.RIGHT && newState.turnLight != TurnLight.RIGHT) {
								ErrorManager.Instance.PushError (5, Main.Instance.time);
							}
							
							//KO giam toc do
							if (prev.lastState != null) {
								if (prev.lastState.speed <= prev.speed && prev.speed > Global.MAX_TURNING_SPEED) {
									ErrorManager.Instance.PushError (18, Main.Instance.time);
								}
							}
						}

					}
				} else if (newState.road.Direction == Ultil.RightOf (oldState.road.Direction)) {//Ko Co Giao Lo
					PlayerState prev = oldState;

					//Ko xi nhanh
					if (prev.turnLight != TurnLight.RIGHT && newState.turnLight != TurnLight.RIGHT) {
						ErrorManager.Instance.PushError (5, Main.Instance.time);
					}
					
					//KO giam toc do
					if (prev.lastState != null) {
						if (prev.lastState.speed <= prev.speed && prev.speed > Global.MAX_TURNING_SPEED) {
							ErrorManager.Instance.PushError (18, Main.Instance.time);
						}
					}
				}
			}
		}

		//3: quay dau xe
		if (newState.road.IsBus == false && oldState.road.IsBus == false) {
			if (newState.road.Direction != MoveDirection.NONE) {
				if (oldState.road.Direction == MoveDirection.NONE) { 							//Co Giao Lo
					PlayerState prev = Ultil.GetPreviousDiffState (oldState, queueStateDiff);
					if (prev != null) {

						if (newState.road.Direction == Ultil.OppositeOf (prev.road.Direction)) {//Quay nguoc lai
							
							//Ko xi nhanh
							if (prev.turnLight != TurnLight.LEFT && oldState.turnLight != TurnLight.LEFT && newState.turnLight != TurnLight.LEFT) {
								ErrorManager.Instance.PushError (5, Main.Instance.time);
							}

							//KO giam toc do
							if (prev.lastState != null) {
								if (prev.lastState.speed <= prev.speed && prev.speed > Global.MAX_TURNING_SPEED) {
									ErrorManager.Instance.PushError (18, Main.Instance.time);
								}
							}
						}
					}
				}
			}
		}
		#endregion

		#region Xi nhanh nhung ko re

		#endregion
	}

	private void OnInRoadChange (PlayerState newState) {
		#region LANG LACH
//		PlayerState oldState = null;
//		object[] arr = queueState.ToArray ();
//
//		int index1 = -1;
//		//int index2 = -1;
//		for (int i = arr.Length-1; i >= 0; --i) {
//			PlayerState pl = arr[i] as PlayerState;
//			if (pl.road.tile.objId == newState.road.tile.objId) {
//				if (pl.inRoadPos != newState.inRoadPos) {
//					index1 = i;
//				} else {
//					if (index1 > -1) {
//						//index2 = i;
//						oldState = pl;
//						if (newState.time - oldState.time < Global.TIME_TO_LANGLACH) {
//							NotifierHandler.Instance.AddNotify ((int)Time.realtimeSinceStartup + "s: [ff0000]Lạng lách[-]");
//						}
//						break;
//					}
//				}
//			}
//		}
		#endregion
	}

	private void CheckState (PlayerState state) {
		//Khong doi mu bao hiem
		if (state.isHelmetOn == false && state.speed > Global.RUN_SPEED_POINT) {
			if (viphamMuBaoHiem == false) {
				viphamMuBaoHiem = true;
				ErrorManager.Instance.PushError (6, Main.Instance.time);
			}
		} else { viphamMuBaoHiem = false; }

		//Nguoc chieu
		if (Ultil.IsOpposite (state.direction, state.road.Direction)) {
			if (viphamNguocChieu == false) {
				viphamNguocChieu = true;
				ErrorManager.Instance.PushError (3, Main.Instance.time);
			}
		} else {
			viphamNguocChieu = false;
		}

		//Chay qua cham
		if (state.speed < state.road.MinSpeed) {
			if (viphamTocDoDuoi == false) {
				viphamTocDoDuoi = true;
				ErrorManager.Instance.PushError (22, Main.Instance.time);
			}
		} else { viphamTocDoDuoi = false; }

		//Chay qua toc do
		if (state.speed > state.road.MaxSpeed) {
			//0-5
			//5-10
			//10-20
			//20-35
			//35
			float deltaSpeed = state.speed - state.road.MaxSpeed;
			if (deltaSpeed < 5) {
				if (viphamTocDo == false) {
					viphamTocDo = true;
					NotifierHandler.Instance.PushNotify ((int)Time.realtimeSinceStartup + "s: [ffff00]Warning: over speed limit![-]");
				}
			} else { viphamTocDo = false; }

			if (deltaSpeed >= 5 && deltaSpeed < 10) {
				if (viphamTocDo1 == false && viphamTocDo2 == false && viphamTocDo3 == false) {
					viphamTocDo1 = true;
					ErrorManager.Instance.PushError (8, Main.Instance.time);
				}
			} else { viphamTocDo1 = false; }

			if (deltaSpeed >= 10 && deltaSpeed < 20) {
				if (viphamTocDo2 == false && viphamTocDo3 == false) {
					viphamTocDo2 = true;
					viphamTocDo1 = true;
					ErrorManager.Instance.PushError (9, Main.Instance.time);
				}
			} else { viphamTocDo2 = false; }

			if (deltaSpeed >= 20) {
				if (viphamTocDo3 == false) {
					viphamTocDo3 = true;
					viphamTocDo2 = true;
					viphamTocDo1 = true;
					ErrorManager.Instance.PushError (10, Main.Instance.time);
				}
			} else { viphamTocDo3 = false; }
		}


		//Khong bat den khi troi toi
		if (state.isLightOn == false && Main.Instance.needTheLight == true) {
			if (viphamBatDen == false) {
				viphamBatDen = true;
				ErrorManager.Instance.PushError (7, Main.Instance.time);
			}
		} else { viphamBatDen = false; }
	}

	public void OnStop (PlayerState oldState, PlayerState newState) {

		//Dung Tram Xe Bus
		if (newState.road.IsBus == true)
		{
			ErrorManager.Instance.PushError (14, Main.Instance.time);
		}

		//Dung xe giua nga 3,4
		//Dam vach
		if (newState.road.tile.typeId == TileID.ROAD_NONE) {
			switch (newState.vachKeDuong) {
			case MoveDirection.LEFT:
			case MoveDirection.RIGHT:
			case MoveDirection.UP:
			case MoveDirection.DOWN:
				ErrorManager.Instance.PushError (20, Main.Instance.time);
				break;
			}
		}

		MoveDirection dir = newState.road.CheckInLeDuong (this.transform.position);

		if (newState.road.IsBus == false) {
			if (newState.road.tile.typeId == TileID.ROAD_NONE && newState.vachKeDuong == MoveDirection.NONE) {
				if (viphamDungGiuaDuong == false) {
					viphamDungGiuaDuong = true;
					ErrorManager.Instance.PushError (11, Main.Instance.time);
				}
			} else if (newState.road.tile.typeId != TileID.ROAD_NONE
			           && dir == MoveDirection.NONE 
			           && newState.road.LightStatus != TrafficLightStatus.red) {

				if (viphamDungGiuaDuong == false) {
					viphamDungGiuaDuong = true;
					ErrorManager.Instance.PushError (11, Main.Instance.time);
				}
			}
		}
		

		//Dung xe khong co tin hieu
//		if (dir != MoveDirection.NONE) {
//			if (oldState.turnLight == TurnLight.NONE) {
//				ErrorManager.Instance.PushError (23, Main.Instance.time);
//			} else if (dir == Ultil.RightOf (newState.direction)) { //Phai
//				if (oldState.turnLight != TurnLight.RIGHT) {
//					ErrorManager.Instance.PushError (23, Main.Instance.time);
//				}
//			} else if (dir == Ultil.LeftOf (newState.direction)) { //Trai
//				if (oldState.turnLight != TurnLight.LEFT) {
//					ErrorManager.Instance.PushError (23, Main.Instance.time);
//				}
//			}
//		}
	}

	private PlayerState _currentState;
	private PlayerState _lastState;

	private void UpdateState () {
		if (Main.Instance.isStarted == false || Main.Instance.isEndGame == true) {return;}

		PlayerState state = GetCurrentState ();

		if (state.road == null) {
			//Debug.Log ("Null");
		} else {
			_lastState = _currentState;
			_currentState = state;

			if (_lastState != null) {
				_lastState.nextState = _currentState;
			}
			_currentState.lastState = _lastState;

			Ultil.AddToQueue (_currentState, queueState, QUEUE_SIZE);
			CheckState (_currentState);

			if (_lastState != null) {
				//Change Road
				if (_lastState.road.tile.objId != _currentState.road.tile.objId) {
					Ultil.AddToQueue (_lastState, queueStateDiff, QUEUE_SIZE);
					Ultil.AddToQueue (_currentState, queueStateDiff, QUEUE_SIZE);
					OnRoadChange (_lastState, _currentState);
				}

				//Change In Road
				if (_lastState.inRoadPos != _currentState.inRoadPos
				    && _lastState.road.tile.objId == _currentState.road.tile.objId) 
				{
					OnInRoadChange (_currentState);
				}
			} else {
				Ultil.AddToQueue (_currentState, queueStateDiff, QUEUE_SIZE);
			}

			//Check Stop
			if (Main.Instance.isStarted == true) {
				if (_currentState.speed < Global.RUN_SPEED_POINT) {
					if (_lastState != null && _lastState.speed > Global.RUN_SPEED_POINT) {
						OnStop (_lastState, _currentState);
					} else {
						viphamDungGiuaDuong = false;
					}
				}
			}
		}
	}

	public PlayerState GetCurrentState () {
		PlayerState p = new PlayerState ();

		p.time = Time.realtimeSinceStartup;
		p.isHelmetOn = bikeHandler.isHelmetOn;
		p.isLightOn = bikeHandler.isLightOn;
		p.isNearLight = bikeHandler.isNearLight;
		p.turnLight = bikeHandler.turnLight;
		p.speed = bikeMovement.Speed;
		p.road = null;
		p.vachKeDuong = MoveDirection.NONE;

		//direction
		float x = transform.forward.x;
		float z = transform.forward.z;
		if (Mathf.Abs (x) > Mathf.Abs (z)) {
			//left-right
			if (x > 0) {
				p.direction = MoveDirection.LEFT;
			} else {
				p.direction = MoveDirection.RIGHT;
			}
		} else {
			//up-down
			if (z > 0) {
				p.direction = MoveDirection.DOWN;
			} else {
				p.direction = MoveDirection.UP;
			}
		}

		//Road
		RaycastHit hit;
		Ray rayDown = new Ray (transform.position + new Vector3 (0, 1, 0), Vector3.down);
		if (Physics.Raycast (rayDown, out hit)) {
			if (hit.transform.gameObject.name.Equals (OBJ.ROAD)) {
				p.road = hit.transform.gameObject.GetComponent <RoadHandler>();

				//InRoadPosition
				p.inRoadPos = p.road.CheckInOutLen (transform.position);
			} else {
				string name = hit.transform.gameObject.name;
				bool damVach = false;

				switch (name) {
				case OBJ.VachLeft:
					damVach = true;
					p.vachKeDuong = MoveDirection.LEFT;
					break;

				case OBJ.VachRight:
					damVach = true;
					p.vachKeDuong = MoveDirection.RIGHT;
					break;

				case OBJ.VachUp:
					damVach = true;
					p.vachKeDuong = MoveDirection.UP;
					break;

				case OBJ.VachDown:
					damVach = true;
					p.vachKeDuong = MoveDirection.DOWN;
					break;
				}

				if (damVach == true) {
					if (hit.transform.parent != null) {
						Transform parent2 = hit.transform.parent.parent;
						if (parent2 != null) {

							if (parent2.gameObject.name.Equals (OBJ.ROAD)) {
								p.road = parent2.gameObject.GetComponent <RoadHandler>();

								//InRoadPosition
								p.inRoadPos = p.road.CheckInOutLen (transform.position);
							}
						}
					}
				}
			}
		}
		
		return p;
	}
	#endregion
}
