using System;
namespace BE_0302.Helpers
{
    public static class PageHelper
    {
        public static int Clamp(int? page, int count, int pageSize = 10)
        {
            int lastPage = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
            return Math.Max(1, Math.Min(page ?? 1, lastPage));
        }
    }
}
