using System.Collections;
using System.Collections.Generic;
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
    }
}
