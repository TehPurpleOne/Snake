using Godot;
using System;

public class Master : Node2D {
    private ViewportContainer vpc;
    private Viewport vp;
    private ColorRect crt;
    private Control gameRoot;
    private AnimationPlayer anim;
    private ShaderMaterial colorLimiter;
    private ShaderMaterial crtShader;
    public AudioStreamPlayer currentSong;

    public enum States {NULL, INIT, FADEIN, FADEOUT, LOADNEXT, RUN};
    public States currentState = States.NULL;
    private States previousState = States.NULL;

    private Vector2 gameRes = new Vector2(256, 240);
    public int scale = 1;

    public int gameType = 0;
    public int musicType = 0;
    public int speed = 0;
    public bool gameover;
    public bool nextScene = false;

    public override void _Ready() {
        gameRoot = (Control)GetNode("ViewportContainer/Viewport/GameRoot");
        anim = (AnimationPlayer)GetNode("AnimationPlayer");
        vpc = (ViewportContainer)GetNode("ViewportContainer");
        vp = (Viewport)vpc.GetNode("Viewport");
        colorLimiter = (ShaderMaterial)vpc.Material;
        crt = (ColorRect)GetNode("CRT");
        crtShader = (ShaderMaterial)crt.Material;

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
        
    }

    private States GetTransition() {
        if(nextScene) {
            return States.FADEOUT;
        }

        return States.NULL;
    }

    private void EnterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                ResizeWindow();
                SetState(States.FADEIN);
                break;
            
            case States.FADEIN:
                anim.Play("FADE");
                break;
            
            case States.FADEOUT:
                nextScene = false;
                anim.PlayBackwards("FADE");
                break;
            
            case States.LOADNEXT:
                // Load the next scene needed based on what's already loaded.
                string path = "";

                switch(gameRoot.GetChild(0).GetType()) {
                    case Type t when t == typeof(Title):
                        path = "res://scenes/Options.tscn";
                        break;
                    
                    case Type t when t == typeof(Options):
                        path = "res://scenes/Game.tscn";
                        break;
                    
                    case Type t when t == typeof(Game):
                        switch(gameover) {
                            case true:
                                path = "res://scenes/Title.tscn";
                                break;
                            
                            case false:
                                path = "res://scenes/Game.tscn";
                                break;
                        }
                        break;
                }

                // Unload the old scene.
                gameRoot.GetChild(0).QueueFree();

                // Load the new.
                PackedScene loader = (PackedScene)ResourceLoader.Load(path);
                Node2D newScene = (Node2D)loader.Instance();
                gameRoot.AddChild(newScene);

                // Fade in the new scene.
                SetState(States.FADEIN);
                break;
        }
    }

    private void ExitState(States oldState, States newState) {

    }

    private void SetState(States newState) {
        previousState = currentState;
        currentState = newState;

        ExitState(previousState, currentState);
        EnterState(currentState, previousState);
    }

    public void PlaySFX(int child) {
        // Play a sound effect.
        Node path = (Node)GetNode("Audio/SFX");
        AudioStreamPlayer sfx = (AudioStreamPlayer)path.GetChild(child);
        sfx.Play();
    }

    public void PlayMusic(int child) {
        // Play a song. NOTE: This will overwrite the currently playing song.
        StopMusic();

        Node path = (Node)GetNode("Audio/Music");
        currentSong = (AudioStreamPlayer)path.GetChild(child);
        currentSong.Play();
    }

    public void StopMusic() {
        // Stops and clears the currently playing song.
        if(currentSong != null) {
            currentSong.Stop();
            currentSong = null;
        }
    }

    private void ResizeWindow() {
        // Get the maximum scale the the current screen.
        Vector2 screenRes = OS.GetScreenSize();

        while((scale * gameRes.y) < screenRes.y) { // Increment the scale value.
            scale++;
        }

        while((scale * gameRes.y) >= screenRes.y) { // Decrement scale value if larger than current screen resolution.
            scale--;
        }

        // First, adjust the size of the viewport container.
        vpc.RectSize = gameRes * scale;
        // Ensure the viewport retains the 1x scale
        vp.Size = gameRes;
        // Make the same adjustment to the CRT filter, but also adjust the scanline values to match the new screen size.
        crt.RectSize = gameRes * scale;
        crtShader.SetShaderParam("screen_width", gameRes.x * scale);
        crtShader.SetShaderParam("screen_height", gameRes.y * scale);
        crtShader.SetShaderParam("lines_distance", scale);

        // Resize the actual game window and center it.
        OS.WindowSize = gameRes * scale;
        OS.CenterWindow();
    }

    private void OnFadeDone(string which) {
        switch(currentState) {
            case States.FADEIN:
                SetState(States.RUN);
                break;
            
            case States.FADEOUT:
                SetState(States.LOADNEXT);
                break;
        }
    }
}
