using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameController : MonoBehaviour
{
    public Vector2 arraySize = new Vector2(10, 22);
    public Transform spawnPosition;
    public Transform nextBlockPreviewPosition;
    public TextMesh scoreValueText;
    public TextMesh rowsValueText;
    public GameObject gameOverText;
    public GameObject pausedText;
    public GameObject namePanel;
    public InputField namePanelText;
    public GameObject[] blockList;
    public float speedupCoeficient = 4;

    public GameObject debugQuad;

    public float FallSpeed { get { return fallSpeed; } } private float fallSpeed;
    public bool IsPaused { get { return isPaused; } } private bool isPaused;

    private GameObject nextBlock;
    private int nextBlockID;
    private int score;
    private int rows;
    private GameObject[,] playGrid;
    private bool gameOver;
    private bool isNamePanelOpen;
    private Bounds positionBoundaries;

    void Start()
    {
        score = 0;
        rows = 0;
        scoreValueText.text = score + "";
        rowsValueText.text = rows + "";
        positionBoundaries = new Bounds(new Vector3(spawnPosition.transform.position.x + 0.5f, spawnPosition.transform.position.y - arraySize.x - 0.5f), new Vector3(arraySize.x, arraySize.y, 1));
        playGrid = new GameObject[(int)arraySize.x, (int)arraySize.y];
        gameOver = false;
        isPaused = false;
        isNamePanelOpen = false;
        gameOverText.SetActive(false);
        pausedText.SetActive(isPaused);
        namePanel.SetActive(isNamePanelOpen);
        UpdateFallSpeed();
        Spawn(Random.Range(0, blockList.Length));
        GenerateAndShowNextBlock();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !gameOver)
        {
            isPaused = !isPaused;
            pausedText.SetActive(isPaused);
        }

        if (Input.GetKeyDown(KeyCode.R) && !isNamePanelOpen)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnDrawGizmos()
    {
        if (Debug.isDebugBuild)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(positionBoundaries.center, positionBoundaries.size);
        }
    }

    public void OnBlockFalled(GameObject block)
    {
        PlaceBlockToGrid(block);
        int deletedRows = CheckFullRows();
        if (deletedRows > 0)
        {
            rows += deletedRows;
            score += GetScoreFromRows(deletedRows);
            scoreValueText.text = score + "";
            rowsValueText.text = rows + "";
            UpdateFallSpeed();
            CleanUpObjects();
        }
        if (!gameOver)
        {
            Spawn(nextBlockID);
            GenerateAndShowNextBlock();
        }
    }

    public bool IsInsideGrid(GameObject block)
    {
        Transform[] childTransforms = block.GetComponentsInChildren<Transform>();
        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] == block.transform)
                continue;
            if (!positionBoundaries.Contains(childTransforms[i].position))
                return false;
        }
        return true;
    }

    public bool IsOverlapringAnotherBlock(GameObject block)
    {
        Transform[] childTransforms = block.GetComponentsInChildren<Transform>();

        for (int j = 0; j < childTransforms.Length; j++)
        {
            if (childTransforms[j] == block.transform)
                continue;

            Vector3 position = spawnPosition.transform.position - childTransforms[j].position;
            position.x += (arraySize.x / 2f);

            if (playGrid[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y)] != null)
                return true;
        }
        return false;
    }

    public void UpdateFallSpeed()
    {
        //fallSpeed = Mathf.Pow(speedupCoeficient, -(score / 50000f));
        fallSpeed = Mathf.Pow(1 + ((speedupCoeficient - 1) / 300f) * rows, -1);
    }

    private void Spawn(int idx)
    {
        if (gameOver)
        {
            Debug.Log("Trying to spawn block even if game is over");
            return;
        }
        GameObject block = Instantiate(blockList[idx], transform);
        block.transform.SetPositionAndRotation(spawnPosition.position, Quaternion.identity);
        for (int i = 0; i < arraySize.y && !IsInsideGrid(block); i++)
        {
            block.transform.position += Vector3.down; //move down
        }
        if (!IsInsideGrid(block))
        {
            Debug.Log("Cannot fit inside grid.");
            Destroy(block);
            return;
        }
        if (IsOverlapringAnotherBlock(block))
        {
            Destroy(block);
            GameOver();
            return;
        }
    }

    private void GameOver()
    {
        gameOver = true;
        gameOverText.SetActive(true);
        OpenNamePanel();
    }

    private void OpenNamePanel()
    {
        isNamePanelOpen = true;
        namePanelText.text = PersistentData.name;
        namePanel.SetActive(isNamePanelOpen);
    }

    private void CloseNamePanel()
    {
        isNamePanelOpen = false;
        namePanel.SetActive(isNamePanelOpen);
    }

    private void CleanUpObjects()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Block");
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].GetComponentsInChildren<Transform>().Length <= 1)
                Destroy(objects[i]);
        }
    }

    private void GenerateAndShowNextBlock()
    {
        Destroy(nextBlock);
        nextBlockID = Random.Range(0, blockList.Length);
        nextBlock = (GameObject)Instantiate(blockList[nextBlockID], nextBlockPreviewPosition);
        Bounds B = nextBlock.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((b1, b2) => { b1.Encapsulate(b2); return b1; });
        nextBlock.transform.position += nextBlock.transform.position - B.center;
        nextBlock.GetComponent<Block>().enabled = false;
    }

    private int GetScoreFromRows(int rows)
    {
        return (int)((Mathf.Pow(2, rows) - 1) * 100);
    }

    private void PlaceBlockToGrid(GameObject block)
    {
        Transform[] childTransforms = block.GetComponentsInChildren<Transform>();

        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] == block.transform)
                continue;
            Vector3 position = spawnPosition.transform.position - childTransforms[i].position;
            position.x += (arraySize.x / 2f);

            playGrid[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y)] = childTransforms[i].gameObject;

        }
    }

    private int CheckFullRows()
    {
        int rowsDeleted = 0;
        for (int i = playGrid.GetLength(1) - 1; i >= 0; i--)
        {
            int sum = 0;
            for (int j = 0; j < playGrid.GetLength(0); j++)
            {
                sum += playGrid[j, i] == null ? 0 : 1;
            }
            if (sum == playGrid.GetLength(0))
            {
                rowsDeleted++;
                DeleteRow(i);
                i++;
            }
        }
        return rowsDeleted;
    }

    private void DeleteRow(int removeIdx)
    {
        for (int j = 0; j < playGrid.GetLength(0); j++)
        {
            Destroy(playGrid[j, removeIdx]);
            playGrid[j, removeIdx] = null;
            for (int l = removeIdx - 1; l >= 0; l--)
            {
                if (playGrid[j, l] != null)
                {
                    playGrid[j, l].transform.position += Vector3.down;
                    playGrid[j, l + 1] = playGrid[j, l];
                    playGrid[j, l] = null;
                }
            }
        }
    }

    private void SendScoreToDatabase(string name, int score, int rows)
    {
        StartCoroutine(Upload(name, score, rows));
    }

    IEnumerator Upload(string name, int score, int rows)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("name", name));
        formData.Add(new MultipartFormDataSection("score", score.ToString()));
        formData.Add(new MultipartFormDataSection("rows", rows.ToString()));

        UnityWebRequest www = UnityWebRequest.Post("http://deserd.wz.cz/unity/tetris/scoreboard/add.php", formData);
        yield return www.Send();

        if (www.isError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log(www.responseCode);
            Debug.Log("Form upload complete!");
        }
    }

    public void OnOkNameClicked(Text nameText)
    {
        if (nameText.text != "" && nameText.text.Length <= 8 && nameText.text.Length > 0)
        {
            PersistentData.name = nameText.text;
            SendScoreToDatabase(nameText.text, score, rows);
            CloseNamePanel();
        }
    }




    public void OnCancelNameClicked()
    {
        CloseNamePanel();
    }

}
