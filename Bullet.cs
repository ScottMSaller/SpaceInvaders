using System.Data.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceInvaders;
public class Bullet
{
        public Vector2 Position;
        public float Speed = -8f; //this makes the bullet float towards the top
        private Texture2D _texture;
        private const float Scale = 0.5f;

    public Bullet(Texture2D texture, Vector2 startPosition)
    {
        _texture = texture;
        Position = startPosition;
    }

        public void Update()
        {
            Position.Y += Speed;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, Position, null, Color.White, 0f, Vector2.Zero, .5f, SpriteEffects.None, 0f);
        }

    public Rectangle Bounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)(_texture.Width * Scale),
            (int)(_texture.Height * Scale)
        );      
    }
        public bool IsOffScreen(int screenHeight)
    {
        return Position.Y + (_texture.Height * Scale) < 0;
    }
}