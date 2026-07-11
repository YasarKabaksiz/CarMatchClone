namespace CarMatchClone.Gameplay
{
    public static class MatchChecker
    {
        // Returns start index of the first 3-in-a-row same-color group, or -1 if none.
        public static int FindMatch(Fruit[] slots)
        {
            for (int i = 0; i <= slots.Length - 3; i++)
            {
                if (slots[i] == null || slots[i + 1] == null || slots[i + 2] == null)
                    continue;
                if (slots[i].Color == slots[i + 1].Color && slots[i + 1].Color == slots[i + 2].Color)
                    return i;
            }
            return -1;
        }
    }
}
