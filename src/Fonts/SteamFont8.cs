using SDL_Sharp;

public class SteamFont8 : FontRenderer
{
	public SteamFont8(Texture fontTexture) : base(fontTexture)
	{
	}

	public override int[] widths { get; set; } = new int[]
	{
		6 , 6 , 5 , 6 , 6 , 4 , 6 , 6 , 2 , 4 , 6 , 2 , 8 , 6 , 6 , 6 , 6 , 4 , 5 , 4 , 6 ,
		6 , 8 , 6 , 6 , 6 , 7 , 6 , 7 , 7 , 6 , 6 , 7 , 7 , 4 , 5 , 6 , 5 , 8 , 7 , 8 , 6 ,
		8 , 7 , 6 , 6 , 7 , 6 , 10, 6 , 6 , 6 , 6 , 4 , 6 , 6 , 6 , 6 , 6 , 6 , 6 , 6 , 4 ,
		2 , 10, 7 , 6 , 10, 8 , 8 , 6 , 4 , 4 , 7 , 8 , 4 , 8 , 3 , 8 , 4 , 4 , 5 , 5 , 2 ,
		3 , 2 , 6 , 6 , 5 , 4 , 2 , 3 , 2 , 4 ,
	};

}