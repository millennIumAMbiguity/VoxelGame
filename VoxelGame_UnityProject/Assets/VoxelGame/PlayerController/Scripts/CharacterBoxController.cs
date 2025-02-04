using System;
using UnityEngine;

namespace VoxelGame.Player
{
	public class CharacterBoxController : MonoBehaviour
	{
		private const float COLLISION_MARGIN = 0.01f;

		public Vector3 Size = new Vector3(1, 1, 1);
		public Vector3 Center = new Vector3(0, 0, 0);

		[NonSerialized] private Vector3 _lastPosition;
		[NonSerialized] private LayerMask _collisionLayers;

		public Vector3 velocity { get; private set; }
		public bool isGrounded { get; private set; }
		public bool hitHead { get; private set; }

		private void Start()
		{
			UpdateCollisionLayers();
		}

		/// <summary>
		/// Update the collision layers based on the configured layer collision matrix.
		/// </summary>
		public void UpdateCollisionLayers()
		{
			_collisionLayers = 0;
			for (int j = 0; j < 32; j++)
			{
				if (!Physics.GetIgnoreLayerCollision(gameObject.layer, j))
				{
					_collisionLayers |= 1 << j;
				}
			}
		}

		public void Move(Vector3 motion)
		{
			if (motion == Vector3.zero)
			{
				velocity = Vector3.zero;
				return;
			}

			Vector3 pos = transform.position + Center;
			Vector3 size = Size / 2;

			isGrounded = false;
			hitHead = false;

			// Check each axis separately
			if (motion.x != 0)
			{
				if (Physics.BoxCast(pos, size, Vector3.right * Mathf.Sign(motion.x), out RaycastHit hit, Quaternion.identity, Mathf.Abs(motion.x) + COLLISION_MARGIN, _collisionLayers))
				{
					motion.x = Mathf.Sign(motion.x) * (hit.distance - COLLISION_MARGIN);
				}
			}

			if (motion.y != 0)
			{
				if (Physics.BoxCast(pos, size, Vector3.up * Mathf.Sign(motion.y), out RaycastHit hit, Quaternion.identity, Mathf.Abs(motion.y) + COLLISION_MARGIN, _collisionLayers))
				{
					isGrounded = motion.y < 0;
					hitHead = !isGrounded;
					motion.y = Mathf.Sign(motion.y) * (hit.distance - COLLISION_MARGIN);
				}
			}

			if (motion.z != 0)
			{
				if (Physics.BoxCast(pos, size, Vector3.forward * Mathf.Sign(motion.z), out RaycastHit hit, Quaternion.identity, Mathf.Abs(motion.z) + COLLISION_MARGIN, _collisionLayers))
				{
					motion.z = Mathf.Sign(motion.z) * (hit.distance - COLLISION_MARGIN);
				}
			}

			// Apply the adjusted movement
			transform.position += motion;

			// Calculate velocity
			velocity = (transform.position - _lastPosition) / Time.deltaTime;

			// Store the new position for the next frame
			_lastPosition = transform.position;
		}

#if UNITY_EDITOR
		/// <summary>
		/// Draw the collider in the editor.
		/// </summary>
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position + Center, Size);
		}
#endif
	}
}