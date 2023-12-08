using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Game : Node2D {
    private Master m;
    private TileMap map;
    private Label baseText;
    private Label levelText;
    private Label scoreText;
    private Label bugText;
    private Label multiText;
    private Label speedText;
    private Label infoText;
    private AudioStreamPlayer heart;
    private AudioStreamPlayer dead;
    private AudioStreamPlayer segBoom;
    private AudioStreamPlayer music;
    private AudioStreamPlayer gameover;

    private int heartID = 0;
    private int segmentID = 1;
    private int maxSegments = 3;
    private List<Vector2> heartPositions = new List<Vector2>();
    private int maxHearts = 0;
    private int targetHearts = 0;
    private Vector2 headPosition = new Vector2(11, 16);
    private List<Vector2> segments = new List<Vector2>();
    private Vector2 direction = Vector2.Zero;
    private Vector2 lastDirection = Vector2.Zero;
    private Vector2 reverse = Vector2.Zero;
    private int speed = 180;
    private int maxSpeed = 180;
    private int deadTicker = 0;
    private int score = 0;
    private int bugs = 0;
    private int multiplier = 3;
    private int multiplierTicker = 180;
    private int infoTicker = 0;

    public enum States {NULL, INIT, READY, MOVE, DYING, GAMEOVER, CLEAR}
    public States currentState = States.NULL;
    private States previousState = States.NULL;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
        map = (TileMap)GetNode("Graphic/Objects");
        baseText = (Label)GetNode("Graphic/BaseText");
        levelText = (Label)GetNode("Graphic/BaseText/Level");
        scoreText = (Label)GetNode("Graphic/BaseText/Score");
        multiText = (Label)GetNode("Graphic/BaseText/Multiplier");
        bugText = (Label)GetNode("Graphic/BaseText/Bugs");
        speedText = (Label)GetNode("Graphic/BaseText/Speed");
        infoText = (Label)GetNode("Graphic/BaseText/Info");

        SetState(States.INIT);
    }

    public override void _PhysicsProcess(float delta) {
        if(currentState != States.NULL) {
            StateLogic(delta);
            States t = GetTransition();
            if(t != States.NULL) {
                SetState(t);
            }
        }
    }

    private void StateLogic(float delta) {
        switch(currentState) {
            case States.MOVE:
            case States.READY:
                if(m.direction != direction && m.direction != Vector2.Zero) {
                    direction = m.direction;
                }

                if(m.reverse != reverse && m.reverse != Vector2.Zero) {
                    reverse = m.reverse;
                }

                speed--;

                if(currentState != States.READY) {
                    multiplierTicker--;

                    if(multiplierTicker == 0 && multiplier > 1) {
                        multiplier--;
                        UpdateText();
                    }

                    multiplierTicker = Mathf.Wrap(multiplierTicker, 0, 181);
                }
                break;

            case States.DYING:
                if(speed > 0) {
                    speed--;
                }

                if(speed == 0) {
                    deadTicker++;

                    if(deadTicker % 8 == 0 && segments.Count == 0 && headPosition != Vector2.One * 99) {
                        m.PlaySFX(2);
                        SpawnBoom(headPosition);
                        headPosition = Vector2.One * 99;
                        deadTicker = 0;
                    }

                    if(deadTicker % 8 == 0 && segments.Count > 0) {
                        if(heartPositions.Count > 0) {
                            heartPositions.Clear();
                        }

                        m.PlaySFX(2);
                        SpawnBoom(segments[0]);
                        segments.RemoveAt(0);
                    }

                    UpdateSegments();
                }
                break;
        }

        if(infoTicker > 0) {
            infoTicker--;
        }

        if(infoTicker == 0 && infoText.Text != "") {
            if(m.gameover || currentState == States.CLEAR) {
                m.nextScene = true;
            }

            infoText.Text ="";
        }
    }

    public States GetTransition() {
        switch(currentState) {
            case States.INIT:
                if(m.currentState == Master.States.RUN) {
                    return States.READY;
                }
                break;
            
            case States.READY:
                if(speed == 0) {
                    return States.MOVE;
                }
                break;
            
            case States.MOVE:
                if(direction != lastDirection || speed == 0) {
                    return States.MOVE;
                }

                if(m.gameType == 1 && bugs == 0) {
                    return States.CLEAR;
                }
                break;
            
            case States.DYING:
                if(segments.Count == 0 && headPosition == Vector2.One * 99 && deadTicker == 30) {
                    return States.GAMEOVER;
                }
                break;
        }

        return States.NULL;
    }

    private void EnterState(States newState, States oldState) {
        switch(newState) {
            case States.READY:
                // Play the music needed.
                switch(m.musicType) {
                    case 0:
                        m.PlayMusic(1);
                        break;
                    
                    case 1:
                        m.PlayMusic(2);
                        break;
                    
                    case 2:
                        m.PlayMusic(3);
                        break;
                }

                // Set the snake's initial position and add the minimum segments.
                int startSegments = 2 + m.level;
                startSegments = Mathf.Clamp(startSegments, 3, 10);
                for(int i = startSegments; i > 0; i--) {
                    segments.Add(headPosition + (Vector2.Down * i));
                }

                // Set the maximum hearts for the game.
                switch(m.gameType) {
                    case 0: // Type-A
                        maxHearts = 1;
                        targetHearts = 5 * m.level; // Used to increase the game level.
                        targetHearts = Mathf.Clamp(targetHearts, 1, 350);
                        break;
                    
                    case 1: // Type-B
                        maxHearts = 5 + (5 * m.level);
                        maxHearts = Mathf.Clamp(maxHearts, 1, 350); // There's only 400 spaces available, so clamping at 350 seems reasonable for higher levels.
                        bugs = maxHearts;
                        break;
                }

                UpdateText();

                // Set the heart's position at random.
                SetHeartPosition();
                UpdateSegments();

                // Display "READY!"
                UpdateInfoBox(0, 180);
                break;
            
            case States.MOVE:
                // Check to see if the head of the snake is within the bounds of the map.
                Vector2 desired = headPosition + direction;
                Rect2 boundary = new Rect2(new Vector2(2, 7), new Vector2(20, 20));
                lastDirection = direction;
                bool boundCheck = !boundary.HasPoint(desired);
                bool overlapCheck = segments.Contains(desired);

                if(boundCheck || overlapCheck) {
                    SetState(States.DYING);
                    return;
                }

                // Check to see if the heart was picked up.
                for(int i = 0; i < heartPositions.Count; i++) {
                    if(heartPositions[i] == desired) {
                        // Play the eaten sound
                        m.PlaySFX(0);

                        // Add a segment.
                        maxSegments++;

                        // Add code for speed ups. 
                        switch(m.gameType) {
                            case 0:
                                if(bugs == targetHearts) {
                                    m.IncrementLevel();
                                    targetHearts = 5 * m.level; // So the amount of bugs always increases with each new level.
                                    targetHearts = Mathf.Clamp(targetHearts, 1, 350);
                                    if(m.level % 2 == 0) {
                                        maxSpeed--;
                                        maxSpeed = Mathf.Clamp(maxSpeed, 5 - m.speed, 30 - m.speed);
                                    }
                                }
                                break;

                            case 1:
                                if(bugs % 5 == 0) {
                                    maxSpeed--;
                                    maxSpeed = Mathf.Clamp(maxSpeed, 7 - m.speed, 30);
                                }
                                break;
                        }
                        
                        // Add to the game score.
                        m.score += 10 * multiplier;
                        m.score = Mathf.Clamp(m.score, 0, 999990);
                        SpawnPoints(multiplier);

                        // Set the high score value when necessary.
                        switch(m.gameType) {
                            case 0:
                                if(m.score > m.topScoreA) {
                                    m.topScoreA = m.score;
                                }
                                break;
                            
                            case 1:
                                if(m.score > m.topScoreB) {
                                    m.topScoreB = m.score;
                                }
                                break;
                        }

                        // Add or subtract bugs based on game mode.
                        switch(m.gameType) {
                            case 0:
                                bugs++;
                                break;
                            
                            case 1:
                                bugs--;
                                break;
                        }

                        // Reset the score multiplier.
                        multiplier = 3;
                        multiplierTicker = 180;

                        // Update text boxes as needed.
                        UpdateText();
                        UpdateInfoBox(1, 120);

                        // Remove the entry that was eaten.
                        heartPositions.RemoveAt(i);

                        // Spawn a new heart in it's place if in Type-A
                        if(m.gameType == 0) {
                            SetHeartPosition();
                        }
                    }
                }

                // Update the snake's position.
                segments.Add(headPosition);
                if(segments.Count > maxSegments) {
                    segments.RemoveAt(0);
                }
                headPosition = desired;

                // Update the tilemap.
                UpdateSegments();
                break;
            
            case States.DYING:
                m.StopMusic();
                m.PlaySFX(1);
                maxSpeed = 30;
                UpdateInfoBox(2, 240);
                break;
            
            case States.GAMEOVER:
                m.PlayMusic(6);
                m.gameover = true;
                UpdateInfoBox(3, 400);
                break;
            
            case States.CLEAR:
                m.PlayMusic(4);
                UpdateInfoBox(4, 400);
                break;
        }
    }

    private void ExitState(States oldState, States newState) {
        switch(oldState) {
            case States.READY:
                direction = Vector2.Up;
                reverse = -direction;
                m.reverse = -direction;
                
                maxSpeed = 30 - (m.speed * 10);
                break;
        }
    }

    private void SetState(States newState) {
        previousState = currentState;
        currentState = newState;

        ExitState(previousState, currentState);
        EnterState(currentState, previousState);

        speed = maxSpeed;
    }

    private void SetHeartPosition() {
        heartPositions.Clear();
        for(int i = 0; i < maxHearts; i++) {
            GD.Randomize();
            Random RNGesus = new Random();
            // Set the initial position of the bug. Yes, I know it's named heart. It's now a bug.
            float x = RNGesus.Next(2, 21);
            float y = RNGesus.Next(7, 26);
            Vector2 bugPos = new Vector2(x, y);

            //Make sure the bug isn't overlapping an existing snake tile.
            while(bugPos == headPosition || bugPos == headPosition + direction || segments.Contains(bugPos) || heartPositions.Contains(bugPos)) {
                x = RNGesus.Next(2, 21);
                y = RNGesus.Next(7, 26);
                bugPos = new Vector2(x, y);
            }

            // Add the bug's position to the list.
            heartPositions.Add(bugPos);
            SpawnBoom(bugPos);
        }
    }

    private void UpdateSegments() {
        // Clear the map.
        map.Clear();

        // Place the heart.
        for(int i = 0; i < heartPositions.Count; i++) {
            map.SetCellv(heartPositions[i], 19);
        }

        // Place the head and orient it's direction.
        int headID = 20;
        switch(direction) {
            case Vector2 v when direction == Vector2.Up:
                headID = 20;
                break;
            
            case Vector2 v when direction == Vector2.Down:
                headID = 21;
                break;
            
            case Vector2 v when direction == Vector2.Left:
                headID = 22;
                break;
            
            case Vector2 v when direction == Vector2.Right:
                headID = 23;
                break;
        }
        map.SetCellv(headPosition, headID);

        //Set the body segments and tail.
        Vector2 butt = Vector2.Zero;
        switch(segments.Count) {
            case int v when segments.Count == 1: // No other segments are in the list aside from the tail.
                butt = headPosition;
                break;
            
            case int v when segments.Count > 1: // Snake is longer than 1 segment.
                butt = segments[1];
                break;
        }

        for(int i = 0; i < segments.Count; i++) {

            // Body segments.
            if(i != 0) {
                map.SetCellv(segments[i], 24);
            }

            // Tail
            if(i == 0) {
                if(segments[i] + Vector2.Up == butt) {
                    map.SetCellv(segments[i], 25);
                }

                if(segments[i] + Vector2.Down == butt) {
                    map.SetCellv(segments[i], 26);
                }

                if(segments[i] + Vector2.Left == butt) {
                    map.SetCellv(segments[i], 27);
                }

                if(segments[i] + Vector2.Right == butt) {
                    map.SetCellv(segments[i], 28);
                }
            }
        }
    }

    private void SpawnBoom(Vector2 pos) {
        string path = "res://scenes/DeadBoom.tscn";
        PackedScene loader = (PackedScene)ResourceLoader.Load(path);
        DeadBoom db = (DeadBoom)loader.Instance();
        AddChild(db);
        db.GlobalPosition = map.MapToWorld(pos) + map.CellSize / 2;
    }

    private void SpawnPoints(int multiplier) {
        string path = "res://scenes/Score.tscn";
        PackedScene loader = (PackedScene)ResourceLoader.Load(path);
        Score points = (Score)loader.Instance();
        AddChild(points);
        switch(multiplier) {
            case 1:
                points.baseFrame = 0;
                break;
            
            case 2:
                points.baseFrame = 4;
                break;
            
            case 3:
                points.baseFrame = 8;
                break;
        }
        points.GlobalPosition = map.MapToWorld(headPosition) + map.CellSize / 2;
    }

    private void UpdateText() {
        // Initialize the text boxes. Speed isn't required as it stays static throughout the game.
        baseText.Text = "";
        levelText.Text = "";
        scoreText.Text = "";
        multiText.Text = "";
        bugText.Text = "";

        // Update the base text.
        switch(m.gameType) {
            case 0:
                baseText.Text = "TYPE-A";
                break;
            
            case 1:
                baseText.Text = "TYPE-B";
                break;
        }

        baseText.Text += "\n\n\nLVL-\n\n\nSCORE\n\n\n\nMULTI\n\n\n\nBUGS\n\n\n\nSPEED";
        
        // Update the values.
        if(m.level < 10) {
            levelText.Text += "0";
        }
        levelText.Text += Convert.ToString(m.level);

        int scoreLen = Convert.ToString(m.score).Length;
        for(int i = 6; i > 0; i--) {
            if(i > scoreLen) {
                scoreText.Text += "0";
            }
        }
        scoreText.Text += Convert.ToString(m.score);

        multiText.Text = "X" + Convert.ToString(multiplier);

        bugText.Text = Convert.ToString(bugs);

        switch(m.speed) {
            case 0:
                speedText.Text = "LOW";
                break;
            
            case 1:
                speedText.Text = "MED";
                break;
            
            case 2:
                speedText.Text = "HIGH";
                break;
        }
    }

    private void UpdateInfoBox(int which, int frames) {
        string[] options = new string[] {"READY!", "CHOMP!!!", "OUCH!!", "GAME  OVER", "COURSE  CLEAR!", "PAUSE!"};

        infoText.Text = options[which];
        infoTicker = frames;
    }
}
