using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
public class Enemy
{
    public Vector2 Position;
    private Texture2D _texture;
    private float _speed;
    public Enemy(Texture2D texture, Vector2 startPosition)
    {
        _texture = texture;
        Position = startPosition;
    }

    public void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(_texture, Position, Color.White);

    public Rectangle Bounds() =>
        new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            _texture.Width,
            _texture.Height
        );
    public bool IsOffScreen(int screenHeight)
    {
        return Position.Y > screenHeight;
    }

    public int Width => _texture.Width;
    public int Height => _texture.Height;
}