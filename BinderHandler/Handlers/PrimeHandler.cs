namespace BinderHandler.Handlers
{
    internal static class PrimeHandler
    {
        internal static bool IsPrime(int number)
        {
            // Numbers less than 2 are not prime.
            // If the number % 1 is not 0 it is not prime.
            // If the number % itself is not 0 it is not prime.
            if (number < 2 || number % 1 != 0 || number % number != 0)
            {
                return false;
            }

            // 2 Is prime.
            if (number == 2)
            {
                return true;
            }

            // Check to see if it is divisible by any numbers other than 1 and itself, if it is, it is not prime.
            for (int i = 3; i < number; i++)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int GetNextPrime(int number)
        {
            while (!IsPrime(number))
            {
                number++;
            }

            return number;
        }
    }
}
