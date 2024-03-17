using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerResultItem : MonoBehaviour
{
	public Text placementText;
	public Text nameText;
    public Text ordersText;

    public void SetResult(string name, int orders, int place)
    {
        placementText.text = 
            place == 1 ? "1st" :
            place == 2 ? "2nd" :
            place == 3 ? "3rd" :
            $"{place}th";   
        nameText.text = name;
        ordersText.text = orders.ToString();
    }
}
