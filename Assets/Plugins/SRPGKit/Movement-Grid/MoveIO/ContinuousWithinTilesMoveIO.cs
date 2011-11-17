using UnityEngine;
using System.Collections;

[RequireComponent (typeof(CharacterController))]
public class ContinuousWithinTilesMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;

	public float moveSpeed = 12;

	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;

	GridOverlay overlay;
	CharacterController cc;
	
	public Vector3 moveDest=Vector3.zero;
	
	override public void Start() {
		base.Start();
		cc = GetComponent<CharacterController>();
		//HACK: 0.09f here is a hack for the charactercontroller collider
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y+0.09f, transform.localPosition.z);
	}

	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		//click to move within area, move the character itself around
		//keyboard to move within area, move the character itself around, prevent movement outside of overlay area
		//end with mouse down on character or return/space key
		if(!map.arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(supportMouse && Input.GetMouseButton(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			if(inside && overlay.ContainsPosition(hitSpot)) {
				moveDest = hitSpot;
				if(Time.time-firstClickTime > doubleClickThreshold) {
					firstClickTime = Time.time;
					TemporaryMove(moveDest);
				} else {
					firstClickTime = -1;
					TemporaryMove(moveDest);
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			//move in terms of world coords on player
			Transform cameraTransform = Camera.main.transform;
			Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;
			Vector3 right = new Vector3(forward.z, 0, -forward.x);
			Vector3 targetDirection = h * right + v * forward;
			cc.SimpleMove(targetDirection*moveSpeed);
			//another approach (instead of ContainsPosition): place invisible box colliders at the edges of tiles which shouldn't be crossed
			//HACK: 5.09f here is a hack for the charactercollider
			Vector3 newDest = map.InverseTransformPointWorld(character.transform.position-new Vector3(0,5.09f,0));
/*			Debug.Log("Dest: " + newDest + " Inside? " + overlay.ContainsPosition(newDest));*/
			//TODO: something with isGrounded to prevent falling off the world
			PathNode pn = overlay.PositionAt(newDest);
			if(pn != null && pn.canStop) {
				moveDest = newDest;
				TemporaryMove(moveDest);
			} else if(overlay.ContainsPosition(moveDest)) {
				//moveDest is still the old one
				TemporaryMove(moveDest);
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			PerformMove(moveDest);
		}
	}
		
	override protected void PresentMoves() {
		PathNode[] destinations = GetComponent<GridMoveStrategy>().GetValidMoves();
		overlay = map.PresentGridOverlay(
			"move", this.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			new Color(0.4f, 0.6f, 0.9f, 0.7f),
			destinations
		);
	}

	override protected void FinishMove() {
		overlay = null;
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
}
