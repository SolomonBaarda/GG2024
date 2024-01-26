using Photon.Pun;
using TMPro;
using UnityEngine;

namespace MultiplayerGame.Game
{
    public class GameConsole : MonoBehaviourPunCallbacks
    {
        private static string consoleText = "";

        public static GameConsole Instance { get; private set; }

        [SerializeField] private GameObject consoleRoot;
        [SerializeField] private TMP_Text text;


        private void Awake()
        {
            Instance = this;
            Instance.text.text = consoleText;
            consoleRoot.SetActive(false);
        }

        public static void Debug(string message)
        {
            consoleText += "\n" + message;

            if (Instance != null)
            {
                Instance.text.text = consoleText;
            }

            UnityEngine.Debug.Log(message);
        }

        private void Update()
        {
            if (Input.GetButtonDown("Console"))
            {
                consoleRoot.SetActive(!consoleRoot.activeSelf);
            }
        }

    }
}
