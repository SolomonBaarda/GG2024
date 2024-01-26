using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerGame.Game
{

    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Rocket : MonoBehaviourPun
    {
        private bool IsLive = false;
        private Rigidbody2D rigid;

        [SerializeField] private float MaxLifetimeSeconds = 5;

        [SerializeField] private LayerMask ExplodeOnContactLayers;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            transform.up = rigid.velocity.normalized;
        }

        public void Shoot(Vector2 position, Vector2 velocty)
        {
            photonView.RPC("ShootRPC", RpcTarget.All, position, velocty);

            StartCoroutine(ExplodeAfterSeconds(MaxLifetimeSeconds));
        }

        [PunRPC]
        private void ShootRPC(Vector2 position, Vector2 velocty)
        {
            // Shoot in direction
            rigid.position = position;
            rigid.velocity = velocty;
            // Face that direction
            transform.up = velocty.normalized;

            IsLive = true;
        }

        [PunRPC]
        private void ExplodeRPC()
        {

        }

        private IEnumerator ExplodeAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            photonView.RPC("ExplodeRPC", RpcTarget.All);
            GameUtils.Destroy(photonView);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (GameUtils.IsMe(photonView) && IsLive && ExplodeOnContactLayers == (ExplodeOnContactLayers | (1 << collision.gameObject.layer)))
            {
                if (collision.gameObject.TryGetComponent(out Vehicle vehicle))
                {
                    vehicle.DestroyVehicle();
                }

                photonView.RPC("ExplodeRPC", RpcTarget.All);
                GameUtils.Destroy(photonView);
            }
        }


    }
}