using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GameScript : MonoBehaviour {

	#region behaviour separator variables

	public bool isPlayer;
	public bool isCamera;
	public bool isPickup;
	public bool canMove;
	public bool canJump;
	public bool canHurt;
	public bool canGrabPickups;
	public bool isEnemy;
	public bool isSightCone;
	public bool isFluid;
	public bool isLava;
	public bool isExplanation;

	#endregion

	#region other variables

	[SerializeField]
	private int fishCount;
	[SerializeField]
	private int health;
	[SerializeField]
	private int level;

	private bool isGrounded;
	private bool isPaused;
	private float speed;

	public GameObject pauseMenu;

	#region camera follow variables

	public GameObject player;
	private Vector3 offset;
	float damping = 2f;

	#endregion

	#region enemy variables

	public Transform[] patrolEndpoints;
	int nextPoint;
	bool stopPatrol;
	public Transform currentTarget;
	public GameScript enemyAttachedTo;

	#endregion

	#endregion

	// Use this for initialization
	void Start () {
		fishCount = 0;
		health = 10;
		nextPoint = 0;
		isGrounded = true;
		isPaused = false;
		if (isPlayer) { //Set player behaviour in case of failure to click a thing
			canMove = true;
			canJump = true;
			canGrabPickups = true;
			speed = 1.5f;
		}
		if (isCamera) { //Set camera behaviour
			offset = player.transform.position - transform.position;
		}
		if (isEnemy) {
			stopPatrol = false;
			canHurt = true;
			canMove = true;
			if (patrolEndpoints.Length != 0) {
				currentTarget = patrolEndpoints[nextPoint];
			}
		}
		if (isExplanation) {
			StartCoroutine(InstructionDisappear());
		}

		string currentScene = SceneManager.GetActiveScene().name;
		switch (currentScene) {
			case "Level1":
				level = 1;
				break;
			case "Level2":
				level = 2;
				break;
			case "Level3":
				level = 3;
				break;
			case "MainMenu":
				level = 0;
				break;
		}

	}
	
	// Update is called once per frame
	void Update () {
		if (isPlayer) {

			if(health <= 0) {
				//Ded, return to menu
				LoadLevel(0);
			}

			if(level==1 && fishCount == 5) {
				//Level 1 has 5 fishies
				Debug.Log("Level 1 complete!!!!");
				LoadLevel(2);
			}
			else if(level==2 && fishCount == 7) {
				//Level 2 has 7 fishies
				Debug.Log("Level 2 complete!!!!");
				LoadLevel(3);
			}
			else if(level==3 && fishCount == 10) {
				//Level 3 has 10 fishies
				Debug.Log("Level 3 complete!!!!");
				LoadLevel(0);
			}

			if (Input.GetKeyDown(KeyCode.Space) && canJump && isGrounded) {
				GetComponent<Rigidbody>().AddForce(0, 120.0f, 0);
				isGrounded = false;
				GetComponent<Animator>().SetBool("Moving", true);
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				//Pause/Unpause
				if (isPaused) {
					//Unpause
					pauseMenu.SetActive(false);
					Time.timeScale = 1;
				}
				else {
					//Pause
					pauseMenu.SetActive(true);
					Time.timeScale = 0;
				}
				isPaused = !isPaused;
			}

			if (canMove) {

				float x = Input.GetAxis("Horizontal") * Time.deltaTime * 80f;
				float z = Input.GetAxis("Vertical") * Time.deltaTime * speed;
				transform.Translate(0, 0, z);
				transform.Rotate(0, x, 0);

				if (z >= 0.001) {
					GetComponent<Animator>().SetBool("Moving", true);
				}
				else {
					GetComponent<Animator>().SetBool("Moving", false);
				}
			}
		}

		if (isEnemy) {
			//Patrol
			if (canMove && !stopPatrol && patrolEndpoints.Length != 0) {
				//Patroling
				if (IsSamePosition(transform.position, patrolEndpoints[nextPoint].position)) {
					//Go to next point.
					nextPoint++;
					nextPoint = nextPoint % patrolEndpoints.Length;
					currentTarget = patrolEndpoints[nextPoint];
				}
			}
			else if (canMove && stopPatrol) {
				//Pursue
			}

			float xrot = transform.localEulerAngles.x;
			float zrot = transform.localEulerAngles.z;

			if (currentTarget != null && canMove) {
				GetComponent<Animator>().SetBool("Moving", true);
				transform.LookAt(currentTarget);
				transform.localRotation = Quaternion.Euler(xrot, transform.localEulerAngles.y, zrot); //Keeps x and z rotation static after look at.
				float z = Time.deltaTime * 1.1f;
				transform.Translate(0, 0, z);
			}
			else {
				GetComponent<Animator>().SetBool("Moving", false);
			}
		}
	}

	private void LateUpdate() {
		if (isCamera) {
			float currentAngle = transform.eulerAngles.y;
			float desiredAngle = player.transform.eulerAngles.y;
			float angle = Mathf.LerpAngle(currentAngle, desiredAngle, Time.deltaTime * damping);

			Quaternion rotation = Quaternion.Euler(0, angle, 0);
			transform.position = player.transform.position - (rotation * offset);

			transform.LookAt(player.transform);
		}
	}

	private bool IsSamePosition(Vector3 obj, Vector3 pos) {
		float xdif = Mathf.Abs(obj.x - pos.x);
		float zdif = Mathf.Abs(obj.z - pos.z);
		if (xdif <= 0.1 && zdif <= 0.1) {
			return true;
		}
		else {
			return false;
		}
	}

	public void AddFish() {
		fishCount++;
	}

	public void UnPause() {
		pauseMenu.SetActive(false);
		isPaused = false;
		Time.timeScale = 1;
	}

	public void QuitGame() {
		Application.Quit();
		Debug.Log("Quit!");
	}

	private void OnTriggerEnter(Collider other) {
		if (isPickup) {
			if (other.GetComponent<GameScript>().canGrabPickups) {
				other.GetComponent<GameScript>().AddFish();
				GetComponent<ParticleSystem>().Play();
				GetComponent<AudioSource>().Play();
				GetComponent<Collider>().enabled = false;
				GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
				GetComponentInChildren<Light>().enabled = false;
				StartCoroutine("PickupFade");

			}
		}

		if (isSightCone) {
			if (other.tag.Equals("Ground")) {
				//Ignore
			}
			else if (other.GetComponent<GameScript>().isPlayer) {
				//Player sighted!  Pursue
				enemyAttachedTo.currentTarget = other.transform;
				enemyAttachedTo.stopPatrol = true;
			}
		}

		if (isFluid) {
			if (other.tag.Equals("Ground")) {
				//Ignore
			}
			else if (other.GetComponent<GameScript>().isPlayer) {
				//Player in water!  Lower gravity accordingly
				Physics.gravity = new Vector3(0, -2.8f, 0);
				other.GetComponent<GameScript>().speed = 1f;
			}
			else if (other.GetComponent<GameScript>().isEnemy) {
				//Enemy in water!  Cannot move!
				other.GetComponent<GameScript>().canMove = false;
				other.GetComponent<GameScript>().canHurt = false;
			}
		}

		if (isLava) {
			if (other.tag.Equals("Ground")) {
				//Ignore
			}
			else if (other.GetComponent<GameScript>().isPlayer) {
				//Player in lava!  Yeowch!
				other.GetComponent<GameScript>().health -= 4;
				other.GetComponent<Rigidbody>().AddForce(0, 120f, 0);
			}
			else if (other.GetComponent<GameScript>().isEnemy) {
				//Enemy in lava!  Ded
				Destroy(other.gameObject);
			}
		}

	}

	private void OnTriggerExit(Collider other) {
		if (isSightCone) {
			if (other.tag.Equals("Ground")) {
				//Ignore
			}
			else if (other.GetComponent<GameScript>().isPlayer) {
				//Player out of sight.  Begin losing track.
				StartCoroutine("EnemyForget");
			}
		}

		if (isFluid) {
			if (other.tag.Equals("Ground")) {
				//Ignore
			}
			else if (other.GetComponent<GameScript>().isPlayer) {
				//Player escaped water!  Restore gravity
				Physics.gravity = new Vector3(0, -9.81f, 0);
				other.GetComponent<GameScript>().speed = 1.5f;
			}
		}
	}

	private void OnCollisionEnter(Collision collision) {
		if (isPlayer) {
			if (collision.collider.tag.Equals("Ground")) {
				isGrounded = true;
			}
			else if (collision.collider.GetComponent<GameScript>().isEnemy) {
				//Oh noes!
				if (collision.collider.GetComponent<GameScript>().canHurt) {
					health--;
				}
			}
		}
	}

	private IEnumerator EnemyForget() {
		yield return new WaitForSeconds(0.1f);
		enemyAttachedTo.stopPatrol = false;
		if (enemyAttachedTo.patrolEndpoints.Length != 0) {
			enemyAttachedTo.currentTarget = enemyAttachedTo.patrolEndpoints[enemyAttachedTo.nextPoint];
		}
	}

	private IEnumerator PickupFade() {
		yield return new WaitForSeconds(0.9f);
		Destroy(gameObject);
	}

	public void LoadLevel(int levelNum) {
		switch (levelNum) {
			case 1:
				SceneManager.LoadScene("Level1");
				break;
			case 2:
				SceneManager.LoadScene("Level2");
				break;
			case 3:
				SceneManager.LoadScene("Level3");
				break;
			case 0:
				SceneManager.LoadScene("MainMenu");
				break;
		}
	}

	public IEnumerator InstructionDisappear() {
		yield return new WaitForSeconds(2f);
		gameObject.SetActive(false);
	}
}
