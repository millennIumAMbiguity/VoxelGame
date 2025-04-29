using System;
using UnityEngine;

namespace VoxelGame.Player
{
	public class HandController : MonoBehaviour
	{
		public float MinHandSway = 1f;

		[NonSerialized] public float HandSwaySpeed;

		[NonSerialized] public Vector2 HandOffset;

		private float _useTime;
		private float _useTimeOffset;
		private float _swayTime;
		private Vector3 _basePos;

		[ContextMenu("Use")]
		public void Use()
		{
			if (_useTime > Mathf.PI / 3f)
			{
				if (_useTime < Mathf.PI)
					_useTime += Mathf.PI;
			}
			else
			{
				_useTime = Mathf.PI;
			}
		}

		private void Start()
		{
			_basePos = transform.localPosition;
		}

		private void LateUpdate()
		{
			float swayTPi = _swayTime / Mathf.PI;
			float swayBaseStrength = Mathf.Min(MinHandSway, _swayTime);
			transform.localEulerAngles = new Vector3(
				Mathf.Cos(swayTPi) * swayBaseStrength * 2f + Mathf.Abs(Mathf.Sin(_useTime)) * -10f,
				Mathf.Sin(swayTPi * 0.5f) * swayBaseStrength * 2f + Mathf.Sin(_useTime * 2) * -5f,
				Mathf.Sin(swayTPi) * swayBaseStrength
			);
			transform.localPosition = _basePos + new Vector3(
				HandOffset.x,
				HandOffset.y,
				Mathf.Abs(Mathf.Sin(_useTime)) * 0.1f
			);

			_swayTime += HandSwaySpeed;
			HandSwaySpeed = Mathf.Lerp(HandSwaySpeed, 0, Time.deltaTime * 2f);

			_useTime = Mathf.Max(0, _useTime - Time.deltaTime * 10f);
		}

		private void OnEnable()
		{
			transform.GetChild(0).gameObject.SetActive(true);
		}

		private void OnDisable()
		{
			transform.GetChild(0).gameObject.SetActive(false);
		}
	}
}