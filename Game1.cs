using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace SpaceInvaders;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class Game1 : Game
{
    private readonly int _width = 800;
    private readonly int _height = 800;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private GameState _gameState = GameState.Menu;
    private KeyboardState _prevKb;

    // Assets
    private Texture2D _shipTexture;
    private Texture2D _bulletTexture;
    private Texture2D _enemyTexture;
    private SpriteFont _font;
    private SoundEffect _shootSound;
    private SoundEffect _explosionSound;
    private Song _menuTheme;
    private Song _inGameTheme;
    private Song _currentSong;

    // Menu
    private readonly string[] _menuItems = { "Start Game", "Quit" };
    private int _selectedIndex = 0;

    // Gameplay state
    private GameState _lastState;
    private Random _random = new Random();
    private Player _player;
    private List<Bullet> _bulletList = new List<Bullet>();
    private List<Enemy> _enemyList = new List<Enemy>();
    private int _score = 0;
    private int _health = 100;

    // Waves
    private int _wave = 0;
    private const int _wavesToBeat = 5;
    private bool _waveActive = false;

    // Swarm Movement (group control)
    private float _swarmDir = 1f;
    private float _swarmSpeed = 40f;
    private float _swarmDrop = 20f;
    private double _waveCooldownTimer = 0;
    private const double _waveCooldownSeconds = 2.0;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = _width;
        _graphics.PreferredBackBufferHeight = _height;
    }


    protected override void Initialize()
    {
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _shipTexture = Content.Load<Texture2D>("ship");
        _bulletTexture = Content.Load<Texture2D>("bullet");
        _enemyTexture = Content.Load<Texture2D>("enemy");
        _font = Content.Load<SpriteFont>("DefaultFont");
        _shootSound = Content.Load<SoundEffect>("shooting");
        _explosionSound = Content.Load<SoundEffect>("explosion");
        _menuTheme = Content.Load<Song>("menu_theme");
        _inGameTheme = Content.Load<Song>("in_game");

        // Create player (bottom center)
        _player = new Player(
            _shipTexture,
            new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height - 60)
        );
    }

    private void MusicHandler(Song next)
    {
        if (_currentSong == next && MediaPlayer.State == MediaState.Playing)
        {
            return;
        }
        _currentSong = next;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Play(next);
    }
    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();

        // Global escape behavior (optional): Esc from Playing -> Menu, otherwise exit
        if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
        {
            if (_gameState == GameState.Playing)
                _gameState = GameState.Menu;
            else
                Exit();
        }

        if (_gameState != _lastState)
        {
            switch (_gameState)
            {
                case GameState.Menu: MusicHandler(_menuTheme); break;
                case GameState.Playing: MusicHandler(_inGameTheme); break;
                case GameState.Paused: MediaPlayer.Pause(); break;
                case GameState.GameOver: MediaPlayer.Pause(); break;
            }
            _lastState = _gameState;
        }

        switch (_gameState)
        {
            case GameState.Menu:
                MusicHandler(_menuTheme);
                UpdateMenu(kb);
                break;
            case GameState.Playing:
                MusicHandler(_inGameTheme);
                UpdatePlaying(kb, gameTime);
                break;
            case GameState.Paused:
                // TODO
                break;
            case GameState.GameOver:
                // TODO
                break;
        }
        _prevKb = kb;
        base.Update(gameTime);
    }

    private void UpdateMenu(KeyboardState kb)
    {
        // Down/S moves selection down
        if ((kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down)) ||
            (kb.IsKeyDown(Keys.S) && _prevKb.IsKeyUp(Keys.S)))
        {
            _selectedIndex = (_selectedIndex + 1) % _menuItems.Length;
        }

        // Up/W moves selection up
        if ((kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up)) ||
            (kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W)))
        {
            _selectedIndex = (_selectedIndex - 1 + _menuItems.Length) % _menuItems.Length;
        }

        // Select with Enter
        if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
        {
            var choice = _menuItems[_selectedIndex];
            if (choice == "Start Game")
            {
                StartNewGame();
                _gameState = GameState.Playing;
            }
            else if (choice == "Quit")
            {
                Exit();
            }
        }
    }

    private void UpdatePlaying(KeyboardState kb, GameTime gameTime)
    {   
        // --- Player movement
        _player.Update();

        // --- Shooting (edge-detected Space)
        if (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space))
        {
            var bullet = new Bullet(_bulletTexture, new Vector2(_player.Position.X, _player.Position.Y));
            _bulletList.Add(bullet);
            _shootSound?.Play();
        }

        // --- Update bullets & cull
        foreach (var b in _bulletList.ToList())
        {
            b.Update();
            if (b.IsOffScreen(GraphicsDevice.Viewport.Height))
                _bulletList.Remove(b);
        }

        // --- Advance wave if all enemies are gone
        if (_waveActive && _enemyList.Count == 0)
        {
            _waveActive = false;
            _waveCooldownTimer = _waveCooldownSeconds;
        }

        if (!_waveActive && _wave < _wavesToBeat && _waveCooldownTimer > 0)
        {
            _waveCooldownTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_waveCooldownTimer <= 0)
            {
                StartNextWave();
            }
        }

        // --- Swarm Movement
        if (_waveActive)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool hitEdge = false;

            foreach (var e in _enemyList)
            {
                e.Position.X += _swarmDir * _swarmSpeed * dt;

                if (e.Position.X < 0 || e.Position.X + e.Width > GraphicsDevice.Viewport.Width)
                {
                    hitEdge = true;
                }
            }
            if (hitEdge)
            {
                _swarmDir *= -1f;
                foreach (var e in _enemyList)
                    e.Position.Y += _swarmDrop;
            }
        }




        // --- Update enemies & cull
        foreach (var e in _enemyList.ToList())
        {
            if (e.IsOffScreen(GraphicsDevice.Viewport.Height))
            {
                _health -= 10;
                _enemyList.Remove(e);
            }
                
        }

        // --- Collisions: bullets vs enemies
        var bulletsToRemove = new List<Bullet>();
        var enemiesToRemove = new List<Enemy>();

        foreach (var b in _bulletList)
        {
            foreach (var e in _enemyList)
            {
                if (b.Bounds().Intersects(e.Bounds()))
                {
                    bulletsToRemove.Add(b);
                    enemiesToRemove.Add(e);
                    _explosionSound?.Play();
                    _score += 10;
                    break; // each bullet kills one enemy
                }
            }
        }

        if (bulletsToRemove.Count > 0)
            _bulletList.RemoveAll(b => bulletsToRemove.Contains(b));

        if (enemiesToRemove.Count > 0)
            _enemyList.RemoveAll(e => enemiesToRemove.Contains(e));
    }

    private void StartNewGame()
    {
        _score = 0;
        _bulletList.Clear();
        _enemyList.Clear();

        _player.Position = new Vector2(
            GraphicsDevice.Viewport.Width / 2f,
            GraphicsDevice.Viewport.Height - 60
        );

        _wave = 0;
        _waveActive = false;
        _waveCooldownTimer = 0;

        StartNextWave();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();

        switch (_gameState)
        {
            case GameState.Menu:
                DrawMenu();
                break;

            case GameState.Playing:
                _player.Draw(_spriteBatch);

                foreach (var b in _bulletList)
                    b.Draw(_spriteBatch);

                foreach (var e in _enemyList)
                    e.Draw(_spriteBatch);

                _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(_font, $"Health: {_health}", new Vector2(10, 30), Color.White);
                break;

            case GameState.Paused:
                // TODO
                break;

            case GameState.GameOver:
                // TODO
                break;
        }
        if (_waveActive == false && _wave < _wavesToBeat && _gameState == GameState.Playing) 
        {
            _spriteBatch.DrawString(_font, $"Wave {_wave} cleared! Next wave incoming...", new Vector2(200, 400), Color.Yellow);
        }
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawMenu()
    {
        var vp = GraphicsDevice.Viewport;
        var centerX = vp.Width / 2f;
        float y = vp.Height / 2f;

        var title = "SPACE INVADERS";
        var titleSize = _font.MeasureString(title);
        _spriteBatch.DrawString(_font, title, new Vector2(centerX - titleSize.X / 2f, y - 120), Color.Lime);

        for (int i = 0; i < _menuItems.Length; i++)
        {
            var text = _menuItems[i];
            var size = _font.MeasureString(text);
            var pos = new Vector2(centerX - size.X / 2f, y + i * 40);
            var color = (i == _selectedIndex) ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_font, text, pos, color);
        }
    }

    private void StartNextWave()
    {
        _wave++;

        if (_wave > _wavesToBeat)
        {
            _gameState = GameState.GameOver;
            return;
        }

        _enemyList.Clear();

        int rows = Math.Min(2 + _wave, 6);
        int cols = Math.Min(5 + _wave, 10);
        int spacingX = 60;
        int spacingY = 50;

        int gridWidth  = cols * (_enemyTexture.Width + spacingX) - spacingX;
        int startX = (GraphicsDevice.Viewport.Width - gridWidth) / 2;
        int startY = 60; // a little down from the top

    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            int x = startX + c * (_enemyTexture.Width + spacingX);
            int y = startY + r * (_enemyTexture.Height + spacingY);
            _enemyList.Add(new Enemy(_enemyTexture, new Vector2(x, y)));
        }
    }

    // Scale swarm speed by wave
    _swarmSpeed = 40f + (_wave - 1) * 12f;
    _swarmDir = 1f;
    _waveActive = true;
    }
}

