using Godot;
using System;
using System.Linq;

public class Options : Node2D {
    private Master m;
    private Sprite gCursor;
    private Sprite sCursor;
    private Sprite mCursor;
    private Sprite activeCursor;
    private Label infoText;

    private enum States {NULL, INIT, RUN};
    private States currentState = States.NULL;
    private States previousState = States.NULL;

    private int actionTicker = 0;
    private int menu = 0;
    private int[] subMenu = new int[] {0, 1, 0};

    private Vector2 gStartPos = Vector2.Zero;
    private Vector2 sStartPos = Vector2.Zero;
    private Vector2 mStartPos = Vector2.Zero;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
        gCursor = (Sprite)GetNode("GameCursor");
        sCursor = (Sprite)GetNode("SpeedCursor");
        mCursor = (Sprite)GetNode("MusicCursor");
        infoText = (Label)GetNode("InfoText");

        SetState(States.INIT);
    }

/*     public override void _Input(InputEvent @event) {
        if(currentState != States.RUN) {
            return;
        }

        // Update which menu is selected.
        if(Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_down")) {
            int y = Convert.ToInt32(Input.IsActionJustPressed("ui_down")) - Convert.ToInt32(Input.IsActionJustPressed("ui_up"));
            int last = menu;

            menu += y;
            menu = Mathf.Clamp(menu, 0, 2);

            if(menu != last) {
                m.PlaySFX(3);
                UpdateMenu();
            }
        }
        
        // Update which menu option is selected.
        if(Input.IsActionJustPressed("ui_left") || Input.IsActionJustPressed("ui_right")) {
            int x = Convert.ToInt32(Input.IsActionJustPressed("ui_right")) - Convert.ToInt32(Input.IsActionJustPressed("ui_left"));
            int last;

            // There's two ways you can tackle the menu options. Below is the first and most line-consuming.
            switch(menu) {
                case 0:
                    last = subMenu[0];
                    subMenu[0] += x;
                    subMenu[0] = Mathf.Clamp(subMenu[0], 0, 1);

                    if(last != subMenu[0]) {
                        m.PlaySFX(3);
                        UpdatePositions();
                    }
                    break;
                
                case 1:
                    last = subMenu[1];
                    subMenu[1] += x;
                    subMenu[1] = Mathf.Clamp(subMenu[0], 0, 2);

                    if(last != subMenu[1]) {
                        m.PlaySFX(3);
                        UpdatePositions();
                    }
                    break;
                
                case 2:
                    last = subMenu[2];
                    subMenu[2] += x;
                    subMenu[2] = Mathf.Clamp(subMenu[0], 0, 3);

                    if(last != subMenu[2]) {
                        m.PlaySFX(3);
                        UpdatePositions();
                    }
                    break;
            }

            // The second is a bit more advanced, but saves a bit of code.
            for(int i = 0; i < subMenu.Length; i++) {
                if(menu == i) {
                    last = subMenu[i];
                    subMenu[i] += x;
                    subMenu[i] = Mathf.Clamp(subMenu[i], 0, i + 1); 

                    if(last != subMenu[i]) {
                        m.PlaySFX(3);
                        UpdatePositions();
                    }
                }
            }
        }

        if(Input.IsActionJustPressed("ui_accept")) {
            m.nextScene = true;
        }
    } */

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
            case States.RUN:
                // Process input.
                if(m.menuDirection.y != 0) {
                    int last = menu;

                    menu += (int)m.menuDirection.y;
                    menu = Mathf.Clamp(menu, 0, 2);

                    if(menu != last) {
                        m.PlaySFX(3);
                        UpdateMenu();
                    }
                }

                if(m.menuDirection.x != 0) {
                    int last;
                    for(int i = 0; i < subMenu.Length; i++) {
                        if(menu == i) {
                            last = subMenu[i];
                            subMenu[i] += (int)m.menuDirection.x;
                            subMenu[i] = Mathf.Clamp(subMenu[i], 0, i + 1); 

                            if(last != subMenu[i]) {
                                m.PlaySFX(3);
                                UpdatePositions();
                            }
                        }
                    }
                }

                if(m.accept || m.pause) {
                    m.nextScene = true;
                }

                actionTicker++;

                if(actionTicker % 8 == 0) {
                    activeCursor.Visible = !activeCursor.Visible;
                }

                if(infoText.VisibleCharacters < infoText.GetTotalCharacterCount()) {
                    infoText.VisibleCharacters++;
                }
                break;
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

    private void EnterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                // Set the starting positions of the cursors.
                gStartPos = gCursor.Position;
                sStartPos = sCursor.Position;
                mStartPos = mCursor.Position;

                // Set Music Type and Game Type to -1 in Master so the Type 1 music plays and text is displayed.
                m.gameType = -1;
                m.musicType = -1;
                break;
            
            case States.RUN:
                // Initialize the menu.
                UpdateMenu();
                UpdatePositions();
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

    private void UpdateMenu() {
        // Make all cursors visible.
        gCursor.Show();
        sCursor.Show();
        mCursor.Show();

        // Reset the action ticker
        actionTicker = 0;

        // Set the active cursor.
        switch(menu) {
            case 0:
                activeCursor = gCursor;
                break;
            
            case 1:
                activeCursor = sCursor;
                break;
            
            case 2:
                activeCursor = mCursor;
                break;
        }
    }

    private void UpdatePositions() {
        // Run UpdateMenu to reset cursor visibility.
        UpdateMenu();

        // Update cursor positions.
        gCursor.Position = gStartPos + (Vector2.Right * (80 * subMenu[0]));
        sCursor.Position = sStartPos + (Vector2.Right * (48 * subMenu[1]));
        mCursor.Position = mStartPos + (Vector2.Right * (40 * subMenu[2]));

        // Update options in Master.
        if(subMenu[0] != m.gameType) {

            //Update the info text box.
            infoText.VisibleCharacters = 0;

            switch(subMenu[0]) {
                case 0:
                    infoText.Text = "GO FOR THE HIGHEST\nSCORE!";
                    break;
                
                case 1:
                    infoText.Text = "EAT BUGS TO CLEAR LEVELS!";
                    break;
            }

            m.gameType = subMenu[0];
        }

        if(subMenu[1] != m.speed) {
            m.speed = subMenu[1];
        }

        if(subMenu[2] != m.musicType) {
            // Play the appropriate music or turn it off.
            switch(subMenu[2]) {
                case 0:
                    m.PlayMusic(7);
                    break;
                
                case 1:
                    m.PlayMusic(8);
                    break;
                
                case 2:
                    m.PlayMusic(9);
                    break;
                
                case 3:
                    m.StopMusic();
                    break;
            }

            m.musicType = subMenu[2];
        }
    }
}
