using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sgr.Modules.Storage;
using Sgr.Modules.Storage.Adapters;

namespace Sgr.Tests.Unit.Storage;

/// <summary>
/// Tests del adapter local con tmp directory por test (cada test aislado).
/// </summary>
public class LocalFileSystemAdapterTests : IDisposable
{
    private readonly string _tmp;
    private readonly LocalFileSystemPhotoStorageAdapter _sut;

    public LocalFileSystemAdapterTests()
    {
        _tmp = Path.Combine(Path.GetTempPath(), $"sgr-storage-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tmp);
        var options = Options.Create(new LocalStorageOptions { RootPath = _tmp });
        _sut = new LocalFileSystemPhotoStorageAdapter(options, NullLogger<LocalFileSystemPhotoStorageAdapter>.Instance);
    }

    [Fact]
    public async Task Store_persists_file_and_returns_correct_metadata()
    {
        var content = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        await using var ms = new MemoryStream(content);

        var result = await _sut.StoreAsync(ms, new StorageHints("surveys/abc/p1", "image/jpeg", "test.jpg"));

        result.AdapterName.Should().Be("local");
        result.SizeBytes.Should().Be(4);
        result.AdapterRef.Should().Contain("surveys/abc/p1/");
        result.AdapterRef.Should().EndWith("_test.jpg");
        // SHA-256 of {01,02,03,04}
        result.ContentHash.Should().Be("9f64a747e1b97f131fabb6b447296c9b6f0201e79fb3c5356e6c77e89b6a806a");

        var fullPath = Path.Combine(_tmp, result.AdapterRef);
        File.Exists(fullPath).Should().BeTrue();
        File.ReadAllBytes(fullPath).Should().Equal(content);
    }

    [Fact]
    public async Task Read_returns_stored_content()
    {
        var content = new byte[] { 0xAA, 0xBB, 0xCC };
        var stored = await _sut.StoreAsync(new MemoryStream(content),
            new StorageHints("default", "application/octet-stream", "x.bin"));

        await using var stream = await _sut.ReadAsync(stored.AdapterRef);
        using var reader = new MemoryStream();
        await stream.CopyToAsync(reader);
        reader.ToArray().Should().Equal(content);
    }

    [Fact]
    public async Task Delete_removes_file()
    {
        var stored = await _sut.StoreAsync(new MemoryStream(new byte[] { 0x01 }),
            new StorageHints("default", "application/octet-stream", "to-delete.bin"));

        var fullPath = Path.Combine(_tmp, stored.AdapterRef);
        File.Exists(fullPath).Should().BeTrue();

        await _sut.DeleteAsync(stored.AdapterRef);
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task TestConnection_returns_true_when_root_writable()
    {
        var ok = await _sut.TestConnectionAsync();
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task Store_creates_subfolders_as_needed()
    {
        var stored = await _sut.StoreAsync(new MemoryStream(new byte[] { 0x42 }),
            new StorageHints("a/b/c/d", "text/plain", "deep.txt"));

        stored.AdapterRef.Should().StartWith("a/b/c/d/");
        var fullPath = Path.Combine(_tmp, stored.AdapterRef);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Fact]
    public async Task Hash_is_deterministic_across_stores()
    {
        var content = new byte[] { 0x10, 0x20, 0x30 };
        var s1 = await _sut.StoreAsync(new MemoryStream(content),
            new StorageHints("a", "application/octet-stream", "1.bin"));
        var s2 = await _sut.StoreAsync(new MemoryStream(content),
            new StorageHints("b", "application/octet-stream", "2.bin"));
        s1.ContentHash.Should().Be(s2.ContentHash);
        s1.AdapterRef.Should().NotBe(s2.AdapterRef); // distinto path
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmp)) Directory.Delete(_tmp, recursive: true);
    }
}
