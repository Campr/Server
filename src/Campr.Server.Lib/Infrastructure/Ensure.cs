using System;

namespace Campr.Server.Lib.Infrastructure
{
    public static class Ensure
    {
        public static class Argument
        {
            /// <summary>
            ///     Ensures that the argument supplied is not null.
            /// </summary>
            /// <param name="argument">The argument to check.</param>
            /// <param name="argumentName">The argument name, used to report failures.</param>
            /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
            public static void IsNotNull(object argument, string argumentName)
            {
                if (argument == null)
                {
                    throw new ArgumentNullException(argumentName);
                }
            }
            
            /// <summary>
            ///     Ensures the argument supplied is not null, whitespace, or empty.
            /// </summary>
            /// <param name="argument">The argument to check.</param>
            /// <param name="argumentName">The argument name, used to report failures.</param>
            /// <exception cref="ArgumentException">Thrown when the argument is whitespace or empty.</exception>
            /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
            public static void IsNotNullOrWhiteSpace(string argument, string argumentName)
            {
                IsNotNull(argument, argumentName);

                if (string.IsNullOrWhiteSpace(argument))
                {
                    throw new ArgumentException("Argument was empty or whitespace.", argumentName);
                }
            }
        }
    }
}
