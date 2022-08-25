public class Random {
    private static readonly System.Random rand = new ();
    public static int RandomInt(int range) {
        return rand.Next(range);
    }

    public static float RandomFloat() {
        return (float)rand.NextDouble();
    }

    public static T RandomEnum<T> () {
        var values = System.Enum.GetValues(typeof(T));
        return (T)values.GetValue(rand.Next(values.Length));
    }

    public static int[] RandomPermutation(int totalSize, int resultSize) {
        int[] array = new int[totalSize];
        for (int i = 0; i < totalSize; ++i) {
            array[i] = i;
        }
        for (int i = 0; i < resultSize; ++i) {
            int exchangeIndex = i + rand.Next(totalSize - i);
            (array[i], array[exchangeIndex]) = (array[exchangeIndex], array[i]);
        }
        return array;
    }
}
