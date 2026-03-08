using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Metadata;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Application.Services;
using Aimy.Core.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Aimy.Tests.Services;

[TestFixture]
public class UploadKnowledgeSyncServiceTests
{
    private Mock<IUploadRepository> _uploadRepositoryMock = null!;
    private Mock<IKnowledgeItemRepository> _knowledgeItemRepositoryMock = null!;
    private Mock<IDataIngestionService> _dataIngestionServiceMock = null!;
    private Mock<IIngestionJobService> _ingestionJobServiceMock = null!;
    private Mock<IMetadataCatalogRepository> _metadataCatalogRepositoryMock = null!;
    private UploadKnowledgeSyncService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _uploadRepositoryMock = new Mock<IUploadRepository>();
        _knowledgeItemRepositoryMock = new Mock<IKnowledgeItemRepository>();
        _dataIngestionServiceMock = new Mock<IDataIngestionService>();
        _ingestionJobServiceMock = new Mock<IIngestionJobService>();
        _metadataCatalogRepositoryMock = new Mock<IMetadataCatalogRepository>();

        _sut = new UploadKnowledgeSyncService(
            _uploadRepositoryMock.Object,
            _knowledgeItemRepositoryMock.Object,
            _dataIngestionServiceMock.Object,
            _ingestionJobServiceMock.Object,
            _metadataCatalogRepositoryMock.Object,
            NullLogger<UploadKnowledgeSyncService>.Instance);

        var definitions = new List<MetadataDefinition>
        {
            new()
            {
                Key = "framework",
                Label = "Framework",
                ValueType = "string",
                Filterable = true,
                AllowFreeText = false,
                Policy = MetadataNormalizationPolicy.Strict
            },
            new()
            {
                Key = "source",
                Label = "Source",
                ValueType = "string",
                Filterable = true,
                AllowFreeText = true,
                Policy = MetadataNormalizationPolicy.Permissive
            }
        };

        _metadataCatalogRepositoryMock
            .Setup(repository => repository.GetDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitions);

        _metadataCatalogRepositoryMock
            .Setup(repository => repository.GetAllValueOptionsAsync("framework", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new MetadataValueOption
                {
                    CanonicalValue = "microsoft.agents",
                    DisplayLabel = "Microsoft Agents",
                    Aliases = ["ms agents", "Microsoft.Agents"],
                    SortOrder = 10
                },
                new MetadataValueOption
                {
                    CanonicalValue = "aspnet.core",
                    DisplayLabel = "ASP.NET Core",
                    Aliases = ["asp.net", "aspnet"],
                    SortOrder = 20
                }
            ]);

        _metadataCatalogRepositoryMock
            .Setup(repository => repository.GetAllValueOptionsAsync("source", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Test]
    public async Task NormalizeMetadataAsync_Alias_ResolvesToCanonicalValue()
    {
        var payload = "{\"framework\":\"ms agents\",\"source\":\"kb\"}";

        var result = await _sut.NormalizeMetadataAsync(payload, MetadataNormalizationPolicy.Permissive, CancellationToken.None);

        result.NormalizedMetadata.Should().NotBeNull();
        result.NormalizedMetadata!.Should().Contain("microsoft.agents");
        result.NormalizedMetadata.Should().Contain("\"source\":\"kb\"");
        result.Warnings.Should().Contain(warning => warning.Key == "framework" && warning.MatchType == MetadataMatchType.Alias);
    }

    [Test]
    public async Task NormalizeMetadataAsync_Fuzzy_ResolvesToCanonicalValue()
    {
        var payload = "{\"framework\":\"aspnet.cpre\"}";

        var result = await _sut.NormalizeMetadataAsync(payload, MetadataNormalizationPolicy.Permissive, CancellationToken.None);

        result.NormalizedMetadata.Should().NotBeNull();
        result.NormalizedMetadata!.Should().Contain("aspnet.core");
        result.Warnings.Should().Contain(warning => warning.Key == "framework" && warning.MatchType == MetadataMatchType.Fuzzy);
    }

    [Test]
    public async Task NormalizeMetadataAsync_StrictUnknownKey_RejectsUnknownKey()
    {
        var payload = "{\"unknown\":\"value\"}";

        var result = await _sut.NormalizeMetadataAsync(payload, MetadataNormalizationPolicy.Strict, CancellationToken.None);

        result.NormalizedMetadata.Should().BeNull();
        result.Warnings.Should().ContainSingle(warning => warning.MatchType == MetadataMatchType.Rejected);
    }

    [Test]
    public async Task NormalizeMetadataAsync_StrictDefinitionRejectsUnknownValue_WhenFreeTextDisabled()
    {
        var payload = "{\"framework\":\"random-framework\"}";

        var result = await _sut.NormalizeMetadataAsync(payload, MetadataNormalizationPolicy.Permissive, CancellationToken.None);

        result.NormalizedMetadata.Should().BeNull();
        result.Warnings.Should().ContainSingle(warning => warning.Key == "framework" && warning.MatchType == MetadataMatchType.Rejected);
    }

    [Test]
    public async Task UpsertValueOptionAsync_DuplicateAliasAcrossDifferentCanonicalValues_Throws()
    {
        _metadataCatalogRepositoryMock
            .Setup(repository => repository.GetAllValueOptionsAsync("framework", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new MetadataValueOption
                {
                    CanonicalValue = "microsoft.agents",
                    DisplayLabel = "Microsoft Agents",
                    Aliases = ["ms agents"]
                }
            ]);

        var act = () => _sut.UpsertValueOptionAsync("framework", new MetadataValueOption
        {
            CanonicalValue = "aspnet.core",
            DisplayLabel = "ASP.NET Core",
            Aliases = ["ms agents"]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
