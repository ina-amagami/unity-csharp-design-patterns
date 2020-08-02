
public static class Locator<T> where T : class
{
	public static T I { get; private set; }

	public static bool IsValid() => I != null;

	public static void Bind(T instance)
	{
		I = instance;
	}

	public static void Unbind(T instance)
	{
		if (I == instance)
		{
			I = null;
		}
	}

	public static void Clear()
	{
		I = null;
	}
}
