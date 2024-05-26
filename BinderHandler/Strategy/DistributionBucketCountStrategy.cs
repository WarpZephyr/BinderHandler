using BinderHandler.Handlers;

namespace BinderHandler.Strategy
{
    /// <summary>
    /// Gets a bucket count by a given distribution target.
    /// </summary>
    /// <param name="bucketDistribution">The amount of files most buckets will have. The amount per bucket will vary around this value.</param>
    /// <param name="totalFileCount">The total number of files being stored across all buckets.</param>
    public class DistributionBucketCountStrategy(int bucketDistribution, int totalFileCount) : IBucketCountStrategy
    {
        /// <summary>
        /// The amount of files most buckets will have. The amount per bucket will vary around this value.
        /// </summary>
        public int BucketDistribution { get; set; } = bucketDistribution;

        /// <summary>
        /// The total number of files being stored across all buckets.
        /// </summary>
        public int TotalFileCount { get; set; } = totalFileCount;

        public int ComputeBucketCount()
        {
            return PrimeHandler.GetNextPrime(TotalFileCount / BucketDistribution);
        }
    }
}
