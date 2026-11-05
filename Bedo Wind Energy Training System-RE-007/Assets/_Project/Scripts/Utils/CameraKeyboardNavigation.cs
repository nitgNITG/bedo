using UnityEngine;

public class CameraKeyboardNavigation : MonoBehaviour
{
    [SerializeField] private SmoothCameraMovement cameraMovement;

    [SerializeField] private int currentIndex = 0;

    [SerializeField] private int cameraCount;


    private void Update()
    {
        // S = next camera
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex++;

            if (currentIndex >= cameraCount)
                currentIndex = cameraCount - 1;


            cameraMovement.MoveToPosition(currentIndex);
        }



        // W = previous camera
        if (Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = 0;


            cameraMovement.MoveToPosition(currentIndex);
        }



        // Number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GoToCamera(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GoToCamera(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GoToCamera(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            GoToCamera(3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            GoToCamera(4);
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            GoToCamera(5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            GoToCamera(6);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            GoToCamera(7);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            GoToCamera(8);
        }
    }



    private void GoToCamera(int index)
    {
        if (index < 0 || index >= cameraCount)
            return;


        currentIndex = index;

        cameraMovement.MoveToPosition(currentIndex);
    }
}