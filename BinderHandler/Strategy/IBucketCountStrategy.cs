namespace BinderHandler.Strategy
{
    /// <summary>
    /// An implementation for strategies that get a bucket count.
    /// </summary>
    public interface IBucketCountStrategy
    {
        /// <summary>
        /// Gets a bucket count according to the implemented strategy.
        /// </summary>
        /// <returns>A bucket count.</returns>
        public int ComputeBucketCount();
    }
}
