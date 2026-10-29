using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class SimpleLoginManager : MonoBehaviour
{
    // List of usernames and corresponding passwords (set in the Inspector)
    [SerializeField] private string[] usernames;
    [SerializeField] private string[] passwords;

    // References to the TextMeshPro input fields (assign in the Inspector)
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    // UnityEvent to trigger on successful login.
    [SerializeField] private UnityEvent onLoginSuccess;

    // This method retrieves the input from the TextMeshPro fields and checks the login.
    public void CheckLogin()
    {
        string inputUsername = usernameInput.text;
        string inputPassword = passwordInput.text;

        // Ensure that both arrays have the same length.
        if (usernames.Length != passwords.Length)
        {
            Debug.LogError("Usernames and passwords arrays must be the same length!");
            return;
        }

        // Loop through all usernames.
        for (int i = 0; i < usernames.Length; i++)
        {
            if (usernames[i].Equals(inputUsername))
            {
                // Found a matching username; now check if the password matches.
                if (passwords[i].Equals(inputPassword))
                {
                    Debug.Log("Login successful.");
                    // Invoke the UnityEvent if the login is successful.
                    onLoginSuccess?.Invoke();
                    return;
                }
                else
                {
                    Debug.Log("Incorrect password.");
                    return;
                }
            }
        }

        // Username not found.
        Debug.Log("Username not found.");
    }
}
