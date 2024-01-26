using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerGame.Game
{
    public static class GameUtils
    {
        public static bool IsMe(PhotonView photonView)
        {
            return
                (PhotonNetwork.IsConnected && photonView.IsMine) ||
                (!PhotonNetwork.IsConnected);
        }

        public static GameObject Instantiate(string name, Vector3 position, Quaternion rotation)
        {
            GameObject g;

            if (PhotonNetwork.IsConnected)
            {
                g = PhotonNetwork.Instantiate(name, position, rotation, 0);
            }
            else
            {
                g = GameObject.Instantiate(Resources.Load(name), position, rotation) as GameObject;
            }

            return g;
        }

        public static void Destroy(PhotonView view)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(view);
            }
            else
            {
                GameObject.Destroy(view.gameObject);
            }
        }

        public static void ShuffleList<T>(ref List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

    }


}
