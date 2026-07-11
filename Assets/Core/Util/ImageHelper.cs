using UnityEngine;

public static class ImageHelper
{
	public static Sprite CreateSprite(Texture2D tex)
	{
		if (tex == null)
		{
			tex = new Texture2D(1, 1);
		}
		return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
	}
}
