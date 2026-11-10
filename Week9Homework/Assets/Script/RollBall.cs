//using System.Reflection.Metadata.Ecma335;
//using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.SceneManagement;

//public class RollBall : MonoBehaviour
//{
//    Rigidbody rigidbody;

//    public float RollPower;
//    public LayerMask personLayer;


//    public int maxRolls = 3;
//    private int rollCount = 0;
//    private bool isRolling = false;
//    private bool readyToRoll = false;

//    public Vector3 resetPosition =
//        new Vector3(0.23f, 0.9f, -12.68f);


//    public LineRenderer predictionLine;


//    public float predictionLength = 20f;


//    void Start()
//    {
//        rigidbody = GetComponent<Rigidbody>();


//        readyToRoll = false;


//        if (predictionLine != null)
//        {
//            predictionLine.enabled = false;
//        }
//    }


//    void Update()
//    {

//        if (Mouse.current.leftButton.wasPressedThisFrame)
//        {

//            if (isRolling)
//            {
//                return;
//            }



//            if (!readyToRoll)
//            {
//                ResetBall();

//                readyToRoll = true;

//                return;
//            }



//            if (readyToRoll)
//            {
//                StartRoll();
//            }
//        }

//        CheckBallStopped();


//        UpdatePredictionLine();
//    }



//    void ResetBall()
//    {

//        rigidbody.linearVelocity = Vector3.zero;
//        rigidbody.angularVelocity = Vector3.zero;

//        transform.position = resetPosition;

//        rigidbody.isKinematic = false;

//        Debug.Log("Ball Reset");
//    }



//    void StartRoll()
//    {
//        Vector3 direction = FindDirection();

//        rigidbody.linearVelocity = Vector3.zero;
//        rigidbody.angularVelocity = Vector3.zero;

//        rigidbody.AddForce(
//            direction * RollPower,
//            ForceMode.Impulse
//        );

//        rollCount++;
//        isRolling = true;
//        readyToRoll = false;

//        if (predictionLine != null)
//            predictionLine.enabled = false;
//    }


//    void CheckBallStopped()
//    {
//        if (!isRolling)
//        {
//            return;
//        }


//        bool stopped =
//            rigidbody.linearVelocity.magnitude < 0.05f &&
//            rigidbody.angularVelocity.magnitude < 0.05f;


//        if (stopped)
//        {
//            isRolling = false;



//            if (rollCount >= maxRolls)
//            {
//                GameOver();
//                return;
//            }

//            readyToRoll = false;

//            Debug.Log(
//                "Ball stopped. Press Left Mouse to reset."
//            );
//        }
//    }



//    Vector3 FindDirection()
//    {
//        Vector2 mousePosition =
//            Mouse.current.position.ReadValue();

//        Ray mouseRay =
//            Camera.main.ScreenPointToRay(
//                mousePosition
//            );


//        Plane ballPlane =
//            new Plane(
//                Vector3.up,
//                transform.position
//            );


//        if (ballPlane.Raycast(
//            mouseRay,
//            out float distance))
//        {
//            Vector3 targetPoint =
//                mouseRay.GetPoint(distance);



//            Vector3 direction =
//                new Vector3(
//                    targetPoint.x -
//                    transform.position.x,

//                    0f,

//                    1f
//                );


//            return direction.normalized;
//        }


//        return Vector3.forward;
//    }

//    void UpdatePredictionLine()
//    {
//        if (predictionLine == null)
//        {
//            return;
//        }


//        if (!readyToRoll || isRolling)
//        {
//            predictionLine.enabled = false;
//            return;
//        }


//        if (rollCount >= maxRolls)
//        {
//            predictionLine.enabled = false;
//            return;
//        }

//        predictionLine.enabled = true;


//        Vector3 direction =
//            FindDirection();

//        predictionLine.positionCount = 2;


//        predictionLine.SetPosition(
//            0,
//            transform.position
//        );


//        predictionLine.SetPosition(
//            1,
//            transform.position +
//            direction * predictionLength
//        );
//    }



//    void GameOver()
//    {
//        Debug.Log("Game Over");

//        if (predictionLine != null)
//        {
//            predictionLine.enabled = false;
//        }

//        SceneManager.LoadScene("GameOver");
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class RollBall : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Ball")]
    public float rollPower = 15f;

    public Vector3 resetPosition =
        new Vector3(0.23f, 0.9f, -12.68f);

    [Header("Prediction")]
    public LineRenderer predictionLine;

    public float predictionLength = 20f;

    [Header("Pins")]
    public Transform pinParent;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text resetTimeText;

    [Header("Game Rules")]
    public int maxRolls = 3;

    public float resetTime = 5f;

    public float perfectDelay = 1.5f;

    // Current roll number
    private int rollCount = 0;

    // Highest score from the three rolls
    private int highScore = 0;

    private bool isAiming = true;
    private bool isRolling = false;
    private bool isResetting = false;

    // Save every pin's original position
    private Dictionary<Transform, Vector3> pinPositions =
        new Dictionary<Transform, Vector3>();

    // Save every pin's original rotation
    private Dictionary<Transform, Quaternion> pinRotations =
        new Dictionary<Transform, Quaternion>();


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        SavePinPositions();

        ResetBall();

        UpdateScoreText();

        if (resetTimeText != null)
        {
            resetTimeText.text = "";
        }

        if (predictionLine != null)
        {
            predictionLine.enabled = true;
        }
    }


    void Update()
    {
        // Player is aiming
        if (isAiming &&
            !isRolling &&
            !isResetting)
        {
            UpdatePredictionLine();

            // RELEASE LEFT MOUSE BUTTON
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                StartRoll();
            }
        }
    }


    // =====================================================
    // START ROLL
    // =====================================================

    void StartRoll()
    {
        Vector3 direction = FindDirection();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = false;

        rb.AddForce(
            direction * rollPower,
            ForceMode.Impulse
        );

        rollCount++;

        isAiming = false;
        isRolling = true;

        if (predictionLine != null)
        {
            predictionLine.enabled = false;
        }

        // Start the 5 second reset timer
        StartCoroutine(RollTimer());
    }


    // =====================================================
    // ROLL TIMER
    // =====================================================

    IEnumerator RollTimer()
    {
        float timeLeft = resetTime;

        while (timeLeft > 0)
        {
            // Show countdown in upper-left UI
            if (resetTimeText != null)
            {
                resetTimeText.text =
                    "Reset time: " +
                    Mathf.CeilToInt(timeLeft) +
                    "s";
            }

            // Check if ALL pins have been knocked down
            if (AllPinsKnockedDown())
            {
                // Give the player some time to see
                // the pins fall.
                yield return new WaitForSeconds(
                    perfectDelay
                );

                CalculateScore();

                // All pins are down -> immediately end
                if (AllPinsKnockedDown())
                {
                    GameOver();
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.1f);

            timeLeft -= 0.1f;
        }

        // Time is over
        if (resetTimeText != null)
        {
            resetTimeText.text = "";
        }

        CalculateScore();

        // If all pins are down,
        // the game is already over.
        if (AllPinsKnockedDown())
        {
            GameOver();
            yield break;
        }

        // Otherwise reset for next roll
        ResetEverything();
    }


    // =====================================================
    // FIND DIRECTION
    // =====================================================

    Vector3 FindDirection()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray mouseRay =
            Camera.main.ScreenPointToRay(
                mousePosition
            );

        // Horizontal plane at the ball's Y position
        Plane ballPlane =
            new Plane(
                Vector3.up,
                transform.position
            );

        if (ballPlane.Raycast(
            mouseRay,
            out float distance))
        {
            Vector3 targetPoint =
                mouseRay.GetPoint(distance);

            // X = mouse controls left/right
            // Y = always 0
            // Z = always forward
            Vector3 direction =
                new Vector3(
                    targetPoint.x -
                    transform.position.x,

                    0f,

                    1f
                );

            return direction.normalized;
        }

        return Vector3.forward;
    }


    // =====================================================
    // PREDICTION LINE
    // =====================================================

    void UpdatePredictionLine()
    {
        if (predictionLine == null)
            return;

        Vector3 direction =
            FindDirection();

        predictionLine.positionCount = 2;

        predictionLine.SetPosition(
            0,
            transform.position
        );

        predictionLine.SetPosition(
            1,
            transform.position +
            direction *
            predictionLength
        );
    }


    // =====================================================
    // CALCULATE SCORE
    // =====================================================

    void CalculateScore()
    {
        int knockedDown = 0;

        foreach (Transform pin in pinParent)
        {
            float rotationAmount =
                Quaternion.Angle(
                    pinRotations[pin],
                    pin.rotation
                );

            float distanceMoved =
                Vector3.Distance(
                    pinPositions[pin],
                    pin.position
                );

            // Pin has fallen
            if (rotationAmount > 10f ||
                distanceMoved > 0.1f)
            {
                knockedDown++;
            }
        }

        // Only keep the highest score
        if (knockedDown > highScore)
        {
            highScore = knockedDown;
        }

        UpdateScoreText();

        Debug.Log(
            "Roll " +
            rollCount +
            ": " +
            knockedDown +
            " pins knocked down. " +
            "High Score: " +
            highScore
        );
    }


    // =====================================================
    // CHECK ALL PINS
    // =====================================================

    bool AllPinsKnockedDown()
    {
        int knockedDown = 0;

        foreach (Transform pin in pinParent)
        {
            float rotationAmount =
                Quaternion.Angle(
                    pinRotations[pin],
                    pin.rotation
                );

            float distanceMoved =
                Vector3.Distance(
                    pinPositions[pin],
                    pin.position
                );

            if (rotationAmount > 10f ||
                distanceMoved > 0.1f)
            {
                knockedDown++;
            }
        }

        return knockedDown ==
               pinPositions.Count;
    }


    // =====================================================
    // RESET EVERYTHING
    // =====================================================

    void ResetEverything()
    {
        ResetBall();

        ResetPins();

        isRolling = false;
        isResetting = false;
        isAiming = true;

        if (resetTimeText != null)
        {
            resetTimeText.text = "";
        }

        if (predictionLine != null)
        {
            predictionLine.enabled = true;
        }

        // If three rolls are finished
        if (rollCount >= maxRolls)
        {
            GameOver();
        }
    }


    // =====================================================
    // RESET BALL
    // =====================================================

    void ResetBall()
    {
        rb.isKinematic = true;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        transform.position =
            resetPosition;

        transform.rotation =
            Quaternion.identity;

        rb.isKinematic = false;
    }


    // =====================================================
    // SAVE PIN POSITIONS
    // =====================================================

    void SavePinPositions()
    {
        foreach (Transform pin in pinParent)
        {
            pinPositions.Add(
                pin,
                pin.position
            );

            pinRotations.Add(
                pin,
                pin.rotation
            );
        }
    }


    // =====================================================
    // RESET PINS
    // =====================================================

    void ResetPins()
    {
        foreach (Transform pin in pinParent)
        {
            Rigidbody pinRb =
                pin.GetComponent<Rigidbody>();

            if (pinRb != null)
            {
                pinRb.isKinematic = true;

                pinRb.linearVelocity =
                    Vector3.zero;

                pinRb.angularVelocity =
                    Vector3.zero;
            }

            pin.position =
                pinPositions[pin];

            pin.rotation =
                pinRotations[pin];

            if (pinRb != null)
            {
                pinRb.isKinematic = false;
            }
        }
    }


    // =====================================================
    // SCORE UI
    // =====================================================

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text =
                "Score: " +
                highScore;
        }
    }


    // =====================================================
    // GAME OVER
    // =====================================================

    void GameOver()
    {
        PlayerPrefs.SetInt(
            "BowlingScore",
            highScore
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(
            "GameOver 1"
        );
    }
}