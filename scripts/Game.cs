using Godot;
using System;
using System.Collections.Generic;

public class Game : Node2D
{
    private TileMap map;
    private ColorRect ready;
    private ColorRect gOver;
    private Label baseText;
    private Label scoreText;
    private Label multiText;
    private Label infoText;
    private AudioStreamPlayer heart;
    private AudioStreamPlayer dead;
    private AudioStreamPlayer segBoom;
    private AudioStreamPlayer music;
    private AudioStreamPlayer gameover;

    private int heartID = 0;
    private int segmentID = 1;
    private int maxSegments = 3;
    private Vector2 heartPosition = Vector2.Zero;
    private Vector2 headPosition = new Vector2(11, 16);
    private List<Vector2> segments = new List<Vector2>();
    private Vector2 direction = Vector2.Zero;
    private Vector2 lastDirection = Vector2.Zero;
    private Vector2 reverse = Vector2.Zero;
    private int speed = 180;
    private int maxSpeed = 180;
    private int deadTicker = 0;
    private int score = 0;
    private int multiplier = 3;
    private int multiplierTicker = 180;
    private int infoTicker = 0;

    public enum States {NULL, INIT, MOVE, DYING, GAMEOVER}
    private States currentState = States.NULL;
    private States previousState = States.NULL;

    public override void _Ready() {
        map = (TileMap)GetNode("Graphic/Objects");
        ready = (ColorRect)GetNode("Graphic/Ready");
        gOver = (ColorRect)GetNode("Graphic/GameOver");
        baseText = (Label)GetNode("Graphic/BaseText");
        scoreText = (Label)GetNode("Graphic/BaseText/Score");
        multiText = (Label)GetNode("Graphic/BaseText/Multiplier");
        infoText = (Label)GetNode("Graphic/BaseText/Info");

        SetState(States.INIT);
    }

    public override void _Input(InputEvent @event) {
        if(currentState != States.MOVE) {
            return;
        }

        if(Input.IsActionJustPressed("ui_up") && reverse != Vector2.Up) {
            direction = Vector2.Up;
            reverse = Vector2.Down;
        }
        if(Input.IsActionJustPressed("ui_down") && reverse != Vector2.Down) {
            direction = Vector2.Down;
            reverse = Vector2.Up;
        }
        if(Input.IsActionJustPressed("ui_left") && reverse != Vector2.Left) {
            direction = Vector2.Left;
            reverse = Vector2.Right;
        }
        if(Input.IsActionJustPressed("ui_right") && reverse != Vector2.Right) {
            direction = Vector2.Right;
            reverse = Vector2.Left;
        }
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
            case States.INIT:
                speed--;

                if(currentState != States.INIT) {
                    multiplierTicker--;

                    if(multiplierTicker == 0 && multiplier > 1) {
                        multiplier--;
                        multiText.Text = "X" + multiplier;
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
                        //segBoom.Play();
                        SpawnBoom(headPosition);
                        headPosition = Vector2.One * 99;
                    }

                    if(deadTicker % 8 == 0 && segments.Count > 0) {
                        heartPosition = Vector2.One * 99;
                        //segBoom.Play();
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
            infoText.Text ="";
        }
    }

    public States GetTransition() {
        switch(currentState) {
            case States.INIT:
                if(speed == 0) {
                    return States.MOVE;
                }
                break;
            
            case States.MOVE:
                if(direction != lastDirection || speed == 0) {
                    return States.MOVE;
                }
                break;
            
            case States.DYING:
                if(segments.Count == 0 && headPosition == Vector2.One * 99) {
                    return States.GAMEOVER;
                }
                break;
        }

        return States.NULL;
    }

    private void EnterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                //music.Play();
                // Set the heart's position at random.
                SetHeartPosition();

                // Set the snake's initial position and add the minimum segments.
                segments.Add(headPosition + (Vector2.Down * 3));
                segments.Add(headPosition + (Vector2.Down * 2));
                segments.Add(headPosition + (Vector2.Down));
                UpdateSegments();
                break;
            
            case States.MOVE:
                // Check to see if the head of the snake is within the bounds of the map.
                Vector2 desired = headPosition + direction;
                Rect2 boundary = new Rect2(new Vector2(2, 4), new Vector2(20, 20));
                lastDirection = direction;
                bool boundCheck = !boundary.HasPoint(desired);
                bool overlapCheck = segments.Contains(desired);

                if(boundCheck || overlapCheck) {
                    SetState(States.DYING);
                    return;
                }

                // Check to see if the heart was picked up.
                if(heartPosition == desired) {
                    //heart.Play();
                    SetHeartPosition();

                    maxSegments++;

                    maxSpeed--;
                    maxSpeed = Mathf.Clamp(maxSpeed, 5, 15);

                    score += 10 * multiplier;
                    scoreText.Text = Convert.ToString(score);
                    SpawnPoints(multiplier);

                    multiplier = 3;
                    multiplierTicker = 180;
                    multiText.Text = "X3";

                    infoText.Text = "CHOMP!!!";
                    infoTicker = 120;
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
                //music.Stop();
                //dead.Play();
                maxSpeed = 30;
                infoText.Text = "OH NO!";
                infoTicker = 240;
                break;
            
            case States.GAMEOVER:
                //gameover.Play();
                gOver.Show();
                break;
        }
    }

    private void ExitState(States oldState, States newState) {
        switch(oldState) {
            case States.INIT:
                ready.Hide();
                direction = Vector2.Up;
                reverse = Vector2.Down;
                maxSpeed = 20;
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
        Random RNGesus = new Random();
        float x = RNGesus.Next(2, 21);
        float y = RNGesus.Next(4, 23);
        heartPosition = new Vector2(x, y);

        // Makes sure the heart isn't placed on a segment.
        while(heartPosition == headPosition || segments.Contains(heartPosition)) {
            bool a = heartPosition == headPosition;
            bool b = segments.Contains(heartPosition);

            x = RNGesus.Next(2, 21);
            y = RNGesus.Next(4, 23);
            heartPosition = new Vector2(x, y);
        }

        SpawnBoom(heartPosition);
    }

    private void UpdateSegments() {
        // Clear the map.
        map.Clear();

        // Place the heart.
        map.SetCellv(heartPosition, 0);

        // Place the head and orient it's direction.
        int headID = 1;
        switch(direction) {
            case Vector2 v when direction == Vector2.Up:
                headID = 1;
                break;
            
            case Vector2 v when direction == Vector2.Down:
                headID = 2;
                break;
            
            case Vector2 v when direction == Vector2.Left:
                headID = 3;
                break;
            
            case Vector2 v when direction == Vector2.Right:
                headID = 4;
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
                map.SetCellv(segments[i], 5);
            }

            // Tail
            if(i == 0) {
                if(segments[i] + Vector2.Up == butt) {
                    map.SetCellv(segments[i], 6);
                }

                if(segments[i] + Vector2.Down == butt) {
                    map.SetCellv(segments[i], 7);
                }

                if(segments[i] + Vector2.Left == butt) {
                    map.SetCellv(segments[i], 8);
                }

                if(segments[i] + Vector2.Right == butt) {
                    map.SetCellv(segments[i], 9);
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
}
