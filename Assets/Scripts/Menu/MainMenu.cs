using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerGame.Game
{

    public class MainMenu : MonoBehaviourPunCallbacks
    {
        [SerializeField]
        private UIDocument document;
        [SerializeField]
        private Launcher launcher;

        // Panels

        private VisualElement playerSelectionElement;
        private VisualElement serverBrowserElement;
        private VisualElement serverCreatorElement;

        private HashSet<RoomInfo> cachedRoomList = new HashSet<RoomInfo>();
        private List<RoomInfo> orderedRoomList = new List<RoomInfo>();

        public void Start()
        {
            var rootElement = document.rootVisualElement;

            playerSelectionElement = rootElement.Q<VisualElement>("PlayerSelection");
            serverBrowserElement = rootElement.Q<VisualElement>("ServerBrowser");
            serverCreatorElement = rootElement.Q<VisualElement>("ServerCreator");


            // Setup event calls
            playerSelectionElement.Q<Button>("Play").clickable.clicked += OnPlayerSelect;


            // Set the correct panels to visible
            playerSelectionElement.style.display = DisplayStyle.Flex;
            serverBrowserElement.style.display = DisplayStyle.None;
            serverCreatorElement.style.display = DisplayStyle.None;
        }

        private void OnPlayerSelect()
        {
            // Name
            TextField name = playerSelectionElement.Q<TextField>("Name");
            string chosenName = name.text;

            // Colour
            Color chosenColour = Color.white;

            // Input type
            DropdownField inputType = playerSelectionElement.Q<DropdownField>("InputType");

            Vehicle.InputTypeEnum chosenInputType = Vehicle.InputTypeEnum.Keyboard;

            if (inputType.value == "Controller")
            {
                chosenInputType = Vehicle.InputTypeEnum.Controller;
            }
            else if (inputType.value == "Touch")
            {
                chosenInputType = Vehicle.InputTypeEnum.Touch;
            }

            launcher.SetPlayerPropertiesThenConnect(chosenName, chosenColour, chosenInputType);
        }

        private void OnDestroy()
        {
            /*        
             *        m_Button.clickable.clicked -= OnButtonClicked;
                    m_Toggle.UnregisterValueChangedCallback(OnToggleValueChanged);*/
        }


        public override void OnJoinedLobby()
        {
            // Joined the games loby
            // That means we can now search for servers

            playerSelectionElement.style.display = DisplayStyle.None;
            serverBrowserElement.style.display = DisplayStyle.Flex;
            serverCreatorElement.style.display = DisplayStyle.Flex;

            Button createRoom = serverCreatorElement.Q<Button>("CreateRoom");

            createRoom.SetEnabled(true);

            void OnCreateRoomClicked()
            {
                launcher.TryCreateRoom();
            }

            createRoom.clickable.clicked += OnCreateRoomClicked;


            ListView browser = serverBrowserElement.Q<ListView>("ServerBrowser");

            lock (orderedRoomList)
            {
                cachedRoomList.Clear();
                orderedRoomList.Clear();
            }

            // The "makeItem" function will be called as needed
            // when the ListView needs more items to render
            browser.makeItem = () =>
            {
                return new ServerElement();
            };

            // As the user scrolls through the list, the ListView object
            // will recycle elements created by the "makeItem"
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list)
            browser.bindItem = (element, index) =>
            {
                RoomInfo info;

                // Lock the list
                lock (orderedRoomList)
                {
                    // Update the display values of the server
                    info = orderedRoomList[index];
                }

                string mapName = (string)info.CustomProperties[GameManager.KeyMapName];

                ServerElement server = element as ServerElement;
                server.UpdateServer(info.Name, mapName, info.PlayerCount, info.MaxPlayers);

            };

            ForceServerBrowserUpdate();

            Button joinRoom = serverBrowserElement.Q<Button>("JoinRoom");

            joinRoom.SetEnabled(browser.selectedIndex >= 0 && browser.selectedIndex < orderedRoomList.Count);

            browser.onSelectionChange += (value) =>
            {
                joinRoom.SetEnabled(browser.selectedIndex >= 0 && browser.selectedIndex < orderedRoomList.Count);
            };




            void OnJoinRoomClicked()
            {
                if (browser.selectedIndex >= 0 && browser.selectedIndex < orderedRoomList.Count)
                {
                    TryJoinRoom(orderedRoomList[browser.selectedIndex].Name);
                }
                else
                {
                    GameConsole.Debug("ERROR: trying to join room when selection is null");
                    joinRoom.SetEnabled(false);
                }
            }

            joinRoom.clickable.clicked += OnJoinRoomClicked;
        }


        private void ForceServerBrowserUpdate()
        {
            ListView browser = serverBrowserElement.Q<ListView>("ServerBrowser");

            lock (orderedRoomList)
            {
                browser.itemsSource = orderedRoomList;
            }
            browser.RefreshItems();
        }

        public override void OnJoinedRoom()
        {
            // We've now joined a room and left the main lobby

        }

        public override void OnLeftLobby()
        {
            ForceServerBrowserUpdate();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            ForceServerBrowserUpdate();
        }

        public override void OnRoomListUpdate(List<RoomInfo> updatedRooms)
        {
            GameConsole.Debug($"Called OnRoomListUpdate() with {updatedRooms.Count} rooms");

            lock (orderedRoomList)
            {
                orderedRoomList.Clear();

                foreach (RoomInfo room in updatedRooms)
                {
                    if (room.RemovedFromList)
                    {
                        cachedRoomList.Remove(room);
                    }
                    else
                    {
                        cachedRoomList.Add(room);
                    }
                }

                // Duplicate the list in memory, but order it by player count
                orderedRoomList.AddRange(cachedRoomList);
                orderedRoomList.Sort((x, y) => x.PlayerCount.CompareTo(y.PlayerCount));
            }

            ForceServerBrowserUpdate();
        }


        public void TryJoinRoom(string roomName)
        {
            try
            {
                PhotonNetwork.JoinRoom(roomName);
            }
            catch (Exception e)
            {
                GameConsole.Debug($"Failed to join room with error: {e.Message}");
            }
        }
    }




}