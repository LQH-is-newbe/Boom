using System.Collections;
using System.Collections.Generic;

public class Random {
    private static System.Random rand = new System.Random();
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
}
