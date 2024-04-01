
using UnityEngine;
using UnityEngine.UI;

public class OrderCollectedItem : MonoBehaviour
{
    public Text nameText;
    
    public void SetResult(string name)
    {
        nameText.text = name + " Collected!";
        Destroy(gameObject,2f);
    }
}
