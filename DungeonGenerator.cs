using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Import SceneManager to switch scenes

namespace DungeonCrawler
{
	[RequireComponent(typeof(CharacterController))]
	public class DungeonMazeGenerator : MonoBehaviour
	{
		[Header("Dungeon Generation")]
		public GameObject floorPrefab;
		public GameObject wallPrefab;
		public GameObject torchPrefab;  // Add wall torch prefab
		public GameObject cratePrefab;  // Add crate prefab
		public GameObject exitObjectPrefab; // Add exit object (e.g., chest or door)

		public GameObject floorParent;
		public GameObject wallsParent;
		public GameObject propsParent;  // New parent for props (torches/crates)

		public GameObject myPlayerObject;

		int roomSize = 8;  // Size of each room
		int gridSize = 21; // Smaller grid for maze (ensure odd gridSize for maze structure)
		bool[,] mapData;

		[Header("Player Settings")]
		public float MoveSpeed = 2.0f;
		public float SprintSpeed = 5.335f;
		public float JumpHeight = 1.2f;
		public float Gravity = -15.0f;

		private CharacterController _controller;
		private float _verticalVelocity;

		private void Start()
		{
			_controller = myPlayerObject.GetComponent<CharacterController>();

			// Generate maze with rooms and passageways
			mapData = GenerateMazeData();
			GenerateDungeon();

			// Place player at entry point and set exit point
			PlaceEntryPoint();  // Create entry point and place player there
			PlaceExitPoint();   // Create exit point

			// Add torches and crates to some rooms
			PlaceProps();
		}

		private void Update()
		{
			HandleMovement();
			ApplyGravity();
		}

		private void HandleMovement()
		{
			float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : MoveSpeed;

			// Gather input
			float moveX = Input.GetAxis("Horizontal");
			float moveZ = Input.GetAxis("Vertical");
			Vector3 _inputDirection = new Vector3(moveX, 0.0f, moveZ).normalized;

			if (_inputDirection.magnitude >= 0.1f)
			{
				Vector3 moveDirection = myPlayerObject.transform.forward * targetSpeed * Time.deltaTime;
				_controller.Move(moveDirection);
			}

			if (_controller.isGrounded && Input.GetButtonDown("Jump"))
			{
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			}

			// Apply vertical velocity (falling or jumping)
			Vector3 velocity = new Vector3(0, _verticalVelocity, 0);
			_controller.Move(velocity * Time.deltaTime);
		}

		private void ApplyGravity()
		{
			if (_controller.isGrounded && _verticalVelocity < 0)
			{
				_verticalVelocity = -2f; // Ensures the player sticks to the ground
			}
			else
			{
				_verticalVelocity += Gravity * Time.deltaTime; // Applies gravity
			}
		}

		// Generate the dungeon layout with rooms and maze-like structure
		void GenerateDungeon()
		{
			for (int z = 0; z < gridSize; z++)
			{
				for (int x = 0; x < gridSize; x++)
				{
					if (mapData[z, x])
					{
						// Create walls
						CreateChildPrefabInstance(wallPrefab, wallsParent, new Vector3(x, 1, z));
						CreateChildPrefabInstance(wallPrefab, wallsParent, new Vector3(x, 2, z));
					}
					else
					{
						// Create floor
						CreateChildPrefabInstance(floorPrefab, floorParent, new Vector3(x, 0, z));
					}
				}
			}
		}

		// Place the player at the entry point, which is (1, 1)
		void PlaceEntryPoint()
		{
			int startX = 1;
			int startZ = 1;

			// Ensure there's a floor at the entry point and no walls
			Instantiate(floorPrefab, new Vector3(startX, 0, startZ), Quaternion.identity);
			myPlayerObject.transform.position = new Vector3(startX, 1, startZ);  // Place the player
		}

		// Place the exit point at the last room
		void PlaceExitPoint()
		{
			int exitX = gridSize - 2;
			int exitZ = gridSize - 2;

			// Ensure there's a floor at the exit point and no walls
			Instantiate(floorPrefab, new Vector3(exitX, 0, exitZ), Quaternion.identity);

			// Instantiate the exit object (e.g., chest or door)
			GameObject exitObject = Instantiate(exitObjectPrefab, new Vector3(exitX, 0.5f, exitZ), Quaternion.identity);
			exitObject.transform.SetParent(propsParent.transform);

			// Add a trigger to detect when the player reaches the end
			BoxCollider trigger = exitObject.AddComponent<BoxCollider>();
			trigger.isTrigger = true; // Make it a trigger
			trigger.size = new Vector3(1.5f, 1.5f, 1.5f); // Adjust trigger size to cover the object

			// Tag the exit object for trigger detection
			exitObject.tag = "Exit"; // Assign the "Exit" tag to the chest/exit object
		}

		// Detect when the player reaches the exit point
		private void OnTriggerEnter(Collider other)
		{
			// Check if the player has collided with the exit object
			if (other.CompareTag("Exit")) // If player reaches the chest/exit object
			{
				// Load the End Scene 
				SceneManager.LoadScene("EndGame");
			}
		}

		// Maze generation using recursive backtracking algorithm
		bool[,] GenerateMazeData()
		{
			bool[,] data = new bool[gridSize, gridSize];

			// Initialize all cells as walls
			for (int y = 0; y < gridSize; y++)
			{
				for (int x = 0; x < gridSize; x++)
				{
					data[y, x] = true; // Fill with walls
				}
			}

			// Carve out maze
			CarvePassagesFrom(1, 1, data); // Start at (1, 1) to begin carving

			return data;
		}

		// Carve passages in the maze using recursive backtracking
		void CarvePassagesFrom(int currentX, int currentZ, bool[,] maze)
		{
			// Randomized directions
			int[] directions = { 0, 1, 2, 3 };
			Shuffle(directions);

			// Movement vectors for directions: North, East, South, West
			int[] dx = { 0, 2, 0, -2 };
			int[] dz = { -2, 0, 2, 0 };

			for (int i = 0; i < directions.Length; i++)
			{
				int dir = directions[i];
				int newX = currentX + dx[dir];
				int newZ = currentZ + dz[dir];

				// Ensure the new position is within the maze boundaries
				if (newX > 0 && newX < gridSize - 1 && newZ > 0 && newZ < gridSize - 1)
				{
					if (maze[newZ, newX]) // If it's still a wall
					{
						// Knock down the wall between current and new cell
						maze[currentZ + dz[dir] / 2, currentX + dx[dir] / 2] = false;
						maze[newZ, newX] = false;

						// Recursively carve passages from the new cell
						CarvePassagesFrom(newX, newZ, maze);
					}
				}
			}
		}

		// Shuffle directions to randomize maze
		void Shuffle(int[] array)
		{
			for (int i = array.Length - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				int temp = array[i];
				array[i] = array[j];
				array[j] = temp;
			}
		}

		// Place random props like torches and crates in some rooms
		void PlaceProps()
		{
			// Go through the map and place props in certain rooms randomly
			for (int z = 1; z < gridSize - 1; z += 2)
			{
				for (int x = 1; x < gridSize - 1; x += 2)
				{
					if (!mapData[z, x]) // Only place in empty floor tiles (rooms)
					{
						float randValue = Random.value;

						if (randValue < 0.2f) // 20% chance to place a crate
						{
							Vector3 cratePosition = new Vector3(x, 0, z);
							CreateChildPrefabInstance(cratePrefab, propsParent, cratePosition);
						}
						else if (randValue < 0.3f) // 10% chance to place a torch
						{
							// Adjust torch placement to be on the wall and higher
							Vector3 torchPosition = new Vector3(x + 0.5f, 2.5f, z); // Higher position (2.5) to avoid player
							GameObject torch = CreateChildPrefabInstance(torchPrefab, propsParent, torchPosition);
							torch.transform.localScale *= 0.5f; // Scale down the torch size
						}
					}
				}
			}
		}

		// Helper method to instantiate prefabs
		GameObject CreateChildPrefabInstance(GameObject prefab, GameObject parent, Vector3 spawnPosition)
		{
			var newGameObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
			newGameObject.transform.parent = parent.transform;
			return newGameObject;
		}
	}
}
