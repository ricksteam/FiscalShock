using UnityEngine;

namespace FiscalShock.Procedural {
    /// <summary>
    /// Gaussian (normal) distribution pseudorandom number generator. Useful
    /// when you want outliers to be rare and most values to fall around a
    /// mean value.
    ///
    /// <para>Adapted from https://stackoverflow.com/a/218600</para>
    /// </summary>
    public static class Gaussian {
        /// <summary>
        /// Get a random float from the Gaussian distribution described by
        /// the function arguments
        /// </summary>
        /// <param name="mean">most values fall around this point</param>
        /// <param name="stdDev">standard deviation of the distribution; most numbers generated will be within this value on either side of the mean (+/-)</param>
        /// <returns></returns>
        public static float next(float mean, float stdDev) {
            float u1 = 1.0f - Random.value;
            float u2 = 1.0f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                        Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
            float randNormal =
                        mean + (stdDev * randStdNormal); //random normal(mean,stdDev^2)

            return randNormal;
        }
    }
}
