using SDL_Sharp;

public class SteamFont7 : FontRenderer
{
	public SteamFont7(Texture fontTexture) : base(fontTexture)
	{
	}

	public override int[] widths { get; set; } = new int[]
	{
		6 , 6 , 5 , 6 , 6 , 4 , 6 , 7 , 2 , 4 , 6 , 3 , 9 , 6 , 6 , 6 , 6 , 4 , 6 , 4 , 6 ,
		6 , 8 , 6 , 6 , 6 , 8 , 6 , 7 , 8 , 6 , 6 , 7 , 7 , 5 , 5 , 6 , 6 , 8 , 7 , 8 , 6 ,
		8 , 7 , 6 , 6 , 7 , 6 , 10 , 6 , 6 , 6 , 6 , 4 , 6 , 6 , 6 , 6 , 6 , 6 , 6 , 6 , 3 ,
		6 , 6 , 6 , 6 , 10 , 6 , 6 , 6 , 4 , 4 , 6 , 7 , 4 , 6 , 6 , 6 , 4 , 4 , 4 , 4 , 2 ,
		3 , 3 , 6 , 6 , 6 , 3 , 3 , 3 , 3 , 3 ,
	};

}