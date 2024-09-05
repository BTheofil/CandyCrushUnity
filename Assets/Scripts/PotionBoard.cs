using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PotionBoard : MonoBehaviour {

    public int width = 6;
    public int height = 8;

    public float spaceingX;
    public float spaceingY;

    public GameObject[] potionPrefabs;

    private Node[,] potionBoard;
    public GameObject potionBoardGO;

    public ArrayLayout arrayLayout;

    public static PotionBoard Instance;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        InitializeBoard();
    }

    private void InitializeBoard() {
        
        potionBoard = new Node[width, height];

        spaceingX = (float)(width - 1 ) / 2;
        spaceingY = (float)((height - 1) / 2) + 1;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector2 position = new Vector2(x - spaceingX, y - spaceingY);
                if (arrayLayout.rows[y].row[x]) {
                    potionBoard[x, y] = new Node(false, null);
                } else {
                    int randomIndex = Random.Range(0, potionPrefabs.Length);

                    GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                    potion.GetComponent<Potion>().SetIndicies(x, y);
                    potionBoard[x, y] = new Node(true, potion);
                }

            }
        }
        CheckBoard();
    }

    public bool CheckBoard() {
        Debug.Log("Checking");
        bool hasMatched = false;

        List<Potion> potionsToRemove = new();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {

                if (potionBoard[x, y].isUsable) {
                    Potion potion = potionBoard[x, y].potion.GetComponent<Potion>();

                    if (!potion.isMatched) {
                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3) {
                            potionsToRemove.AddRange(matchedPotions.connectedPotions);

                            foreach (Potion pot in matchedPotions.connectedPotions) 
                                pot.isMatched = true;

                            hasMatched = true;
                        }
                    }
                }
            }
        }

        return hasMatched;
    }

    MatchResult IsConnected(Potion potion) {
        List<Potion> connectedPotions = new();
        PotionType potionType = potion.potionType;

        connectedPotions.Add(potion);

        //check right
        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);
        //check left
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);
        //have we made a 3 match? (Horizontal Match)
        if (connectedPotions.Count == 3) {
            Debug.Log("I have a normal horizontal match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Horizontal
            };
        }
        //checking for more than 3 (Long horizontal Match)
        else if (connectedPotions.Count > 3) {
            Debug.Log("I have a Long horizontal match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongHorizontal
            };
        }
        //clear out the connectedpotions
        connectedPotions.Clear();
        //readd our initial potion
        connectedPotions.Add(potion);

        //check up
        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);
        //check down
        CheckDirection(potion, new Vector2Int(0, -1), connectedPotions);

        //have we made a 3 match? (Vertical Match)
        if (connectedPotions.Count == 3) {
            Debug.Log("I have a normal vertical match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Vertical
            };
        }
        //checking for more than 3 (Long Vertical Match)
        else if (connectedPotions.Count > 3) {
            Debug.Log("I have a Long vertical match, the color of my match is: " + connectedPotions[0].potionType);

            return new MatchResult {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongVertical
            };
        } else {
            return new MatchResult {
                connectedPotions = connectedPotions,
                direction = MatchDirection.None
            };
        }
    }

    void CheckDirection(Potion pot, Vector2Int direction, List<Potion> connectedPotions) {
        PotionType potionType = pot.potionType;
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        while (x >= 0 && x < width && y >= 0 && y < height) {
            if (potionBoard[x, y].isUsable) {
                Potion neighbourPotion = potionBoard[x, y].potion.GetComponent<Potion>();

                if (!neighbourPotion.isMatched && neighbourPotion.potionType == potionType) {
                    connectedPotions.Add(neighbourPotion);

                    x += direction.x;
                    y += direction.y;
                } else {
                    break;
                }
            } else {
                break;
            }
        }
    }
}

public class MatchResult {

    public List<Potion> connectedPotions;
    public MatchDirection direction;
}

public enum MatchDirection {

    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Super,
    None
}
