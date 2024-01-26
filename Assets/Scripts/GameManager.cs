using Cinemachine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerGame.Game
{
    public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        [SerializeField] private TMP_Text pingDisplay;

        [SerializeField] private CinemachineVirtualCamera followCamera;


        [SerializeField]
        private GameObject ClientsLoadingObject;
        [SerializeField]
        private GameObject MasterClientLoading;
        [SerializeField]
        private GameObject NonMasterClientLoading;

        [SerializeField] private TMP_Text ClientLoadingNumPlayers;

        /// <summary>
        /// string
        /// </summary>
        public const string KeyMapName = "n";

        /// <summary>
        /// int[]
        /// </summary>
        public const string KeySpawnXPositions = "sx";
        /// <summary>
        /// int[]
        /// </summary>
        public const string KeySpawnYPositions = "sy";


        /// <summary>
        /// Player[]
        /// </summary>
        public const string KeyAlivePlayers = "p";

        public const byte StartGameEventID = 1;
        public const byte EndGameEventID = 2;

        public const byte PlayerDiedEventID = 10;


        private void Awake()
        {
            // Register the serialise method for Vector3Int
            /*
            byte code = 1;
            if (!PhotonPeer.RegisterType(typeof(Vector3Int), code, SerializeVector3Int, DeserializeVector3Int))
            {
                Debug.LogError("Failed to register custom type Vector3Int");
            }
            */

            PhotonNetwork.AddCallbackTarget(this);

            StartCoroutine(LoadMap());
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private IEnumerator LoadMap()
        {
            ClientsLoadingObject.SetActive(true);

            AsyncOperation console = SceneManager.LoadSceneAsync("Console", LoadSceneMode.Additive);

            while (!console.isDone)
            {
                yield return null;
            }

            StartPreGame();
        }

        private void StartPreGame()
        {
            UpdateNumCurrentPlayersDisplay();

            if (PhotonNetwork.IsConnected)
            {
                ClientsLoadingObject.SetActive(true);

                // We are the master client
                if (PhotonNetwork.IsMasterClient)
                {
                    MasterClientLoading.SetActive(true);
                    NonMasterClientLoading.SetActive(false);
                }
                // We are not the master client
                else
                {
                    MasterClientLoading.SetActive(false);
                    NonMasterClientLoading.SetActive(true);
                }
            }
            else
            {
                GameObject g = GameUtils.Instantiate("Player", new Vector3(0f, 5f, 0f), Quaternion.identity);
                followCamera.Follow = g.transform;

                ClientsLoadingObject.SetActive(false);
            }
        }

        private void UpdateNumCurrentPlayersDisplay()
        {
            ClientLoadingNumPlayers.text = $"Current players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
        }

        public void StartGameIfMasterClient()
        {
            // We are the master client
            if (PhotonNetwork.IsMasterClient)
            {
                StartGameEvent();
            }
        }

        private void StartGameEvent()
        {
            ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;

            // Set the list of alive players
            List<Player> alivePlayers = new List<Player>();
            foreach (var p in PhotonNetwork.CurrentRoom.Players)
            {
                alivePlayers.Add(p.Value);
            }
            roomProperties[KeyAlivePlayers] = alivePlayers.ToArray();
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);


            List<(int, int)> shuffledSpawnPositions = new List<(int, int)>();

            int[] spawnXPositions = (int[])roomProperties[KeySpawnXPositions];
            int[] spawnYPositions = (int[])roomProperties[KeySpawnYPositions];

            for (int i = 0; i < spawnXPositions.Length; i++)
            {
                shuffledSpawnPositions.Add((spawnXPositions[i], spawnYPositions[i]));
            }

            GameUtils.ShuffleList(ref shuffledSpawnPositions);

            List<Player> players = new List<Player>(PhotonNetwork.CurrentRoom.Players.Values);

            // Add spawn pos to the event content
            object[] content = new object[(players.Count * 3)];
            for (int i = 0; i < players.Count; i++)
            {
                int index = i * 3;

                content[index] = players[i];
                content[index + 1] = shuffledSpawnPositions[i].Item1;
                content[index + 2] = shuffledSpawnPositions[i].Item2;
            }

            // You would have to set the Receivers to All in order to receive this event on the local client as well
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(StartGameEventID, content, raiseEventOptions, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            if (eventCode == StartGameEventID)
            {
                ClientsLoadingObject.SetActive(false);

                object[] data = (object[])photonEvent.CustomData;

                Vector3Int ourSpawnPosition = Vector3Int.zero;

                for (int i = 0; i < data.Length / 3; i++)
                {
                    int index = i * 3;
                    Player p = (Player)data[index];

                    if (PhotonNetwork.LocalPlayer == p)
                    {
                        // This is our spawn point
                        int x = (int)data[index + 1];
                        int y = (int)data[index + 2];

                        ourSpawnPosition = new Vector3Int(x, y, 0);
                        break;
                    }
                }

                int mapCentreX = (int)data[data.Length - 2];
                int mapCentreY = (int)data[data.Length - 1];

                StartGame(ourSpawnPosition, new Vector3Int(mapCentreX, mapCentreY, 0));
            }
            else if (eventCode == EndGameEventID)
            {
                StartPreGame();

                // Clean up all of our objects
                PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
            }
            else if (eventCode == PlayerDiedEventID)
            {
                object[] data = (object[])photonEvent.CustomData;
                Player died = (Player)data[0];

                GameConsole.Debug($"Player {died.NickName} was killed");

                PlayerDied(died);
            }
        }

        private void PlayerDied(Player died)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                List<Player> alivePlayers = new List<Player>((Player[])roomProperties[KeyAlivePlayers]);
                alivePlayers.Remove(died);
                roomProperties[KeyAlivePlayers] = alivePlayers.ToArray();
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                // End of the game
                if (alivePlayers.Count <= 1)
                {
                    object[] content = new object[0];
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(EndGameEventID, content, raiseEventOptions, SendOptions.SendReliable);
                }
            }
        }

        private void StartGame(Vector3Int spawnTile, Vector3Int mapCentreTile)
        {
            try
            {
                ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                string mapName = (string)roomProperties[KeyMapName];

                // Instantiate our player
                Vector3 spawnWorldPos = spawnTile;
                Vector3 facing = Vector3.up;
                float angleZ = Vector2.SignedAngle(Vector2.up, facing);

                GameObject g = GameUtils.Instantiate("Player", spawnWorldPos, Quaternion.Euler(0, 0, angleZ));
                followCamera.Follow = g.transform;
            }
            catch (Exception e)
            {
                GameConsole.Debug($"Failed to load map with error: {e.Message}");
                PhotonNetwork.LeaveRoom(false);
            }
        }

        #region Photon Callbacks

        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            GameConsole.Debug($"Left the room");

            PhotonNetwork.LoadLevel("Menu");
        }

        public override void OnJoinedRoom()
        {
            GameConsole.Debug($"Joined room \"{PhotonNetwork.CurrentRoom.Name}\"");
        }

        #endregion

        #region Public Methods



        private void Update()
        {
            if (Input.GetButtonDown("Cancel"))
            {
                PhotonNetwork.LeaveRoom(false);
            }

            pingDisplay.text = $"{PhotonNetwork.GetPing()} ms";
        }

        #endregion

        #region Private Methods

        private void OnApplicationQuit()
        {
            PhotonNetwork.Disconnect();
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerEnteredRoom(Player other)
        {
            UpdateNumCurrentPlayersDisplay();

            // Called when a new player joins the room
            // Not called for the joining player

            GameConsole.Debug($"Player \"{other.NickName}\" entered the room");
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            UpdateNumCurrentPlayersDisplay();

            GameConsole.Debug($"Player \"{other.NickName}\" left the room");

            PlayerDied(other);
        }

        #endregion


        #region Custom type serialisation

        public static readonly byte[] memVector3Int = new byte[3 * 4];

        private static short SerializeVector3Int(StreamBuffer outStream, object customObject)
        {
            Vector3Int vo = (Vector3Int)customObject;

            lock (memVector3Int)
            {
                byte[] bytes = memVector3Int;
                int index = 0;
                Protocol.Serialize(vo.x, bytes, ref index);
                Protocol.Serialize(vo.y, bytes, ref index);
                Protocol.Serialize(vo.z, bytes, ref index);
                outStream.Write(bytes, 0, bytes.Length);
            }

            return (short)memVector3Int.Length;
        }

        private static object DeserializeVector3Int(StreamBuffer inStream, short length)
        {
            int x, y, z;

            lock (memVector3Int)
            {
                inStream.Read(memVector3Int, 0, memVector3Int.Length);
                int index = 0;
                Protocol.Deserialize(out x, memVector3Int, ref index);
                Protocol.Deserialize(out y, memVector3Int, ref index);
                Protocol.Deserialize(out z, memVector3Int, ref index);
            }

            return new Vector3Int(x, y, z);
        }


        #endregion


    }
}