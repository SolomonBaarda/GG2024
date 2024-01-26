using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MultiplayerGame.Game
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class Vehicle : MonoBehaviourPun
    {
        [Header("Movement")]
        [SerializeField] private float MovementSpeed = 10.0f;
        [SerializeField] private float TurnSpeed = 1.25f;
        [SerializeField] private bool AllowPlayerInput = true;

        [SerializeField] private float ReloadTimeSeconds = 2.0f;
        [SerializeField] private float AttackPower = 15.0f;

        private Rigidbody2D rigid;
        private Animator animator;

        [SerializeField] private Transform GunSpriteTransform;

        [SerializeField] private TMP_Text PlayerNameText;

        public Vector2 Position => rigid.position;

        public Vector2 Forward => transform.up;

        public ControlsClass Controls { get; protected set; } = new ControlsClass();


        private bool canAttack = true;



        public enum InputTypeEnum
        {
            Keyboard = 0,
            Controller = 1,
            Touch = 2
        }

        [SerializeField]
        private InputTypeEnum InputType;








        /// <summary>
        /// float
        /// </summary>
        public const string KeyColourRed = "r";
        /// <summary>
        /// float
        /// </summary>
        public const string KeyColourGreen = "g";
        /// <summary>
        /// float
        /// </summary>
        public const string KeyColourBlue = "b";
        /// <summary>
        /// InputTypeEnum
        /// </summary>
        public const string KeyInputType = "i";



        private void Start()
        {
            rigid = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            // Disable the cursor
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;

            if (PhotonNetwork.IsConnected)
            {
                // For any instance
                Photon.Realtime.Player owner = photonView.Owner;
                ExitGames.Client.Photon.Hashtable playerProperties = owner.CustomProperties;

                // Name
                PlayerNameText.text = owner.NickName;

                // Colour
                float r = (float)playerProperties[KeyColourRed];
                float g = (float)playerProperties[KeyColourGreen];
                float b = (float)playerProperties[KeyColourBlue];

                Color colour = new Color(r, g, b, 1.0f);
                PlayerNameText.color = colour;

                // Input type
                InputType = (InputTypeEnum)playerProperties[KeyInputType];
            }


        }

        private bool IsMe()
        {
            return
                (PhotonNetwork.IsConnected && photonView.IsMine) ||
                (!PhotonNetwork.IsConnected);
        }

        private void Update()
        {
            // Playing online
            if (IsMe())
            {
                // We are controlling this player

                UpdatePlayerInput();
            }
            // Someone else is controlling this player
            else
            {

            }

            // Update the animator
            int direction = transform.InverseTransformDirection(rigid.velocity).y < 0 ? -1 : 1;
            animator.SetFloat("direction", rigid.velocity.magnitude * direction);

            // Also update the username text display
            PlayerNameText.transform.position = transform.position + new Vector3(0, 1, 0);
            PlayerNameText.transform.rotation = Quaternion.identity;
        }

        private void FixedUpdate()
        {
            // Playing online
            if (IsMe())
            {
                // We are controlling this player
                Move();

                // If we are playing using a mouse then calculate the exact direction 
                Vector2 direction = Controls.desiredAimingDirection;

                GunSpriteTransform.up = direction;

                // Try attacking
                if (canAttack && Controls.wantToAttack)
                {
                    // Start timer
                    canAttack = false;
                    StartCoroutine(ReloadWeapon(ReloadTimeSeconds));

                    // Shoot the rocket
                    GameObject g = GameUtils.Instantiate("Rocket", Position, Quaternion.identity);
                    Rocket r = g.GetComponent<Rocket>();
                    r.Shoot(Position + direction * 1.5f, direction * AttackPower);
                }
            }
            // Someone else is controlling this player
            else
            {

            }
        }

        private IEnumerator ReloadWeapon(float reloadTime)
        {
            canAttack = false;

            yield return new WaitForSeconds(reloadTime);

            canAttack = true;
        }

        public void DestroyVehicle()
        {
            photonView.RPC("DestroyVehicleRPC", RpcTarget.All);
        }

        [PunRPC]
        private void DestroyVehicleRPC()
        {
            // Play animation


            if (GameUtils.IsMe(photonView))
            {
                // Raise event
                object[] content = new object[1];
                content[0] = photonView.Owner;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(GameManager.PlayerDiedEventID, content, raiseEventOptions, SendOptions.SendReliable);

                // Destroy object
                AllowPlayerInput = false;
                GameUtils.Destroy(photonView);
            }
        }


        /*
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // We are updating the values synced on the server
            if (stream.IsWriting)
            {
                stream.SendNext((Vector2)(Position));
                stream.SendNext((Vector2)(controls.desiredMovingDirection));
                stream.SendNext((Vector2)(controls.desiredAimingDirection));
            }
            // We are updating the values stored locally
            else
            {
                //transform.position = (Vector2)stream.ReceiveNext();
                //.desiredMovingDirection = (Vector2)stream.ReceiveNext();
                //.desiredAimingDirection = (Vector2)stream.ReceiveNext();

                // timeSinceLastSyncSeconds = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime)); // Time duration since the values sync request was made

                //GameConsole.Debug($"Time since last update {timeSinceLastSyncSeconds} (number of physics updates {(int)(timeSinceLastSyncSeconds / Time.fixedDeltaTime)})");
            }
        }
        */

        void Move()
        {
            rigid.AddRelativeForce(new Vector2(0, Controls.vertical * MovementSpeed), ForceMode2D.Force);

            // Invert the turning direction if the player is moving backwards
            rigid.angularVelocity += Controls.horizontal * TurnSpeed * -Mathf.Sign(Controls.vertical);
        }




        protected void UpdatePlayerInput()
        {
            if (AllowPlayerInput)
            {
                // Handle keyboard/controller/touch specific controls
                switch (InputType)
                {
                    // Player is using a keyboard and mouse
                    case InputTypeEnum.Keyboard:

                        // Use mouse aiming if the cursor is within the window
                        if (Input.mousePresent && Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 && Input.mousePosition.x < Screen.width && Input.mousePosition.y < Screen.height)
                        {
                            Vector3 aimingPositionWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            Controls.mouseAimingPosition = aimingPositionWorld;

                            Vector2 aimingDirection = aimingPositionWorld - transform.position;
                            Controls.desiredAimingDirection = new Vector2(aimingDirection.x, aimingDirection.y).normalized;
                        }
                        else
                        {
                            // Ensure that the aiming direction is always set to something
                            Controls.desiredAimingDirection = Forward;
                        }

                        Controls.wantToAttack = Input.GetButton("Fire1");

                        break;

                    // Player is using a controller
                    case InputTypeEnum.Controller:


                        Controls.desiredAimingDirection = new Vector2(Input.GetAxisRaw("Aim X"), Input.GetAxisRaw("Aim Y"));

                        if(Controls.desiredAimingDirection == Vector2.zero)
                        {
                            // Ensure that the aiming direction is always set to something
                            Controls.desiredAimingDirection = Forward;
                        }

                        Controls.wantToAttack = Input.GetAxisRaw("Fire1") != 0;

                        break;

                    // Player is using touch controls
                    case InputTypeEnum.Touch:
                        break;
                }


                Controls.desiredAimingDirection = Forward;

                // Update the direction that the player wishes to move in (wishdir)
                Controls.vertical = Input.GetAxisRaw("Vertical");
                Controls.horizontal = Input.GetAxisRaw("Horizontal");

                // Up and down
                Controls.desiredMovingDirection = new Vector2(Controls.horizontal, Controls.vertical);




                Controls.desiredMovingDirection.Normalize();
                Controls.desiredAimingDirection.Normalize();
            }
            else
            {
                Controls.Reset();
            }
        }

        public class ControlsClass
        {
            public float horizontal;
            public float vertical;
            public bool wantToAttack;

            public Vector2 desiredAimingDirection;
            public Vector2 desiredMovingDirection;

            public Vector2 mouseAimingPosition;

            public void Reset()
            {
                horizontal = 0;
                vertical = 0;
                wantToAttack = false;

                desiredAimingDirection = Vector2.zero;
                desiredMovingDirection = Vector2.zero;

                mouseAimingPosition = Vector2.zero;
            }
        }



        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && rigid != null)
            {
                // Position
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(Position, 0.25f);

                // Forward
                Gizmos.color = Color.green;
                Gizmos.DrawLine(Position, Position + Forward);

                // Wishdir
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(Position, Position + (Vector2)transform.TransformDirection(new Vector2(Controls.horizontal, Controls.vertical)));

                // Aiming direction
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Position, Position + new Vector2(Controls.desiredAimingDirection.x, Controls.desiredAimingDirection.y));
            }
        }

        private void OnDestroy()
        {

        }
    }


}
