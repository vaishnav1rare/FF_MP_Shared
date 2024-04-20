using UnityEngine;

namespace OneRare.FoodFury.Multiplayer
{
	public class ForceField : MonoBehaviour
	{
		[SerializeField] Material forceFieldMaterial;

		[SerializeField] MeshRenderer meshRenderer;

		string[] toggleProperties = new string[8] {"_PLAYER1Toggle", "_PLAYER2Toggle", "_PLAYER3Toggle", "_PLAYER4Toggle","_PLAYER5Toggle","_PLAYER6Toggle","_PLAYER7Toggle","_PLAYER8Toggle"};
		string[] positionProperties = new string[8] {"_PositionPLAYER1", "_PositionPLAYER2", "_PositionPLAYER3", "_PositionPLAYER4", "_PositionPLAYER5", "_PositionPLAYER6", "_PositionPLAYER7", "_PositionPLAYER8"};

		void Awake()
		{
			forceFieldMaterial = new Material(forceFieldMaterial);
			meshRenderer.material = forceFieldMaterial;
		}

		public void SetPlayer(int index, MonoBehaviour behaviour)
		{
			if (behaviour != null)
				forceFieldMaterial.SetVector(positionProperties[index], behaviour.transform.position);
			forceFieldMaterial.SetInt(toggleProperties[index], behaviour==null ? 0 : 1);
		}
	}
}