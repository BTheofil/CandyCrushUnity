using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour {

    public PotionType potionType;

    public int xIndex;
    public int yIndex;

    public bool isMatch;
    public bool isMoveing;

    private Vector2 currentPos;
    private Vector2 targetPos;

    public Potion(int x, int y) {
        xIndex = x;
        yIndex = y;
    }
}

public enum PotionType {
    Red,
    Blue,
    Purple,
    Green,
    White
}
