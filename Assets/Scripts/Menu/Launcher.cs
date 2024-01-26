using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerGame.Game
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        const string gameVersion = "0.1.1";

        private TypedLobby lobby = new TypedLobby("games", LobbyType.Default);

        #region MonoBehaviour CallBacks

        void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        void Start()
        {
            SceneManager.LoadScene("Console", LoadSceneMode.Additive);

            // Rejoining after a game
            if (PhotonNetwork.IsConnected)
            {
                // We will re connect to master/lobby automatically
            }
        }

        private void Update()
        {
            if (Input.GetButtonDown("Cancel"))
            {
                Application.Quit();
            }
        }

        private void OnApplicationQuit()
        {
            PhotonNetwork.Disconnect();
        }


        #endregion


        #region Public Methods

        public void SetPlayerPropertiesThenConnect(string name, Color colour, Vehicle.InputTypeEnum inputType)
        {
            // Set name
            PhotonNetwork.LocalPlayer.NickName = name;

            ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable();

            // Colour
            playerProperties[Vehicle.KeyColourRed] = colour.r;
            playerProperties[Vehicle.KeyColourGreen] = colour.g;
            playerProperties[Vehicle.KeyColourBlue] = colour.b;

            playerProperties[Vehicle.KeyInputType] = inputType;


            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);


            // Connect to master
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();

                //PhotonNetwork.SerializationRate = 30;
                //PhotonNetwork.SendRate = 60;
            }
        }

        #endregion


        #region MonoBehaviourPunCallbacks Callbacks

        public override void OnConnectedToMaster()
        {
            GameConsole.Debug($"Connected to master. There are {PhotonNetwork.CountOfRooms} rooms, {PhotonNetwork.CountOfPlayersOnMaster} players on master, and {PhotonNetwork.CountOfPlayersInRooms} players in rooms.");
            GameConsole.Debug($"Using serialisation rate of {PhotonNetwork.SerializationRate} and send rate of {PhotonNetwork.SendRate}");

            PhotonNetwork.JoinLobby(lobby);
        }

        public override void OnJoinedLobby()
        {
            GameConsole.Debug($"Joined lobby \"{PhotonNetwork.CurrentLobby.Name}\"");
        }

        public override void OnLeftLobby()
        {
            GameConsole.Debug($"Left loby");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            GameConsole.Debug($"Disconnected with reason {cause}");
        }

        public void TryCreateRoom()
        {
            try
            {
                // Setup the room
                ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();

                roomProperties[GameManager.KeyMapName] = "MAP NAME";

                int[] spawnPosX = new int[] { 0, 1, 2, 3 };
                roomProperties[GameManager.KeySpawnXPositions] = spawnPosX;
                roomProperties[GameManager.KeySpawnYPositions] = new int[] { 0, 1, 2, 3 };
                roomProperties[GameManager.KeyAlivePlayers] = new Player[] { };


                int maxPlayers = spawnPosX.Length;

                RoomOptions roomOptions = new RoomOptions()
                {
                    MaxPlayers = Convert.ToByte(maxPlayers),
                    IsVisible = true,
                    SuppressRoomEvents = false,
                    SuppressPlayerInfo = false,
                    IsOpen = true,
                    EmptyRoomTtl = 0,
                    PlayerTtl = 0,
                    CustomRoomProperties = roomProperties,
                    CustomRoomPropertiesForLobby = new string[] { GameManager.KeyMapName } // Only make the map name public
                };

                // Now try and create the room
                PhotonNetwork.CreateRoom(PhotonNetwork.LocalPlayer.NickName, roomOptions, lobby);
            }
            catch (Exception e)
            {
                GameConsole.Debug($"Failed to create room with error: {e.Message}");
            }
        }

        public override void OnJoinedRoom()
        {
            GameConsole.Debug($"Joined room \"{PhotonNetwork.CurrentRoom.Name}\" with \"{PhotonNetwork.CurrentRoom.PlayerCount}\" players");

            // #Critical: We only load if we are the first player, else we rely on `PhotonNetwork.AutomaticallySyncScene` to sync our instance scene.
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                // #Critical
                // Load the Room Level.
                PhotonNetwork.LoadLevel("Game");
            }
        }

        #endregion


    }
}

