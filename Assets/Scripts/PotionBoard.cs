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

        bool hasMatch = CheckBoard();

        if (!hasMatch) {
            DoSwap(_currentPotion, _targetPotion);
        }
        isProcessingMove = false;
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
