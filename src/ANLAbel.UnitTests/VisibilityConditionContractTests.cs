using ANLAbel.Core.Data;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class VisibilityConditionContractTests
{
    [Theory]
    [InlineData("", "x", true)]
    [InlineData("{Show}", "1", true)]
    [InlineData("{Show}", "Lô 12", true)]
    [InlineData("{Show}", "", false)]
    [InlineData("{Show}", "0", false)]
    [InlineData("{Show}", "False", false)]
    [InlineData("{Show}", "không", false)]
    public void ObjectPrintsOnlyWhenTheConditionHasAValue(string condition, string value, bool shown)
        => Assert.Equal(shown, VisibilityConditionContract.IsShown(condition, new Dictionary<string, string> { ["Show"] = value }));

    [Fact]
    public void WithoutADataRowTheObjectIsShown()
        => Assert.True(VisibilityConditionContract.IsShown("{Show}", null));
}
