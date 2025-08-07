using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Player
{
    public Vector2 Position;
    private Texture2D _texture;
    private float _speed = 5f;

    public Player(Texture2D texture, Vector2 startPosition)
    {
        _texture = texture;
        Position = startPosition;
    }

    public void Update()
    {

        if (Keyboard.GetState().IsKeyDown(Keys.Left) || Keyboard.GetState().IsKeyDown(Keys.A))
        {
            Position.X -= _speed;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.Right) || Keyboard.GetState().IsKeyDown(Keys.D))
        {
            Position.X += _speed;
        }

        Position.X = MathHelper.Clamp(Position.X, 0, 800 - _texture.Width);
    }
   public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, Color.White);
    }
}