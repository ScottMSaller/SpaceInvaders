using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
public class Enemy
{
    public Vector2 Position;
    private Texture2D _texture;
    private float _speed = .5f;
    public Enemy(Texture2D texture, Vector2 startPosition)
    {
        _texture = texture;
        Position = startPosition;
    }

    public void Update()
    {
        Position.Y += _speed;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, Position, Color.Black);
 
    }
}