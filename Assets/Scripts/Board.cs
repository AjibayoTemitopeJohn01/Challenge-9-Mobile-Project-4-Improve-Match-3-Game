using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    /// <summary>
    /// Tiles
    /// </summary>
    public int width;
    public int height;
    public GameObject bgTilePrefab;
    /// <summary>
    /// Gems
    /// </summary>
    public Gem[] gems;
    public Gem[,] allGems; // will store x&y value for each gem
    public float gemSpeed;
    public Gem bomb;
    public float bombChance = 2f;
    /// <summary>
    /// Matching
    /// </summary>
    [HideInInspector]
    public MatchFinder matchFind;
    /// <summary>
    /// Board state
    /// </summary>
    public enum BoardState {wait, move}
    public BoardState currentState = BoardState.move;
    [HideInInspector]
    public RoundManager roundManager;
    /// <summary>
    /// bonuses
    /// </summary>
    private float bonusMultiply;
    public float bonusAmount = .5f;

    private BoardLayout boardLayout;
    private Gem[,] layoutStore;

    private void Awake()
    {
        matchFind = FindObjectOfType<MatchFinder>();
        roundManager = FindObjectOfType<RoundManager>();
        boardLayout = GetComponent<BoardLayout>();
    }
    void Start()
    {
        allGems = new Gem[width, height];

        layoutStore = new Gem[width, height];

        Setup();
    }

    private void Update()
    {
        // matchFind.FindAllMatches();

        if (Input.GetKeyDown(KeyCode.S))
        {
            ShuffleBoard();
        }
    }
    /// <summary>
    /// Create BG
    /// </summary>
    private void Setup()
    {
        if(boardLayout != null)
        {
            layoutStore = boardLayout.GetLayout();
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2(x, y);
                GameObject bgTile = Instantiate(bgTilePrefab, position, Quaternion.identity);
                bgTile.transform.parent = transform; // attaching bricks in main tile
                bgTile.name = "BG Tile - " + x + ", " + y; // for more correct naming tiles

                if(layoutStore[x,y] != null)
                {
                    SpawnGem(new Vector2Int(x,y), layoutStore[x,y]);
                }
                else 
                { 
                int gemToUse = Random.Range(0, gems.Length);

                int iterations = 0; // crutch to prevent potential crash
                while (MatchesAt(new Vector2Int(x, y), gems[gemToUse]) && iterations < 100)
                {
                    gemToUse = Random.Range(0, gems.Length);
                    iterations++;
                }

                SpawnGem(new Vector2Int(x, y), gems[gemToUse]);
                }
            }
        }
    }

    private void SpawnGem(Vector2Int position, Gem gemToSpawn)
    {
        if(Random.Range(0f, 100f) < bombChance) // bomb spawn chance
        {
            gemToSpawn = bomb;
        }

        Gem gem = Instantiate(gemToSpawn, new Vector3(position.x, position.y + height, 0f), Quaternion.identity);
        gem.transform.parent = this.transform;
        gem.name = "Gem - " + position.x + ", " + position.y;
        allGems[position.x, position.y] = gem;

        gem.SetupGem(position, this); // this means we will use current board
    }
    /// <summary>
    /// MatchesGem
    /// </summary>
    /// <param name="positionToCheck"></param>
    /// <param name="gemToCheck"></param>
    /// <returns></returns>
    bool MatchesAt(Vector2Int positionToCheck, Gem gemToCheck)
    {
        if (positionToCheck.x > 1)
        {
            if (allGems[positionToCheck.x - 1, positionToCheck.y].type == gemToCheck.type && allGems[positionToCheck.x - 2, positionToCheck.y].type == gemToCheck.type)
            {
                return true;
            }
        }

        if (positionToCheck.y > 1)
        {
            if (allGems[positionToCheck.x, positionToCheck.y - 1].type == gemToCheck.type && allGems[positionToCheck.x, positionToCheck.y - 2].type == gemToCheck.type)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// DestroyingGems
    /// </summary>
    /// <param name="position"></param>
    private void DestroyMatchedGemAt(Vector2Int position)
    {
        if (allGems[position.x, position.y] != null) // checking all gems
        {
            if (allGems[position.x, position.y].isMatched) // doublecheck
            {
                if(allGems[position.x, position.y].type == Gem.GemType.bomb)
                {
                    SFXManager.instance.PlayExplode();
                }
                else if (allGems[position.x, position.y].type == Gem.GemType.stone)
                {
                    SFXManager.instance.PlayStoneBreak();
                }
                else 
                {
                    SFXManager.instance.PlayGemBreak();
                }

                Instantiate(allGems[position.x, position.y].destroyEffect, new Vector2(position.x, position.y), Quaternion.identity);

                Destroy(allGems[position.x, position.y].gameObject);
                allGems[position.x, position.y] = null;
            }
        }

    }

    public void DestroyMatches()
    {
        for (int i = 0; i < matchFind.currentMatches.Count; i++)
        {
            if (matchFind.currentMatches[i] != null) // checking list
            {
                ScoreCheck(matchFind.currentMatches[i]);

                DestroyMatchedGemAt(matchFind.currentMatches[i].positionIndex);
            }
        }

        StartCoroutine(DecreaseRowCoroutine());
    }
    /// <summary>
    /// FallingGems
    /// </summary>
    /// <returns></returns>
    private IEnumerator DecreaseRowCoroutine()
    {
        yield return new WaitForSeconds(.2f); // delay before explosion gems

        int nullCounter = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allGems[x, y] == null)
                {
                    nullCounter++;
                }
                else if (nullCounter > 0)
                {
                    allGems[x, y].positionIndex.y -= nullCounter;
                    allGems[x, y - nullCounter] = allGems[x, y];
                    allGems[x, y] = null;
                }
            }

            nullCounter = 0;
        }

        StartCoroutine(FillBoardCoroutine());
    }
    /// <summary>
    /// RefillingGems
    /// </summary>
    /// <returns></returns>
    private IEnumerator FillBoardCoroutine() // refilling/matching logic, cascade effect
    {
        yield return new WaitForSeconds(.5f);
        RefillBoard();

        yield return new WaitForSeconds(.5f);

        matchFind.FindAllMatches();

        if (matchFind.currentMatches.Count > 0)
        {
            bonusMultiply++;

            yield return new WaitForSeconds(.5f);
            DestroyMatches();
        }
        else
        {
            yield return new WaitForSeconds(.5f);
            currentState = BoardState.move;

            bonusMultiply = 0f;
        }
    }

    private void RefillBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (allGems[x, y] == null)
                {
                    int gemToUse = Random.Range(0, gems.Length);

                    SpawnGem(new Vector2Int(x, y), gems[gemToUse]);
                }
            }
        }
        CheckMisplacedGems();
    }
    /// <summary>
    /// Another check after refill, will replace after game is finished
    /// </summary>
    private void CheckMisplacedGems() // found and remove duplicate of gems
    {
        List<Gem> foundGems = new List<Gem>();

        foundGems.AddRange(FindObjectsOfType<Gem>());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (foundGems.Contains(allGems[x, y]))
                {
                    foundGems.Remove(allGems[x, y]);
                }
            }
        }

        foreach(Gem gem in foundGems)
        {
            Destroy(gem.gameObject);
        }
    }
    /// <summary>
    /// Shuffle board...
    /// </summary>
    public void ShuffleBoard()
    {
        if (currentState != BoardState.wait)
        {
            currentState = BoardState.wait;

            List<Gem> gemsFromBoard = new List<Gem>();

            for (int x = 0; x < width; x++) // strip out gems
            {
                for (int y = 0; y < height; y++)
                {
                    gemsFromBoard.Add(allGems[x, y]);
                    allGems[x, y] = null;
                }
            }

            // two "for" loops for being sure that our board completly empty before we pu back the gems

            for (int x = 0; x < width; x++) // put back gems
            {
                for (int y = 0; y < height; y++)
                {
                    int gemToUse = Random.Range(0, gemsFromBoard.Count);

                    int iterations = 0;
                    while(MatchesAt(new Vector2Int(x,y), gemsFromBoard[gemToUse]) && iterations < 100 && gemsFromBoard.Count > 1)
                    {
                        gemToUse = Random.Range(0, gemsFromBoard.Count);
                        iterations++;
                    }

                    gemsFromBoard[gemToUse].SetupGem(new Vector2Int(x, y), this);
                    allGems[x, y] = gemsFromBoard[gemToUse];
                    gemsFromBoard.RemoveAt(gemToUse);
                }
            }
            StartCoroutine(FillBoardCoroutine());
        }
    }

    public void ScoreCheck(Gem gemToCheck)
    {
        roundManager.currentScore += gemToCheck.scoreValue;

        if(bonusMultiply > 0)
        {
            float bonusToAdd = gemToCheck.scoreValue * bonusMultiply * bonusAmount;
            roundManager.currentScore += Mathf.RoundToInt(bonusToAdd);
        }
    }
}