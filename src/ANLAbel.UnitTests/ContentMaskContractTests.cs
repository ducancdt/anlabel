using ANLAbel.Core.Text;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ContentMaskContractTests
{
    [Theory]
    [InlineData("(***) ***-****", "*", "1234567890", "(123) 456-7890")]
    [InlineData("(**)", "*", "1J", "(1J)")]
    [InlineData("(**)", "*", "J", "(J")]
    [InlineData("**-**", "*", "123456", "12-3456")]
    [InlineData("#-#", "#", "ab", "a-b")]
    [InlineData("", "*", "plain", "plain")]
    [InlineData("(**)", "*", "", "")]
    public void MaskCharactersTakeValueCharactersInOrder(string mask, string character, string value, string expected)
        => Assert.Equal(expected, ContentMaskContract.Apply(mask, character, value));

    [Fact]
    public void EachLineStartsTheMaskAgain()
        => Assert.Equal("(1A)\n\n(2B)", ContentMaskContract.Apply("(**)", "*", "1A\r\n\r\n2B"));
}
