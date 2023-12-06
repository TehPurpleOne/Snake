using Godot;
using System;

public class DeadBoom : Node2D {
    private AnimationPlayer anim;
    public override void _Ready() {
        anim = (AnimationPlayer)GetNode("AnimationPlayer");

        anim.Play("boom");
    }

    private void onAnimDone(string which) {
        QueueFree();
    }
}
