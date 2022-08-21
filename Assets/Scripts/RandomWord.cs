using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RandomWord : MonoBehaviour
{
    public TextMesh largeText;

    public void BtnAction(){
        PickRandomFromList();
    }

    private void PickRandomFromList() {
        string[] words = new string[] { "Sabon", "SunFlower", "Humberger", "Laptop", "Tiger", "Dolpine", "Aircon", "Toyota", "Nike", 
        "Door Knob", "Apple", "TV", "Iphone", "Baso", "Lapis", "ToothBrush", "Mouse", "Eagle", "Ice Cream", "Pop Corn",
        "Toe", "mug", "Tsinelas", "Eyebrow", "Chin", "Ice", "Eyelash"};
        string randomWord = words[Random.Range(0, words.Length)];
        largeText.text= randomWord;
    }
}