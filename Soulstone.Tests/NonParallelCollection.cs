using Xunit;

namespace Soulstone.Tests
{
    [CollectionDefinition("NonParallel", DisableParallelization = true)]
    public class NonParallelCollection
    {
    }
}
