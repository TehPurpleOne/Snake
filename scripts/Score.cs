using Godot;
using System;

public class Score : Node2D
{
    private Sprite s;

    public int baseFrame = -1;
    private int frameOffset = 0;
    private float y = 2;
    private float gravity = 0.1f;

    public override void _Ready() {
        s = (Sprite)GetNode("Sprite");
    }

    public override void _PhysicsProcess(float delta) {
        if(baseFrame > -1) {
            if(!s.Visible) {
                s.Show();
            }

            frameOffset++;
            frameOffset = Mathf.Wrap(frameOffset, 0, 4);
            s.Frame = baseFrame + frameOffset;

            GlobalPosition += Vector2.Up * y;
            y -= gravity;
        }

        if(y <= 0) {
            QueueFree();
        }
    }
}
