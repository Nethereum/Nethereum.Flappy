using System.Collections;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.KeyStore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameControl : MonoBehaviour {
    public static GameControl instance; //A reference to our game control script so we can access it statically.
    public Text scoreText; //A reference to the UI text component that displays the player's score.
    public GameObject gameOvertext; //A reference to the object that displays the text which appears when the player dies.
    public Text continueGameOverText;

    private int score = 0; //The player's score.

    public bool gameOver = false; //Is the game over?
    public float scrollSpeed = -1.5f;

    public int TopScore { get; set; }
    public int TopScoreRecorded { get; set; }
    public bool SubmitTopScore { get; set; }

    public string UserAccount { get; set; }

    public string Key { get; set; }

    void Start () {
        TopScore = 0;
        TopScoreRecorded = -1;
        SubmitTopScore = false;
    }

    void Awake () {
        //If we don't currently have a game control...
        if (instance == null) {
            //...set this one to be it...
            instance = this;
        } //...otherwise...
        else if (instance != this)
            //...destroy this one because it is a duplicate.
            Destroy (gameObject);
    }

    void Update () {
        if (gameOver && SubmitTopScore) {
            continueGameOverText.text = "Submitting Top Score";
        }

        if (gameOver && !SubmitTopScore) {
            continueGameOverText.text = "Tap or click to continue";
        }
        //If the game is over and the player has pressed some input...
        if (gameOver && Input.GetMouseButtonDown (0) && !SubmitTopScore) {
            //...reload the current scene.
            SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex);
        }
    }

    public void UniScored () {
        //The uni can't score if the game is over.
        if (gameOver)
            return;
        //If the game is not over, increase the score...
        score++;

        //...and adjust the score text.
        scoreText.text = "Score: " + score.ToString ();
    }

    public void UniDied () {
        //Activate the game over text.
        gameOvertext.SetActive (true);
        //Set the game to be over.

        if (score > TopScoreRecorded && TopScoreRecorded > -1) {
            TopScore = score;
            SubmitTopScore = true;
        }

        gameOver = true;
    }
}