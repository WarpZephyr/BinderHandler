using BinderHandler.Strategy;

namespace BinderHandler.Hashes
{
    /// <summary>
    /// An object that holds information on how buckets should be handled.
    /// </summary>
    /// <param name="BucketCountStrategy">The strategy dictating how bucket counts are calculated.</param>
    /// <param name="BucketIndexStrategy">The strategy dictating how bucket indices will be calculated.</param>
    public record BucketInfo(IBucketCountStrategy BucketCountStrategy, IBucketIndexStrategy BucketIndexStrategy);
}
