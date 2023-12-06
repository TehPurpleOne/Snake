using Godot;
using System;

public class Title : Node2D {
    private Master m;

    private enum States {NULL, INIT, RUN}
    private States currentState = States.NULL;

    public override void _Ready() {
        // We'll grab Master with each scene to pull data or play audio.
        m = (Master)GetNode("/root/Master");

        SetState(States.INIT);
    }

    public override void _Input(InputEvent @event) {
        if(m.currentState != Master.States.RUN) {
            return;
        }

        if(Input.IsActionJustPressed("ui_accept")) {
            m.nextScene = true;
        }
    }

    public override void _PhysicsProcess(float delta) {
        if(currentState != States.NULL) {
            // StateLogic isn't required here.
            States t = GetTransition();
            if(t != States.NULL) {
                SetState(t);
            }
        }
    }

    private States GetTransition() {
        switch(currentState) {
            case States.INIT:
                if(m.currentState == Master.States.RUN) {
                    return States.RUN;
                }
                break;
        }

        return States.NULL;
    }

    private void EnterState(States newState) {
        switch(newState) {
            case States.RUN:
                m.PlayMusic(0);
                break;
        }
    }

    private void SetState(States newState) {
        currentState = newState;

        EnterState(currentState);
    }
}
