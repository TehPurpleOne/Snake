using Godot;
using System;

public class Background : TileMap {
    private Master m;
    private ShaderMaterial tilePalette;

    private int lastLevel = 1;
    private int defaultID = 0;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
        tilePalette = (ShaderMaterial)Material;

        SetPaletteID();
    }

    public override void _PhysicsProcess(float delta) {
        if(m.level != lastLevel) {
            SetPaletteID();
        }
    }

    private void SetPaletteID() {
        // Adjust the palette based on the game's current level.
        if(m.level == 1) {
            tilePalette.SetShaderParam("palette_index", 0);
        }

        if(m.level % 5 == 0) {
            defaultID++;
            defaultID = Mathf.Wrap(defaultID, 0, 10);
            tilePalette.SetShaderParam("palette_index", defaultID);
        }
        
        lastLevel = m.level;
    }
}
