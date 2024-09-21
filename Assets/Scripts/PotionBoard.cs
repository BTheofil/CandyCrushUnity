using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
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

    public List<GameObject> potionsToDestroy = new();

    public GameObject potionParent;

    [SerializeField]
    List<Potion> potionsToRemove = new();

    [SerializeField]
    private Potion selectedPotion;

    [SerializeField]
    private bool isProcessingMove;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        InitializeBoard();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject.GetComponent<Potion>()) {
                if (isProcessingMove) {
                    return;
                }

                Potion potion = hit.collider.gameObject.GetComponent<Potion>();
                Debug.Log("Clicked potion " + potion.gameObject);

                SelectPotion(potion);
            }
        }
    }

    private void InitializeBoard() {

        DestroyPotions();

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
                    potion.transform.SetParent(potionParent.transform);

                    potion.GetComponent<Potion>().SetIndicies(x, y);
                    potionBoard[x, y] = new Node(true, potion);
                    potionsToDestroy.Add(potion);
                }

            }
        }
        if (CheckBoard()) {
            InitializeBoard();
        }
    }

    private void DestroyPotions() {
        if (potionsToDestroy != null) {
            foreach (GameObject potion in potionsToDestroy) {
                Destroy(potion);
            }
            potionsToDestroy.Clear();
        }
    }

    public bool CheckBoard() {

        if (GameManager.instance.isGameEnded) { 
            return false;
        }

        Debug.Log("Checking");
        bool hasMatched = false;

        potionsToRemove.Clear();

        foreach (Node nodePotion in potionBoard) {
            if (nodePotion.potion != null) {
                nodePotion.potion.GetComponent<Potion>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {

                if (potionBoard[x, y].isUsable) {
                    Potion potion = potionBoard[x, y].potion.GetComponent<Potion>();
               
                    if (!potion.isMatched) {                      
                        MatchResult matchedPotions = IsConnected(potion);

                        if (matchedPotions.connectedPotions.Count >= 3) {

                            MatchResult superMatchedPotions = SuperMatched(matchedPotions);

                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);

                            foreach (Potion pot in superMatchedPotions.connectedPotions) 
                                pot.isMatched = true;

                            hasMatched = true;
                        }
                    }
                }
            }
        }

        return hasMatched;
    }

    public IEnumerator ProcessTurnOnMatchedBoard(bool _subtractMoves) {
        foreach (Potion potionToRemove in potionsToRemove) {
            potionToRemove.isMatched = false;
        }

        RemoveAndRefill(potionsToRemove);
        GameManager.instance.ProcessTurn(potionsToRemove.Count, _subtractMoves);
        yield return new WaitForSeconds(0.4f);

        if (CheckBoard()) {
            StartCoroutine(ProcessTurnOnMatchedBoard(false));
        }
    }

    private MatchResult SuperMatched(MatchResult _matchedPotions) {

        if (_matchedPotions.direction == MatchDirection.Horizontal || _matchedPotions.direction == MatchDirection.LongHorizontal) {
            foreach (Potion pot in _matchedPotions.connectedPotions) {
                List<Potion> extraConnectedPotions = new();

                CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPotions);

                if (extraConnectedPotions.Count >= 2) {
                    Debug.Log("super horizontal match");
                    extraConnectedPotions.AddRange(_matchedPotions.connectedPotions);
                    return new MatchResult {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            return new MatchResult {
                connectedPotions = _matchedPotions.connectedPotions,
                direction = _matchedPotions.direction
            };
        } else if (_matchedPotions.direction == MatchDirection.Vertical || _matchedPotions.direction == MatchDirection.LongVertical) {
            foreach (Potion pot in _matchedPotions.connectedPotions) {
                List<Potion> extraConnectedPotions = new();

                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPotions);
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPotions);

                if (extraConnectedPotions.Count >= 2) {
                    Debug.Log("super vertical match");
                    extraConnectedPotions.AddRange(_matchedPotions.connectedPotions);
                    return new MatchResult {
                        connectedPotions = extraConnectedPotions,
                        direction = MatchDirection.Super
                    };
                }
            }
            return new MatchResult {
                connectedPotions = _matchedPotions.connectedPotions,
                direction = _matchedPotions.direction
            };
        }
        return null;
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

    #region Swapping Potions

    public void SelectPotion(Potion _potion) {
        if (selectedPotion == null) {
            Debug.Log(_potion);
            selectedPotion = _potion;
        } else if (selectedPotion == _potion) {
            selectedPotion = null;
        } else if (selectedPotion != _potion) {
            SwapPotion(selectedPotion, _potion);
            selectedPotion = null;
        }
    }

    private void SwapPotion(Potion _currentPotion, Potion _targetPotion) {

        if (!IsAdjectent(_currentPotion, _targetPotion)) {
            return;
        }

        DoSwap(_currentPotion, _targetPotion);
        isProcessingMove = true;

        StartCoroutine(ProcessMatches(_currentPotion, _targetPotion));
    }

    private void DoSwap(Potion _currentPotion, Potion _targetPotion) {
        GameObject temp = potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion;

        potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion = potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion;
        potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion = temp;

        int tempXIndex = _currentPotion.xIndex;
        int tempYIndex = _currentPotion.yIndex;
        _currentPotion.xIndex = _targetPotion.xIndex;
        _currentPotion.yIndex = _targetPotion.yIndex;  
        _targetPotion.xIndex = tempXIndex;
        _targetPotion.yIndex = tempYIndex;

        _currentPotion.MoveToTarget(potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion.transform.position);
        _targetPotion.MoveToTarget(potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion.transform.position);
    }

    private bool IsAdjectent(Potion _currentPotion, Potion _targetPotion) {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }

    private IEnumerator ProcessMatches(Potion _currentPotion, Potion _targetPotion) {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoard()) {
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        } else {
            DoSwap(_currentPotion, _targetPotion);
        }

        isProcessingMove = false;
    }

    #endregion

    #region Cascading Potions

    private void RemoveAndRefill(List<Potion> _potionsToRemove) {
        foreach (Potion potion in _potionsToRemove) {
            int _xIndex = potion.xIndex;
            int _yIndex = potion.yIndex;

            Destroy(potion.gameObject);

            potionBoard[_xIndex, _yIndex] = new Node(true, null);
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (potionBoard[x, y].potion == null) {
                    Debug.Log("The location X: " + x + "Y: " + y + "is empty");
                    RefillPotion(x, y);
                }
            }
        }
    }

    private void RefillPotion(int x, int y) {
        int yOffset = 1;
        while (y + yOffset < height && potionBoard[x, y + yOffset].potion == null) {
            yOffset++;
        }

        if (y + yOffset < height && potionBoard[x, y + yOffset].potion != null) {
            Potion potionAbove = potionBoard[x, y + yOffset].potion.GetComponent<Potion>();
            Vector3 targetPos = new Vector3(x - spaceingX, y - spaceingY, potionAbove.transform.position.z);
            potionAbove.MoveToTarget(targetPos);
            potionAbove.SetIndicies(x, y);
            potionBoard[x, y] = potionBoard[x, y + yOffset];
            potionBoard[x, y + yOffset] = new Node(true, null);
        }

        if (y + yOffset == height) {
            SpawnPotionTop(x);
        }
    }

    private void SpawnPotionTop(int x) {
        int index = FindIndexOfLowestNull(x);
        int locationToMove = 8 - index;
        int randomIndex = Random.Range(0, potionPrefabs.Length);

        GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spaceingX, height - spaceingY), Quaternion.identity);
        newPotion.transform.SetParent(potionParent.transform);

        newPotion.GetComponent<Potion>().SetIndicies(x, index);
        potionBoard[x, index] = new Node(true, newPotion);

        Vector3 targetPosition = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMove, newPotion.transform.position.z);
        newPotion.GetComponent<Potion>().MoveToTarget(targetPosition);
    }

    private int FindIndexOfLowestNull(int x) {
        int lowestNull = 99;
        for (int y = 7; y >= 0; y--) {
            if (potionBoard[x, y].potion == null) {
                lowestNull = y;
            }
        }

        return lowestNull;
    }

    #endregion
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
