using System.Collections;
using UnityEngine;


public class Uni : MonoBehaviour {
    public float upForce; //Upward force of the "flap".
    private bool isDead = false; //Has the player collided with a wall?

    private Animator anim; //Reference to the Animator component.
    private Rigidbody2D rb2d; //Holds a reference to the Rigidbody2D component of the uni.

    void Start () {
        //Get reference to the Animator component attached to this GameObject.
        anim = GetComponent<Animator> ();
        //Get and store a reference to the Rigidbody2D attached to this GameObject.
        rb2d = GetComponent<Rigidbody2D> ();
    }

    void Update () {
        //Don't allow control if the uni has died.
        if (isDead == false) {
            //Look for input to trigger a "flap".
            if (Input.GetMouseButtonDown (0)) {
                //...tell the animator about it and then...
                anim.SetTrigger ("FlapUni");
                //...zero out the unis current y velocity before...
                rb2d.velocity = Vector2.zero;
                //	new Vector2(rb2d.velocity.x, 0);
                //..giving the uni some upward force.
                rb2d.AddForce (new Vector2 (0, upForce));
            }
        }
    }

    void OnCollisionEnter2D (Collision2D other) {
        // Zero out the uni's velocity
        rb2d.velocity = Vector2.zero;
        // If the uni collides with something set it to dead...
        isDead = true;
        //...tell the Animator about it...
        anim.SetTrigger ("DeadUni");
        //...and tell the game control about it.
        GameControl.instance.UniDied ();
    }
}