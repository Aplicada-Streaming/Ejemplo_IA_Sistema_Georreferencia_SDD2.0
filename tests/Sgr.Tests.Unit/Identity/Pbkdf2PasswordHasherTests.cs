using FluentAssertions;
using Sgr.Modules.Identity.Authentication;

namespace Sgr.Tests.Unit.Identity;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_then_Verify_returns_true_for_correct_password()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple");
        _sut.Verify("CorrectHorseBatteryStaple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple");
        _sut.Verify("Wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_produces_different_output_for_same_input_due_to_random_salt()
    {
        var hash1 = _sut.Hash("samepass");
        var hash2 = _sut.Hash("samepass");
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_throws_for_empty_password(string? input)
    {
        var act = () => _sut.Hash(input!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash()
    {
        _sut.Verify("any", "not-a-valid-hash").Should().BeFalse();
        _sut.Verify("any", "pbkdf2.x.x.x").Should().BeFalse();
    }
}
