using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerGame.Game
{
    public class RoomDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomName;
        [SerializeField] private TMP_Text numberOfPlayers;
        public Button SetSelectedButton;

        public void UpdateRoomDisplay(RoomInfo info)
        {
            roomName.text = $"{info.Name}";
            name = info.Name;
            numberOfPlayers.text = $"{info.PlayerCount}/{info.MaxPlayers}";
        }
    }
}