using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerContext
{
    public PlayerInputContext InputContext;
    public PlayerController PlayerController;
    public Transform Player;
    public Animator Animator;
    public Rigidbody2D Rb;
    public float MoveSpeed;
}