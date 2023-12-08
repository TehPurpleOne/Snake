using Godot;
using System;

public class Title : Node2D {
    private Master m;
    private Label topA;
    private Label topB;

    private enum States {NULL, INIT, RUN}
    private States currentState = States.NULL;

    public override void _Ready() {
        // We'll grab Master with each scene to pull data or play audio.
        m = (Master)GetNode("/root/Master");
        topA = (Label)GetNode("TypeA");
        topB = (Label)GetNode("TypeB");

        SetState(States.INIT);
    }

    public override void _PhysicsProcess(float delta) {
        if(currentState != States.NULL) {
            // StateLogic isn't required here.
            if(m.accept || m.pause) {
                m.nextScene = true;
            }
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
            case States.INIT:
                // Reset values.
                m.gameType = 0;
                m.musicType = 0;
                m.speed = 0;
                m.level = 1;
                m.score = 0;
                m.gameover = false;

                // Update high scores.
                int a = Convert.ToString(m.topScoreA).Length;
                int b = Convert.ToString(m.topScoreB).Length;

                for(int i = 6; i > 0; i--) {
                    if(i > a) {
                        topA.Text += "0";
                    }
                }

                for(int i = 6; i > 0; i--) {
                    if(i > b) {
                        topB.Text += "0";
                    }
                }

                topA.Text += m.topScoreA;
                topB.Text += m.topScoreB;
                
                break;

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
