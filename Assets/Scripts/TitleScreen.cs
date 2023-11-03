using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public void NewGame()
    {
        GameManager.Instance.NewGame();
    }

    public void LoadGame()
    {
        GameManager.Instance.LoadGame();
    }
}
