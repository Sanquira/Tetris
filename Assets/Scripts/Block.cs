using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{

    public int states = 4;

    private int rotation = 0;
    private GameController gameController;
    private float fallTimer;

    void Start()
    {
        GameObject gameControllerObject = GameObject.FindGameObjectWithTag("GameController");
        if (gameControllerObject != null)
        {
            gameController = gameControllerObject.GetComponent<GameController>();
        }
        if (gameController == null)
        {
            Debug.Log("Cannot find 'GameController' script");
        }
    }

    public void Rotate()
    {
        if (states == 1)
            return;

        rotation++;

        if (states == 2)
        {
            int dir = ((rotation % 2) * 2) - 1;
            RotateChildrens(-dir);
            RotateParent(dir);
            return;
        }
        if (states == 4)
        {
            RotateChildrens(-1);
            RotateParent(1);
            return;
        }

        Debug.Log("Wrong number of states.");
    }

    private void RotateBack()
    {
        if (states == 1)
            return;

        rotation--;

        if (states == 2)
        {
            int dir = ((rotation % 2) * 2) - 1;
            RotateChildrens(-dir);
            RotateParent(dir);
            return;
        }
        if (states == 4)
        {
            RotateChildrens(1);
            RotateParent(-1);
            return;
        }

        Debug.Log("Wrong number of states.");
    }

    private void RotateChildrens(int direction)
    {
        Transform[] childTransform = GetComponentsInChildren<Transform>();
        for (int i = 0; i < childTransform.Length; i++)
        {
            if (childTransform[i] == transform)
                continue;
            childTransform[i].Rotate(0, 0, direction * -90, Space.Self);
        }
    }

    private void RotateParent(int direction)
    {
        transform.Rotate(0, 0, direction * -90, Space.Self);
    }

    void Update()
    {
        if (gameController.IsPaused || !this.enabled)
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            transform.position += Vector3.right;
            if (!gameController.IsInsideGrid(gameObject) || gameController.IsOverlapringAnotherBlock(gameObject))
            {
                transform.position -= Vector3.right;
            }
        }

        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            transform.position += Vector3.left;
            if (!gameController.IsInsideGrid(gameObject) || gameController.IsOverlapringAnotherBlock(gameObject))
            {
                transform.position -= Vector3.left;
            }
        }

        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Rotate();
            if (!gameController.IsInsideGrid(gameObject) || gameController.IsOverlapringAnotherBlock(gameObject))
            {
                RotateBack();
            }
        }

        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            transform.position += Vector3.down;
            if (!gameController.IsInsideGrid(gameObject) || gameController.IsOverlapringAnotherBlock(gameObject))
            {
                transform.position -= Vector3.down;
            }
        }

        fallTimer += Time.deltaTime;
        if (fallTimer >= gameController.FallSpeed)
        {
            transform.position += Vector3.down;
            if (!gameController.IsInsideGrid(gameObject) || gameController.IsOverlapringAnotherBlock(gameObject))
            {
                this.enabled = false;
                transform.position -= Vector3.down;
                gameController.OnBlockFalled(gameObject);
            }
            fallTimer = 0;
        }
    }
}
