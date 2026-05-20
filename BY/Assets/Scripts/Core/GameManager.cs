using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ManagerOrder(2)]
public class GameManager : SingletonInstance<GameManager>, IManager
{

    public void StartGame()
    {}
}

