namespace BinderHandler.Handlers
{
    internal static class StringHandler
    {
        internal static string GetMost(IList<string> strs)
        {
            var instances = new Dictionary<string, int>();
            foreach (var str in strs)
            {
                if (!instances.TryAdd(str, 1))
                {
                    instances[str] += 1;
                }
            }

            int greatestNumber = 0;
            string most = string.Empty;
            foreach (var instance in instances)
            {
                if (instance.Value > greatestNumber)
                {
                    greatestNumber = instance.Value;
                    most = instance.Key;
                }
            }

            return most ?? string.Empty;
        }
    }
}
