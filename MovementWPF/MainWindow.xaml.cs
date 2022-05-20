using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MovementWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public enum Movement
    {
        Left,
        Right,
        Up,
        Down,
        None
    }
    public enum Facing
    {
        Left,
        Right,
        Up,
        Down
    }
    class Character
    {

        public Movement _moveing = Movement.None;
        public Facing _facing = Facing.Left;

        public int health = 5;


        public int frames_elapsed = 0;
        public int frames_since_hit = 0;
        public int minimum_frames_between_hits = 1000;
        public bool alive = true;
        public bool friendly;

        public double x;
        public double y;

        protected double down_val = 0.5;
        protected double up_val = 0.5;
        protected double left_val = 0.5;
        protected double right_val = 0.5;

        int _width;
        int _height;
        protected bool stop_at_wall = true;

        protected Rectangle _self_rect;
        protected Canvas mainCanvas;
        public Character(Canvas _mainCanvas, int width, int height, double _x, double _y, bool _friendly)
        {
            mainCanvas = _mainCanvas;
            // most of this is from https://www.ictdemy.com/csharp/wpf/drawing-on-canvas-in-csharp-net-wpf
            // but changed to work correcly
            // (the width and height sections were breaking)
            friendly = _friendly;
            // set the x and y of the rectangle
            x = _x;
            y = _y;
            _width = width;
            _height = height;
            // make a new rectangle
            _self_rect = new Rectangle();
            // set the width and height of the rectangle
            _self_rect.Width = width;
            _self_rect.Height = height;
            // set the colour of the rectangle based off of if it is friendly or not
            _self_rect.Fill = friendly ? Brushes.Green : Brushes.Red;
            // add the created rectangle to the screen
            mainCanvas.Children.Add(_self_rect);
            // draw the rectangle on the screen
            initalDraw();
        }
        protected void initalDraw()
        {
            Canvas.SetTop(_self_rect, y);
            Canvas.SetLeft(_self_rect, x);
        }
        public void move(object sender, EventArgs e)
        {
            switch (_moveing)
            {
                case Movement.Down:
                    _facing = Facing.Down;
                    y += down_val;
                    if (stop_at_wall && offScreen())
                    {
                        y -= down_val;
                    }
                    Canvas.SetTop(_self_rect, y);
                    break;

                case Movement.Up:
                    _facing = Facing.Up;
                    y -= up_val;
                    if (stop_at_wall && offScreen())
                    {
                        y += up_val;
                    }
                    Canvas.SetTop(_self_rect, y);
                    break;

                case Movement.Left:
                    _facing = Facing.Left;
                    x -= left_val;
                    if (stop_at_wall && offScreen())
                    {
                        x += left_val;
                    }
                    Canvas.SetLeft(_self_rect, x);
                    break;

                case Movement.Right:
                    _facing = Facing.Right;
                    x += right_val;
                    if (stop_at_wall && offScreen())
                    {
                        x -= right_val;
                    }
                    Canvas.SetLeft(_self_rect, x);
                    break;
                default:
                    break;
            } 
        }
        public bool inHitbox(double ent_x, double ent_y)
        {
            double upperbound_x = x + _width;
            double lowerbound_x = x;
            double upperbound_y = y;
            double lowerbound_y = y + _height;
            if (ent_x <= upperbound_x && ent_x >= lowerbound_x)
            {
                // if in the x box
                if (ent_y <= upperbound_y && ent_y >= lowerbound_y)
                {
                    // if in the y box
                    return true;
                }
                return false;
            }
            return false;
        }
        public bool inHitbox(Character _char)
        {
            return (_char.friendly!=friendly) // if the enemy is not friendly
                && (_char.x <= x+_width)  // and the x is to the left of the right x side
                && (_char.x >= x)   // and to the right of the left side
                && (_char.y <= y) // and the y is below the top
                && (_char.y >= y + _height); // and above the bottom
            // return that it hit
        }
        public void hit()
        {
            // if not enough frames have passed since the last time hit
            if ((frames_elapsed - frames_since_hit) < minimum_frames_between_hits)
            {
                // return as hit did not hit
                return;
            }
            // if enough time has passed between hits
            // reset the frames since the last hit to 0
            frames_since_hit = frames_elapsed;
            // reduce the health by 1
            health -= 1;
            if (health <= 0)
            {
                alive = false;
            }
        }
        public bool offScreen()
        {
            return (x < 0 || y < 0 || x>mainCanvas.ActualWidth-_width || y>mainCanvas.ActualHeight-_height);
        }
    }
    class Player : Character
    {
        // having a list of keys for each direction allows multiple keys to be pressed
        // to move the same direction, eg w and Up both moving the character up the screen
        public List<Key> leftKeys = new List<Key> { Key.Left };
        public List<Key> rightKeys = new List<Key> { Key.Right };
        public List<Key> downKeys = new List<Key> { Key.Down };
        public List<Key> upKeys = new List<Key> { Key.Up };
        public List<Key> fireKeys = new List<Key> { Key.Space };

        public List<Projectile> _projectiles = new List<Projectile>();
        int max_projectiles = 3;

        int projectile_width = 10;
        int projectile_height = 10;
        public Player (Canvas mainCanvas, int width, int height, double x, double y) : base (mainCanvas, width, height, x, y, true)
        {
        }

        protected void fire()
        {
            // goes through each projectile on the screen
            List<int> IdsToRemove = new List<int>();
            for (int projectile_id = 0; projectile_id < _projectiles.Count(); projectile_id++)
            {
                Projectile projectile = _projectiles[projectile_id];
                // if the projectile is off screen
                if (projectile.offScreen())
                {
                    // kill it
                    projectile.kill();
                }
            }
           
            // if another projectile can be fired 
            if (_projectiles.Count() < max_projectiles)
            {
                _projectiles.Add(new Projectile(mainCanvas, projectile_width, projectile_height, this));
            }
        }
        public void changeDirection (Key pressedKey)
        {
            // a switch case would be nicer here but it gave an error wanting a constant value
            // as [up|down|left|right]Key is/are varaibles
            // and if statements and early returns was a better solution than assigning temp variables as const
            // that was my over solution (i dont even know if that would work)

            if (downKeys.Contains(pressedKey)) { 
                this._moveing = Movement.Down;
                return;
            }
            if (upKeys.Contains(pressedKey)) {
                this._moveing = Movement.Up;
                return;
            }
            if (leftKeys.Contains(pressedKey)) {
                this._moveing = Movement.Left;
                return;
            }
            if (rightKeys.Contains(pressedKey)) {
                this._moveing = Movement.Right;
                return;
            }
            if (fireKeys.Contains(pressedKey))
            {
                fire();
                return;
            }
            this._moveing = Movement.None;
            return;
        }
        public Movement facing_to_movement()
        {
            switch (_facing)
            {
                case Facing.Up:
                    return Movement.Up;
                case Facing.Down:
                    return Movement.Down;
                case Facing.Left:
                    return Movement.Left;
                case Facing.Right:
                    return Movement.Right;
            }
            // this should never run but it causes an error if missing
            return Movement.Right;
        }
    }
    class Projectile : Character
    {
        Player _player;
        public Projectile(Canvas mainCanvas, int width, int height, Player player) : base(mainCanvas, width, height, player.x, player.y, player.friendly)
        {
            _player = player;
            stop_at_wall = false;
            _moveing = _player.facing_to_movement(); ;
        }
        public void kill()
        {
            _player._projectiles.Remove(this);
            mainCanvas.Children.Remove(_self_rect);
        }
    }
    class Enemy : Character
    {
        Player _player;
        public Enemy(Canvas mainCanvas, int width, int height, double x, double y, Player player) :base (mainCanvas, width, height, x, y, !player.friendly)
        {
            _player = player;
            down_val = 0.25;
            up_val = 0.25;
            left_val = 0.25;
            right_val = 0.25;
        }
        public void changeDirection()
        {
            double x_to_player = _player.x - this.x;
            double y_to_player = _player.y - this.y;
            if (Math.Abs(x_to_player) > Math.Abs(y_to_player))
            {
                if (x_to_player>0)
                {
                    this._moveing = Movement.Right;
                    return;
                }
                this._moveing = Movement.Left;
                return;
            }
            else
            {
                if (y_to_player >0)
                {
                    this._moveing = Movement.Down;
                    return;
                }
                this._moveing = Movement.Up;
                return;
            }
        }
    }
    public partial class MainWindow : Window
    {
        Player character;
        List<Enemy> enemys = new List<Enemy>();
        public MainWindow()
        {
            InitializeComponent();
            
            //create a new player instance
            character = new Player(MainCanvas, 50 ,20, 50, 60);

            // add other movement keys
            character.leftKeys.Add(Key.A);
            character.rightKeys.Add(Key.D);
            character.upKeys.Add(Key.W);
            character.downKeys.Add(Key.S);
            // adding keys ends here
            
            // add enemys
            enemys.Add(new Enemy(MainCanvas, 10, 10, 200, 200, character));
            enemys.Add(new Enemy(MainCanvas, 10, 10, 400, 300, character));

            

            //update the ui
            updateUI();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler(character.move);
            timer.Tick += new EventHandler(tick);
            timer.Start();

            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
            this.KeyUp += new KeyEventHandler(OnButtonKeyRelease);

        }
        public void tick(object sender, EventArgs e)
        {
            if (!character.alive)
            {
                return;
            }
            character.frames_elapsed++;
            foreach (Enemy enemy in enemys)
            {
                enemy.changeDirection();
                enemy.move(sender, e);
                // add a last hit timer so doesnt get hit every tick and die instantly
                if (character.inHitbox(enemy))
                {
                    character.hit();
                }

                foreach (Projectile _projectile in character._projectiles)
                {
                    if (_projectile.inHitbox(enemy))
                    {
                        enemy.hit();
                        _projectile.kill();
                        break;
                    }
                }
            }
            foreach (Projectile _projectile in character._projectiles)
            {
                _projectile.move(sender, e);
            }
            updateUI();
            
        }
        protected void updateUI()
        {
            _X_Box.Text = String.Format("{0:0.##}", character.x);
            _Y_Box.Text = String.Format("{0:0.##}", character.y);
            _Health_Box.Text = character.health.ToString();
            _Direction_Box.Text = character._moveing.ToString();
            _Facing_Box.Text = character._facing.ToString();
            _Time_Since_Box.Text = (character.frames_elapsed-character.frames_since_hit).ToString();
        }
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            character.changeDirection(e.Key);
        }
        private void OnButtonKeyRelease(object sender, KeyEventArgs e)
        {
            character._moveing = Movement.None;
            // i dont know what this line does so ive commented it out to see if something breaks
            //this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }
    }
}
