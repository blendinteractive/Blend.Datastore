
using Xunit;

// Because all tests use the same database or set it up and tear it down,
// need to run tests serially. One should not be running DB migrations 
// from multiple threads
[assembly: CollectionBehavior(DisableTestParallelization = true)]


