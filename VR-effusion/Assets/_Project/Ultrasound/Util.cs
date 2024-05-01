namespace Ultrasound {
    internal class Util {
        internal static int NumGroups(int total, int groupSize) => (total + groupSize - 1) / groupSize;
    }
}