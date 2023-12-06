using Godot;
using System;

public class Master : Node2D {
    private Control gameRoot;
    private ColorRect crt;
    private AnimationPlayer anim;

    public enum States {NULL, INIT, FADEINT, FADEOUT, LOADNEXT, RUN};
    public States currentState = States.NULL;
    private States previousState = States.NULL;

    private Vector2 gameRes = new Vector2(256, 240);
    public int scale = 3;
    [Export] public int _scale {
        get{return scale;}
        set{scale = value;
        ResizeWindow(scale);}
    }
    public bool crtShader = true;

    public int gameType = 0;
    public int musicType = 0;
    public int speed = 0;



}
