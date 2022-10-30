using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int positionIndex;
    [HideInInspector]
    public Board board;


    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;

    private bool mousePressed;
    private float swipeAngle = 0;

    private Gem otherGem;

    public enum GemType { blue, green, red, yellow, purple, bomb, stone}
    public GemType type;

    public bool isMatched;
  
    private Vector2Int previousPosition;

    public GameObject destroyEffect;

    public int blastSize = 2;

    public int scoreValue = 10;

    void Start()
    {
        
    }

    
    void Update()
    {
        if(Vector2.Distance(transform.position, positionIndex) > 0.01f)
        {
            transform.position = Vector2.Lerp(transform.position, positionIndex, board.gemSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = new Vector3(positionIndex.x, positionIndex.y, 0f);
            board.allGems[positionIndex.x, positionIndex.y] = this;
        }
        
            
        if(mousePressed && Input.GetMouseButtonUp(0))
        {
            mousePressed = false;

            if(board.currentState == Board.BoardState.move && board.roundManager.roundTime > 0) 
            { 
                finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                CalculateAngle();
            }
        }
    }

    public void SetupGem(Vector2Int position, Board theBoard)
    {
        positionIndex = position;
        board = theBoard;
    }

    private void OnMouseDown()
    {
        if (board.currentState == Board.BoardState.move && board.roundManager.roundTime > 0)
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePressed = true;
        }
        
    }

    private void CalculateAngle()
    {
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x);
        swipeAngle = swipeAngle * 180 / Mathf.PI;

        if(Vector3.Distance(firstTouchPosition, finalTouchPosition) > .5f)
        {
            MovePieces();
        }
        
    }

    private void MovePieces()
    {
        previousPosition = positionIndex;

        if(swipeAngle < 45 && swipeAngle > -45 && positionIndex.x < board.width - 1)
        {
            otherGem = board.allGems[positionIndex.x + 1, positionIndex.y];
            otherGem.positionIndex.x--;
            positionIndex.x++;
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && positionIndex.y < board.height - 1)
        {
            otherGem = board.allGems[positionIndex.x, positionIndex.y + 1];
            otherGem.positionIndex.y--;
            positionIndex.y++;
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && positionIndex.y > 0)
        {
            otherGem = board.allGems[positionIndex.x, positionIndex.y - 1];
            otherGem.positionIndex.y++;
            positionIndex.y--;
        }
        else if (swipeAngle > 135 || swipeAngle < -45 && positionIndex.x > 0)
        {
            otherGem = board.allGems[positionIndex.x - 1, positionIndex.y];
            otherGem.positionIndex.x++;
            positionIndex.x--;
        }

        board.allGems[positionIndex.x, positionIndex.y] = this; // store current gem we move
        board.allGems[otherGem.positionIndex.x, otherGem.positionIndex.y] = otherGem;

        StartCoroutine(CheckMoveCoroutine());
    }

    public IEnumerator CheckMoveCoroutine()
    {
        board.currentState = Board.BoardState.wait;

        yield return new WaitForSeconds(.5f);

        board.matchFind.FindAllMatches();

        if(otherGem != null)
        {
            if (!isMatched && !otherGem.isMatched)
            {
                otherGem.positionIndex = positionIndex;
                positionIndex = previousPosition;

                board.allGems[positionIndex.x, positionIndex.y] = this; 
                board.allGems[otherGem.positionIndex.x, otherGem.positionIndex.y] = otherGem;

                yield return new WaitForSeconds(.5f);

                board.currentState = Board.BoardState.move;
            }
            else
            {
                board.DestroyMatches();
            }
        }
    }
}
